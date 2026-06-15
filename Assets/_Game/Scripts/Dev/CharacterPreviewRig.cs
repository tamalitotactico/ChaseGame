using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Character Preview Harness (dev-only). Spawnea un CharacterData sobre el prefab base de
/// su bando, le aplica una Skin opcional, y lo maneja desde el inspector (mover, atacar,
/// disparar cada habilidad, oir un SFX) para iterar animaciones, sonidos y shaders/efectos
/// SIN armar una partida completa.
///
/// Tambien puede spawnear "training dummies" enemigos inmoviles y registra un
/// IWorldQueryService minimo, para poder probar habilidades que dependen de objetivos
/// (Enchant, Assault, mordisco del lobo, etc.) y ULTS por carga de hits (boton para simular
/// golpes basicos al dummy) sin necesidad de GameManager ni una partida real.
///
/// Workflow:
///   1. Tools > Chase Game > Build Character Preview Scene (la crea cableada).
///   2. Asignar 'characterData' (y 'skin' opcional) en este componente.
///   3. Entrar en Play y usar los botones del inspector (CharacterPreviewRigEditor).
///
/// Conduce el personaje con un CharacterPreviewBrain (gesto press/hold/release real), asi que
/// las habilidades, SFX (sfxOnCast) y efectos pasan por el mismo pipeline que en partida.
/// </summary>
public class CharacterPreviewRig : MonoBehaviour
{
    [Header("Que personaje")]
    [Tooltip("CharacterData a previsualizar (define bando, stats y habilidades).")]
    public CharacterData characterData;
    [Tooltip("Skin opcional: reemplaza el RuntimeAnimatorController del hijo visual.")]
    public Skin skin;

    [Header("Prefabs base (uno por bando)")]
    public GameObject hunterPrefab;
    public GameObject preyPrefab;

    [Header("Aim por defecto al disparar habilidades")]
    public Vector2 abilityAimDir = Vector2.right;

    [Header("SFX suelto a previsualizar (boton en el inspector)")]
    public AudioCue sfxToPreview;

    [Header("Training dummy (probar target / ults)")]
    [Tooltip("CharacterData del dummy enemigo. Si null, usa el data por defecto del prefab base del bando contrario.")]
    public CharacterData dummyData;
    [Tooltip("Distancia al frente (eje X) a la que aparece el primer dummy.")]
    public float dummySpawnDistance = 3f;

    GameObject            _instance;
    Character             _character;
    CharacterPreviewBrain _brain;

    readonly List<Character> _dummies = new();

    public Character Current     => _character;
    public bool      HasInstance => _instance != null;
    public int       DummyCount  => _dummies.Count;

    /// <summary>Destruye lo anterior y spawnea characterData sobre el prefab de su bando.</summary>
    public void Spawn()
    {
        Despawn();
        if (characterData == null) { Debug.LogError("[Preview] Asigna un CharacterData."); return; }

        var prefab = characterData.team == CharacterTeam.Hunter ? hunterPrefab : preyPrefab;
        if (prefab == null) { Debug.LogError($"[Preview] Falta el prefab base de {characterData.team}."); return; }

        EnsureServices();

        _instance = Instantiate(prefab, transform.position, Quaternion.identity);
        _instance.name = $"PREVIEW_{characterData.displayName}";

        _character = _instance.GetComponent<Character>();
        if (_character == null) { Debug.LogError("[Preview] El prefab base no tiene Character."); Despawn(); return; }

        _character.SetData(characterData);
        ApplySkin();

        _brain = _instance.GetComponent<CharacterPreviewBrain>() ?? _instance.AddComponent<CharacterPreviewBrain>();
        _character.SetBrain(_brain);
        _character.SetAuthority(LocalAuthority.Instance);
    }

    public void Despawn()
    {
        ClearDummies();
        if (_instance != null) Destroy(_instance);
        _instance = null; _character = null; _brain = null;
    }

    /// <summary>Re-aplica la skin (swap del RuntimeAnimatorController + rebuild de la tabla).</summary>
    public void ApplySkin()
    {
        if (_instance == null || skin == null || skin.animatorController == null) return;
        var animator = _instance.GetComponentInChildren<Animator>();
        if (animator != null) animator.runtimeAnimatorController = skin.animatorController;
        var charAnim = _instance.GetComponent<CharacterAnimator>();
        if (charAnim != null) charAnim.RebuildStateTable();
    }

    public void Move(Vector2 dir)     { if (_brain != null) _brain.SetMove(dir); }
    public void StopMove()            { if (_brain != null) _brain.Stop(); }
    public void Attack()              { if (_brain != null) _brain.QueueAttack(); }
    public void TriggerSlot(int slot) { if (_brain != null) _brain.QueueAbility(slot, abilityAimDir); }

    public void PreviewSfx()
    {
        if (sfxToPreview == null) return;
        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(sfxToPreview, transform.position);
    }

