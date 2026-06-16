using System.Collections.Generic;
using UnityEngine;
#if FUSION2
using Fusion;
#endif

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

    [Tooltip("Mapeo IndicatorShape -> prefab para los indicadores de aim del jugador local.")]
    [SerializeField] IndicatorRegistry indicatorRegistry;

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

    /// <summary>Registry de indicadores de aim, para que el setup local (incl. el path de red en
    /// Character.Spawned) configure el AbilityIndicatorView del jugador local.</summary>
    public IndicatorRegistry IndicatorRegistry => indicatorRegistry;

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
        // Sesion de partida: decide la composicion (cuantos por bando + que es cada slot).
        // Hoy siempre local (Solo y multiplayer-stub). Phase 3: elegir FusionMatchSession por MatchConfig.Mode.
        ServiceLocator.Register<IMatchSession>(new LocalMatchSession());
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
        ServiceLocator.Unregister<IMatchSession>();
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
            // Entrada desde el Meta (JUGAR/MULTIJUGADOR): se respeta el rol y la composicion que el
            // jugador eligio en MatchSetup (MatchConfig). Antes se randomizaba el rol aqui; el random
            // se reactivara con matchmaking real. El rematch in-place reusa estos valores.
            playerTeam   = MatchConfig.PlayerTeam;
            huntersTotal = MatchConfig.HuntersTotal;
            preysTotal   = MatchConfig.PreysTotal;
#if FUSION2
            // Multijugador: arrancar el runner de Fusion. El spawn es host-only y event-driven
            // (OnNetworkPlayerJoined), por eso SpawnMatchEntities queda no-op en red (ver abajo).
            if (MatchConfig.Mode == MatchConfig.PlayMode.Multiplayer)
                StartNetworkedMatch();
