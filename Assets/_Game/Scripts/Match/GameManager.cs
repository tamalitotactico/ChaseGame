using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Glue de la escena de partida. Posee:
///  - Configuracion (MatchSettings, prefabs, datos, spawn points)
///  - MatchStateMachine
///  - Listas de personajes vivos
///  - Registra servicios en ServiceLocator (ISpawnService, IAuthorityContext)
///
/// Phase 0: scene-scoped, sin DontDestroyOnLoad, una instancia por escena.
/// Phase 3: el Runner de Fusion sera quien spawne; este componente leera datos
/// pero no hara Instantiate directo.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] MatchSettings matchSettings;

    [Header("Prefabs")]
    [SerializeField] GameObject hunterPrefab;
    [SerializeField] GameObject preyPrefab;

    [Header("Spawns")]
    [SerializeField] Transform[] hunterSpawns;
    [SerializeField] Transform[] preySpawns;

    [Header("Composicion (Phase 0 sandbox)")]
    [SerializeField] CharacterTeam playerTeam = CharacterTeam.Hunter;
    [SerializeField] int huntersTotal = 1;
    [SerializeField] int preysTotal   = 2;
    [SerializeField] bool autoStart   = true;

    [Header("Audio - Musica de partida")]
    [Tooltip("Musica durante PlayingState. Debe ser un AudioCue con loop=true y mixerGroup=Music.")]
    [SerializeField] AudioCue gameplayMusic;

    [Tooltip("Musica durante EndingState (pantalla de resultados). Puede ser null.")]
    [SerializeField] AudioCue endingMusic;

    public AudioCue GameplayMusic => gameplayMusic;
    public AudioCue EndingMusic   => endingMusic;

    public MatchSettings Settings   => matchSettings;
    public MatchStateMachine States { get; private set; }
    public List<Character> Hunters { get; } = new();
    public List<Character> Preys   { get; } = new();
    public float TimeRemaining { get; set; }
    public CharacterTeam PlayerTeam => playerTeam;

    public int AliveHuntersCount
    {
        get { int n = 0; foreach (var c in Hunters) if (c != null && c.IsAlive) n++; return n; }
    }
    public int AlivePreysCount
    {
        get { int n = 0; foreach (var c in Preys) if (c != null && c.IsAlive) n++; return n; }
    }

    /// <summary>
    /// Preys que aun pueden estar en partida: vivos O downed (todavia salvables).
    /// La match termina cuando este contador llega a 0, no AlivePreysCount.
    /// </summary>
    public int ActivePreysCount
    {
        get { int n = 0; foreach (var c in Preys) if (c != null && c.IsTargetable) n++; return n; }
    }

    void Awake()
    {
        Instance = this;
        ServiceLocator.Register<ISpawnService>(new LocalSpawnService());
        ServiceLocator.Register<IAuthorityContext>(LocalAuthority.Instance);
        ServiceLocator.Register<IWorldQueryService>(new LocalWorldQuery(this));
        States = new MatchStateMachine(this);
    }

    /// <summary>
    /// Teardown de los sistemas que ESTE GameManager posee. Se llama al destruirse (unload de
    /// la escena de partida). NO hace Clear() global: con el split Meta/Gameplay hay servicios
    /// persistentes cross-scene (IProfileService, registrado por AppRoot con DontDestroyOnLoad)
    /// y suscripciones de objetos persistentes que deben sobrevivir el cambio de escena. Los
    /// subscribers scene-scoped se desuscriben en su propio OnDisable; aqui solo desregistramos
    /// los servicios que registramos en Awake.
    /// </summary>
    public static void ResetStaticState()
    {
        Instance = null;
        ServiceLocator.Unregister<ISpawnService>();
        ServiceLocator.Unregister<IAuthorityContext>();
        ServiceLocator.Unregister<IWorldQueryService>();
    }

    void OnDestroy()
    {
        // CRITICO para el loop (rematch/lobby): al recargar la escena, el Awake de la
        // NUEVA GameManager corre ANTES de este OnDestroy y ya re-registro servicios y
        // re-suscribio eventos. Si limpiaramos incondicionalmente, borrariamos ese estado
        // nuevo -> la partida recargada queda injugable. Solo limpiamos si seguimos siendo
        // la instancia activa (no fuimos reemplazados).
        if (Instance != this) return;
        ResetStaticState();
    }

    void Start()
    {
        // MatchConfig sobrevive al reload de escena (rematch/lobby). Si esta configurado,
        // arrancamos directo con el rol elegido; si no, mostramos el lobby.
        if (MatchConfig.Configured)
        {
            // Entrada desde el Meta (JUGAR): el rol se asigna AL AZAR. Se persiste en MatchConfig
            // para coherencia: el rematch es in-place (no re-corre Start) y reusa este mismo rol.
            playerTeam = Random.value < 0.5f ? CharacterTeam.Hunter : CharacterTeam.Prey;
            MatchConfig.PlayerTeam = playerTeam;
            States.ChangeState(new StartingState());
        }
        else if (autoStart)
        {
            States.ChangeState(new StartingState());
        }
        else
        {
            States.ChangeState(new LobbyState());
        }
    }

    /// <summary>Llamado por el LobbyPanel al elegir rol. Fija MatchConfig y arranca la partida.</summary>
    public void StartMatch(CharacterTeam team)
    {
        MatchConfig.PlayerTeam = team;
        MatchConfig.Configured = true;
        playerTeam = team;
        States.ChangeState(new StartingState());
    }

    /// <summary>Rejugar con el mismo rol. Reset IN-PLACE (sin recargar escena) + nueva partida.</summary>
    public void Rematch()
    {
        MatchConfig.Configured = true; // coherencia; el reset in-place no recarga la escena
        ResetMatch();
        States.ChangeState(new StartingState());
    }

    /// <summary>Volver al lobby para re-elegir rol. Reset IN-PLACE + LobbyState.</summary>
    public void ReturnToLobby()
    {
        MatchConfig.Configured = false;
        ResetMatch();
        States.ChangeState(new LobbyState());
    }

    /// <summary>
    /// Reset IN-PLACE de la partida SIN recargar la escena. Recargar la escena activa
    /// (SceneManager.LoadScene de la propia escena) resultaba inestable: dejaba la partida
    /// sin iniciar / injugable. Asi mantenemos managers, UI y suscripciones vivos (no se
    /// toca EventBus/ServiceLocator) y solo reciclamos las entidades de la partida.
    /// </summary>
    void ResetMatch()
    {
        DespawnList(Hunters);
        DespawnList(Preys);
        Hunters.Clear();
        Preys.Clear();

        // Objetos de gameplay residuales (se recrean en la nueva partida).
        DespawnAllOfType<Projectile>();
        DespawnAllOfType<FearProjectile>();
        DespawnAllOfType<RemnantDecoy>();

        TimeRemaining = 0f;
    }

    static void DespawnList(List<Character> list)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null) Destroy(list[i].gameObject);
    }

    static void DespawnAllOfType<T>() where T : Component
    {
        var arr = FindObjectsByType<T>(FindObjectsInactive.Exclude);
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) Destroy(arr[i].gameObject);
    }

    void Update()
    {
        States.Tick(Time.deltaTime);
    }

    /// <summary>Spawna jugador + bots segun la composicion configurada.</summary>
    public void SpawnMatchEntities()
    {
        Hunters.Clear();
        Preys.Clear();

        bool playerIsHunter = playerTeam == CharacterTeam.Hunter;

        // Hunter pool
        for (int i = 0; i < huntersTotal; i++)
        {
            bool isPlayer = playerIsHunter && i == 0;
            SpawnOne(CharacterTeam.Hunter, i, isPlayer);
        }

        // Prey pool
        for (int i = 0; i < preysTotal; i++)
        {
            bool isPlayer = !playerIsHunter && i == 0;
            SpawnOne(CharacterTeam.Prey, i, isPlayer);
        }
    }

    void SpawnOne(CharacterTeam team, int index, bool isPlayer)
    {
        GameObject prefab = team == CharacterTeam.Hunter ? hunterPrefab : preyPrefab;
        if (prefab == null) { Debug.LogError($"[GameManager] Falta prefab de {team}"); return; }

        var spawns = team == CharacterTeam.Hunter ? hunterSpawns : preySpawns;
        Vector3 pos = (spawns != null && spawns.Length > 0)
            ? spawns[index % spawns.Length].position
            : Vector3.zero;

        var go = Instantiate(prefab, pos, Quaternion.identity);
        go.name = $"{team}{index}{(isPlayer ? "_Player" : "_Bot")}";
        var character = go.GetComponent<Character>();
        if (character == null) { Debug.LogError($"[GameManager] Prefab {prefab.name} sin Character"); Destroy(go); return; }

        // Aplica la skin equipada (loadout persistente del Meta) al hijo "Visual". Las skins NO
        // afectan gameplay: solo intercambian el RuntimeAnimatorController. Ver IProfileService.
        ApplyEquippedSkin(go, team);

        // Brain + AI components
        IBrain brain;
        if (isPlayer)
        {
            var pb = go.GetComponent<PlayerBrain>() ?? go.AddComponent<PlayerBrain>();
            brain = pb;
            // Indicador visual de aim (linea/circulo/cono) — solo player local.
            // Lo agregamos aqui en runtime para no depender de su presencia en el prefab.
            if (go.GetComponent<AbilityIndicatorView>() == null)
                go.AddComponent<AbilityIndicatorView>();
        }
        else
        {
            EnsureAIComponents(go);
            var bb = go.GetComponent<BotBrain>() ?? go.AddComponent<BotBrain>();
            brain = bb;
        }
        character.SetBrain(brain);
        character.SetAuthority(LocalAuthority.Instance);

        if (team == CharacterTeam.Hunter) Hunters.Add(character);
        else                              Preys.Add(character);

        EventBus.Publish(new CharacterSpawnedEvent { Character = character });
    }

    /// <summary>
    /// Intercambia el RuntimeAnimatorController del Animator del personaje por el de la skin
    /// equipada (rol correspondiente), leida de IProfileService. Como CharacterAnimator cachea
    /// los hashes de estado en su Awake (ya corrido al Instantiate), se vuelve a resolver la tabla
    /// con RebuildStateTable tras el swap. Si no hay servicio/skin/controller, no toca nada.
    /// </summary>
    void ApplyEquippedSkin(GameObject go, CharacterTeam team)
    {
        var profile = ServiceLocator.Resolve<IProfileService>();
        if (profile == null) return;

        var skin = profile.GetEquippedSkin(team);
        if (skin == null || skin.animatorController == null) return;

        var animator = go.GetComponentInChildren<Animator>();
        if (animator == null) return;
        animator.runtimeAnimatorController = skin.animatorController;

        var charAnim = go.GetComponent<CharacterAnimator>();
        if (charAnim != null) charAnim.RebuildStateTable();
    }

    /// <summary>
    /// Asegura que el GO bot tenga los componentes minimos para AI (Seeker, AIPath,
    /// BotLocomotion). Idealmente el prefab ya los trae bakeados; este metodo es
    /// fallback. Los valores de AIPath (maxSpeed/endReachedDistance/slowdownDistance)
    /// vienen del prefab o de los defaults de AIPath — NO los sobreescribimos para
    /// permitir tuning desde inspector. Las flags estructurales (updatePosition,
    /// canMove, orientation, gravity) las setea BotLocomotion.Awake.
    /// </summary>
    void EnsureAIComponents(GameObject go)
    {
        bool anyAdded = false;
#if ASTAR_EXISTS
        if (go.GetComponent<Pathfinding.Seeker>() == null)
        {
            go.AddComponent<Pathfinding.Seeker>();
            anyAdded = true;
        }
        if (go.GetComponent<Pathfinding.AIPath>() == null)
        {
            go.AddComponent<Pathfinding.AIPath>();
            anyAdded = true;
        }
#endif
        if (go.GetComponent<BotLocomotion>() == null)
        {
            go.AddComponent<BotLocomotion>();
            anyAdded = true;
        }
        if (anyAdded)
        {
            Debug.LogWarning($"[GameManager] {go.name}: componentes de AI agregados en runtime. " +
                             "Bakea el prefab via 'Tools > Chase Game > Bake AI Components Into Selected Prefab' " +
                             "para tunear AIPath desde el inspector.", go);
        }
    }
}