    // --- Training dummies ---

    /// <summary>Spawnea un dummy ENEMIGO (bando contrario), inmovil (sin brain) y vivo.</summary>
    public void SpawnDummy() => SpawnDummy(ally: false);

    /// <summary>Spawnea un dummy del bando indicado (ally=true => mismo bando que characterData, para
    /// probar habilidades de aliado como Booster Pills/Sanacion). El world-query lo agrupa por su Team.</summary>
    public void SpawnDummy(bool ally)
    {
        if (characterData == null) { Debug.LogError("[Preview] Asigna characterData primero."); return; }
        EnsureServices();

        var team = ally
            ? characterData.team
            : (characterData.team == CharacterTeam.Hunter ? CharacterTeam.Prey : CharacterTeam.Hunter);
        var prefab = team == CharacterTeam.Hunter ? hunterPrefab : preyPrefab;
        if (prefab == null) { Debug.LogError($"[Preview] Falta el prefab base de {team}."); return; }

        Vector3 pos = transform.position + Vector3.right * (dummySpawnDistance + _dummies.Count * 1.5f);
        var go = Instantiate(prefab, pos, Quaternion.identity);
        go.name = $"DUMMY_{(ally ? "ALLY" : "ENEMY")}_{team}_{_dummies.Count}";

        var c = go.GetComponent<Character>();
        if (c == null) { Debug.LogError("[Preview] El prefab dummy no tiene Character."); Destroy(go); return; }
        if (dummyData != null && !ally) c.SetData(dummyData);
        c.SetAuthority(LocalAuthority.Instance); // sin brain => queda quieto
        _dummies.Add(c);
    }

    public void ClearDummies()
    {
        for (int i = 0; i < _dummies.Count; i++)
            if (_dummies[i] != null) Destroy(_dummies[i].gameObject);
        _dummies.Clear();
    }

    /// <summary>Publica un BasicAttackLandedEvent del personaje sobre el dummy mas cercano:
    /// carga las ults por hits (usesHitCharge) sin danar al dummy (asi se reutiliza). Es el
    /// mismo evento que emite el combate real al conectar un basico.</summary>
    public void SimulateHitNearestDummy()
    {
        if (_character == null) return;
        var dummy = NearestDummy();
        if (dummy == null) { Debug.LogWarning("[Preview] No hay dummies. Spawnea uno primero."); return; }
        EventBus.Publish(new BasicAttackLandedEvent { Attacker = _character, Victim = dummy });
    }

    Character NearestDummy()
    {
        if (_character == null) return null;
        Vector2 from = _character.transform.position;
        Character best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < _dummies.Count; i++)
        {
            var d = _dummies[i];
            if (d == null) continue;
            float sqr = ((Vector2)d.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = d; }
        }
        return best;
    }

    /// <summary>La escena de preview no tiene GameManager; registra los servicios minimos
    /// que las habilidades resuelven (spawn de proyectiles/placeables y consultas de mundo
    /// para target/ults). El world-query lee el estado vivo de este rig.</summary>
    void EnsureServices()
    {
        if (ServiceLocator.Resolve<ISpawnService>() == null)
            ServiceLocator.Register<ISpawnService>(new LocalSpawnService());
        if (ServiceLocator.Resolve<IWorldQueryService>() == null)
            ServiceLocator.Register<IWorldQueryService>(new PreviewWorldQuery(this));
    }

    /// <summary>IWorldQueryService minimo que reporta al personaje previsualizado + los dummies,
    /// agrupados por bando. Lee el estado vivo del rig (refleja respawns y dummies nuevos).</summary>
    class PreviewWorldQuery : IWorldQueryService
    {
        readonly CharacterPreviewRig _rig;
        public PreviewWorldQuery(CharacterPreviewRig rig) { _rig = rig; }

        List<Character> Bucket(CharacterTeam team)
        {
            var list = new List<Character>();
            if (_rig._character != null && _rig._character.Team == team && _rig._character.IsAlive)
                list.Add(_rig._character);
            for (int i = 0; i < _rig._dummies.Count; i++)
            {
                var d = _rig._dummies[i];
                if (d != null && d.Team == team && d.IsAlive) list.Add(d);
            }
            return list;
        }

        static CharacterTeam Opposite(CharacterTeam t) =>
            t == CharacterTeam.Hunter ? CharacterTeam.Prey : CharacterTeam.Hunter;

        public IReadOnlyList<Character> Hunters => Bucket(CharacterTeam.Hunter);
        public IReadOnlyList<Character> Preys   => Bucket(CharacterTeam.Prey);
        public IReadOnlyList<Character> GetTeam(CharacterTeam team)      => Bucket(team);
        public IReadOnlyList<Character> GetEnemiesOf(CharacterTeam team) => Bucket(Opposite(team));
        public IReadOnlyList<Character> GetAlliesOf(CharacterTeam team)  => Bucket(team);
    }
}