#endif
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
            if (list[i] != null) NetDespawn.Despawn(list[i].gameObject);
    }

    static void DespawnAllOfType<T>() where T : Component
    {
        var arr = FindObjectsByType<T>(FindObjectsInactive.Exclude);
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) NetDespawn.Despawn(arr[i].gameObject);
    }

    void Update()
    {
        States.Tick(Time.deltaTime);
    }

    /// <summary>Spawna humano local + bots (y, con Fusion, humanos remotos) segun la composicion
    /// de la IMatchSession. La sesion decide que es cada slot; este metodo no sabe de red.</summary>
    public void SpawnMatchEntities()
    {
#if FUSION2
        // En red el spawn es host-only y event-driven (OnNetworkPlayerJoined + FillBots), no aqui.
        if (MatchConfig.Mode == MatchConfig.PlayMode.Multiplayer) return;
#endif
        Hunters.Clear();
        Preys.Clear();

        // Fallback a LocalMatchSession si no hubiera nadie registrado (defensivo).
        var session = ServiceLocator.Resolve<IMatchSession>() ?? new LocalMatchSession();

        // Hunter pool
        for (int i = 0; i < huntersTotal; i++)
            SpawnOne(CharacterTeam.Hunter, i, session.SlotKind(CharacterTeam.Hunter, i));

        // Prey pool
        for (int i = 0; i < preysTotal; i++)
            SpawnOne(CharacterTeam.Prey, i, session.SlotKind(CharacterTeam.Prey, i));
    }

    void SpawnOne(CharacterTeam team, int index, MatchSlotKind kind)
    {
        bool isPlayer = kind == MatchSlotKind.LocalPlayer;
        GameObject prefab = team == CharacterTeam.Hunter ? hunterPrefab : preyPrefab;
        if (prefab == null) { Debug.LogError($"[GameManager] Falta prefab de {team}"); return; }

        var spawns = team == CharacterTeam.Hunter ? hunterSpawns : preySpawns;
        Vector3 pos = (spawns != null && spawns.Length > 0)
            ? spawns[index % spawns.Length].position
            : Vector3.zero;

        // Resuelve el personaje (CharacterData) + skin del slot: el player usa el equipado (loadout
        // persistente del Meta); los bots, uno al azar de su bando. Data-driven: los 8 personajes
        // viven sobre 2 prefabs base via Character.SetData.
        ResolveLoadout(team, isPlayer, out var data, out var skin);

        var spawnService = ServiceLocator.Resolve<ISpawnService>();
        var go = spawnService != null
            ? spawnService.Spawn(prefab, pos, Quaternion.identity)
            : Instantiate(prefab, pos, Quaternion.identity);
        go.name = $"{team}{index}{(isPlayer ? "_Player" : "_Bot")}";
        var character = go.GetComponent<Character>();
        if (character == null) { Debug.LogError($"[GameManager] Prefab {prefab.name} sin Character"); Destroy(go); return; }

        // Inyecta los stats/abilities del personaje elegido sobre el prefab base.
        if (data != null) character.SetData(data);

        // Aplica la skin (visual; NO afecta gameplay: solo el RuntimeAnimatorController).
        ApplySkin(go, skin);

        // Brain + AI components segun el kind del slot.
        // LocalPlayer -> PlayerBrain + indicadores. Bot -> BotBrain. RemoteHuman -> TODO Fusion
        // (brain replicado); hoy LocalMatchSession nunca lo produce, asi que cae a bot defensivamente.
        IBrain brain;
        if (kind == MatchSlotKind.LocalPlayer)
        {
            var pb = go.GetComponent<PlayerBrain>() ?? go.AddComponent<PlayerBrain>();
            brain = pb;
            // Indicador visual de aim (linea/circulo/cono) — solo player local.
            // Lo agregamos aqui en runtime para no depender de su presencia en el prefab.
            var indicatorView = go.GetComponent<AbilityIndicatorView>();
            if (indicatorView == null) indicatorView = go.AddComponent<AbilityIndicatorView>();
            indicatorView.SetRegistry(indicatorRegistry);
        }
        else
        {
            // Bot (y, por ahora, RemoteHuman hasta que exista el brain de Fusion).
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
    /// Resuelve el CharacterData + Skin de un slot. El player usa el personaje EQUIPADO del loadout
    /// (IProfileService); los bots, uno AL AZAR de su bando del catalogo. Si no hay perfil o el
    /// personaje no tiene gameplayData, devuelve null (el prefab usa su data por defecto).
    /// </summary>
    void ResolveLoadout(CharacterTeam team, bool isPlayer, out CharacterData data, out Skin skin)
    {
        data = null; skin = null;
        var profile = ServiceLocator.Resolve<IProfileService>();
        if (profile == null) return;

        MetaCharacter mc = isPlayer
            ? profile.GetEquippedCharacter(team)
            : PickRandomCharacter(profile.Catalog, team);
        if (mc == null) return;

        data = mc.gameplayData;
        skin = isPlayer ? (profile.GetEquippedSkin(team) ?? mc.DefaultSkin) : mc.DefaultSkin;
    }

    /// <summary>Un MetaCharacter al azar del bando con gameplayData != null (para bots).</summary>
    static MetaCharacter PickRandomCharacter(MetaCatalog catalog, CharacterTeam team)
    {
        if (catalog == null) return null;
        var pool = new List<MetaCharacter>();
        foreach (var c in catalog.CharactersForRole(team))
            if (c != null && c.gameplayData != null) pool.Add(c);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    /// <summary>
    /// Intercambia el RuntimeAnimatorController del Animator por el de la skin dada. Como
    /// CharacterAnimator cachea los hashes en Awake, se reconstruye la tabla tras el swap.
    /// Si la skin o el controller son null, no toca nada (conserva el del prefab/personaje).
    /// </summary>
    void ApplySkin(GameObject go, Skin skin)
    {
        if (skin == null || skin.animatorController == null) return;

        var animator = go.GetComponentInChildren<Animator>();
        if (animator == null) return;
        animator.runtimeAnimatorController = skin.animatorController;

        var charAnim = go.GetComponent<CharacterAnimator>();
        if (charAnim != null) charAnim.RebuildStateTable();
    }

#if FUSION2
    // ===== Multijugador (Fusion, Host Mode). Spawn host-only, event-driven por OnPlayerJoined. =====

    NetworkBootstrap _bootstrap;
    bool _netBotsFilled;

    void StartNetworkedMatch()
    {
        _bootstrap = gameObject.GetComponent<NetworkBootstrap>() ?? gameObject.AddComponent<NetworkBootstrap>();
        // Slice: AutoHostOrClient sobre una sala fija; el primero es host y llena con bots.
        _ = _bootstrap.StartNetwork(GameMode.AutoHostOrClient);
    }

    /// <summary>Lo llama FusionInputCollector.OnPlayerJoined. Solo el host (StateAuthority) spawnea.</summary>
    public void OnNetworkPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || !runner.IsServer) return;

        // Registrar el spawn service de red ahora que hay runner (idempotente).
        if (!(ServiceLocator.Resolve<ISpawnService>() is FusionSpawnService))
            ServiceLocator.Register<ISpawnService>(new FusionSpawnService(runner));

        // El host (jugador local) usa su rol elegido; los clientes, el bando opuesto (slice).
        bool isLocal = player == runner.LocalPlayer;
        CharacterTeam team = isLocal ? playerTeam : Opposite(playerTeam);
        SpawnNetworkedCharacter(runner, team, player, isBot: false);

        // Llenar con bots una sola vez, tras el primer join (el host).
        if (!_netBotsFilled)
        {
            _netBotsFilled = true;
            FillBots(runner);
        }
    }

    void FillBots(NetworkRunner runner)
    {
        int hunterBots = Mathf.Max(0, huntersTotal - (playerTeam == CharacterTeam.Hunter ? 1 : 0));
        int preyBots   = Mathf.Max(0, preysTotal   - (playerTeam == CharacterTeam.Prey   ? 1 : 0));
        for (int i = 0; i < hunterBots; i++) SpawnNetworkedCharacter(runner, CharacterTeam.Hunter, PlayerRef.None, isBot: true);
        for (int i = 0; i < preyBots;   i++) SpawnNetworkedCharacter(runner, CharacterTeam.Prey,   PlayerRef.None, isBot: true);
    }

    void SpawnNetworkedCharacter(NetworkRunner runner, CharacterTeam team, PlayerRef inputAuthority, bool isBot)
    {
        GameObject prefab = team == CharacterTeam.Hunter ? hunterPrefab : preyPrefab;
        if (prefab == null) { Debug.LogError($"[GameManager] Falta prefab de {team}"); return; }

        var spawns = team == CharacterTeam.Hunter ? hunterSpawns : preySpawns;
        int idx = team == CharacterTeam.Hunter ? Hunters.Count : Preys.Count;
        Vector3 pos = (spawns != null && spawns.Length > 0) ? spawns[idx % spawns.Length].position : Vector3.zero;

        ResolveLoadout(team, !isBot, out var data, out var skin);

        // onBeforeSpawned: inyecta data/skin/flag bot ANTES de Character.Spawned (que lee NetworkIsBot).
        runner.Spawn(prefab, pos, Quaternion.identity, inputAuthority, (r, obj) =>
        {
            var c = obj.GetComponent<Character>();
            if (c == null) return;
            if (data != null) c.SetData(data);
            ApplySkin(obj.gameObject, skin);
            c.NetworkIsBot = isBot;
        });
    }

    /// <summary>Configura un bot networkeado. Lo llama Character.Spawned cuando NetworkIsBot &&
    /// HasStateAuthority (el BotBrain solo corre en el host). La authority ya la fijo Spawned.</summary>
    public void ConfigureBot(GameObject go)
    {
        EnsureAIComponents(go);
        var bb = go.GetComponent<BotBrain>() ?? go.AddComponent<BotBrain>();
        var c = go.GetComponent<Character>();
        if (c != null) c.SetBrain(bb);
    }

    static CharacterTeam Opposite(CharacterTeam t) =>
        t == CharacterTeam.Hunter ? CharacterTeam.Prey : CharacterTeam.Hunter;
#endif

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
