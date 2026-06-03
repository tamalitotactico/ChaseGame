using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Panel de diagnostico runtime. Toggle con F1 (teclado) o triple-tap (mobile).
/// Muestra el estado completo del pipeline de movimiento, input y sistemas
/// criticos. Sirve para detectar problemas multiplataforma sin depender de
/// breakpoints o adb logcat.
///
/// Uso:
///   - Arrastra este script a un GameObject vacio en la escena de gameplay,
///     o llama DiagnosticsPanel.SpawnAtRuntime() desde algun bootstrap.
///   - F1 (PC) o triple-tap en la esquina superior izquierda (mobile) para
///     mostrar/ocultar.
///
/// El panel NO afecta gameplay: solo lee estado via reflection-free APIs.
/// </summary>
public class RuntimeDiagnosticsPanel : MonoBehaviour
{
    public static RuntimeDiagnosticsPanel Instance { get; private set; }

    [Header("Toggle")]
    [SerializeField] Key toggleKey = Key.F1;
    [SerializeField] bool showOnStart = false;

    bool   _show;
    GUIStyle _box;
    GUIStyle _label;
    GUIStyle _header;
    Texture2D _bgTex; // fondo 1x1 del _box; se destruye en OnDestroy para no filtrarlo

    // Mobile triple-tap state
    int   _tapCount;
    float _tapWindowEnd;

    /// <summary>Spawna el panel en runtime sin necesidad de tenerlo en la escena.</summary>
    public static void SpawnAtRuntime()
    {
        if (Instance != null) return;
        var go = new GameObject("RuntimeDiagnosticsPanel");
        DontDestroyOnLoad(go);
        go.AddComponent<RuntimeDiagnosticsPanel>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _show    = showOnStart;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_bgTex != null) { Destroy(_bgTex); _bgTex = null; }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            _show = !_show;

        if (Touchscreen.current != null)
            DetectTripleTap();
    }

    void DetectTripleTap()
    {
        var t = Touchscreen.current.primaryTouch;
        if (!t.press.wasPressedThisFrame) return;

        // Solo cuenta tap en la esquina superior izquierda (1/4 del ancho, 1/4 del alto desde el top).
        Vector2 p = t.position.ReadValue();
        if (p.x > Screen.width * 0.25f || p.y < Screen.height * 0.75f) { _tapCount = 0; return; }

        if (Time.realtimeSinceStartup > _tapWindowEnd) _tapCount = 0;
        _tapCount++;
        _tapWindowEnd = Time.realtimeSinceStartup + 0.6f;
        if (_tapCount >= 3) { _show = !_show; _tapCount = 0; }
    }

    void OnGUI()
    {
        if (!_show) return;
        EnsureStyles();

        var sb = new StringBuilder(1024);
        AppendHeader(sb);
        AppendSection(sb, "Platform & Input", BuildPlatformAndInput);
        AppendSection(sb, "Player",           BuildPlayer);
        AppendSection(sb, "Bots",             BuildBots);
        AppendSection(sb, "Match & Systems",  BuildSystems);
        AppendSection(sb, "Astar",            BuildAstar);

        float w = Mathf.Min(Screen.width * 0.45f, 520f);
        float h = Screen.height - 40f;
        GUI.Box(new Rect(10, 10, w, h), GUIContent.none, _box);
        GUI.Label(new Rect(20, 20, w - 20, h - 20), sb.ToString(), _label);
    }

    // -------- section builders --------

    void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine("<b>RUNTIME DIAGNOSTICS</b>  toggle=" + toggleKey + " or triple-tap TL");
        sb.AppendLine($"frame={Time.frameCount}  t={Time.time:F1}s  dt={Time.deltaTime * 1000f:F1}ms");
        sb.AppendLine();
    }

    void AppendSection(StringBuilder sb, string title, System.Action<StringBuilder> build)
    {
        sb.AppendLine("<b>== " + title + " ==</b>");
        try { build(sb); } catch (System.Exception ex) { sb.AppendLine("ERR: " + ex.Message); }
        sb.AppendLine();
    }

    void BuildPlatformAndInput(StringBuilder sb)
    {
        sb.AppendLine($"platform={Application.platform}");
        sb.AppendLine($"isEditor={Application.isEditor}  isMobile={Application.isMobilePlatform}");
        sb.AppendLine($"Keyboard.current={(Keyboard.current != null ? "yes" : "NULL")}");
        sb.AppendLine($"Mouse.current={(Mouse.current != null ? "yes" : "NULL")}");
        sb.AppendLine($"Touchscreen.current={(Touchscreen.current != null ? "yes" : "NULL")}");
        sb.AppendLine($"Gamepad.current={(Gamepad.current != null ? "yes" : "NULL")}");

        var him = Object.FindAnyObjectByType<HybridInputManager>();
        if (him != null)
        {
            Vector2 v = him.GetMovementInput();
            sb.AppendLine($"HIM.move=({v.x:F2},{v.y:F2}) active={him.IsActive}");
        }
        else sb.AppendLine("HIM=NULL (player will not receive WASD/joystick input)");

        var vj = Object.FindAnyObjectByType<VirtualJoystick>();
        sb.AppendLine($"VirtualJoystick={(vj != null ? "yes" : "NULL")}");
    }

    void BuildPlayer(StringBuilder sb)
    {
        var gm = GameManager.Instance;
        if (gm == null) { sb.AppendLine("GameManager=NULL"); return; }

        Character player = null;
        foreach (var c in gm.Hunters)
            if (c != null && c.GetComponent<PlayerBrain>() != null) { player = c; break; }
        if (player == null)
            foreach (var c in gm.Preys)
                if (c != null && c.GetComponent<PlayerBrain>() != null) { player = c; break; }

        if (player == null) { sb.AppendLine("no Character with PlayerBrain"); return; }

        var rb = player.GetComponent<Rigidbody2D>();
        sb.AppendLine($"name={player.name}  team={player.Team}");
        sb.AppendLine($"pos={player.transform.position.ToString("F2")}");
        sb.AppendLine($"alive={player.IsAlive} downed={player.IsDowned}");
        sb.AppendLine($"authority.CanSimulate={player.Authority.CanSimulate}");
        sb.AppendLine($"motor maxSpeed={player.Motor.MaxSpeed:F2} mult={player.Motor.SpeedMultiplier:F2}");
        sb.AppendLine($"RB vel={rb.linearVelocity.ToString("F2")} bodyType={rb.bodyType} sim={rb.simulated}");
        sb.AppendLine($"RB constraints={rb.constraints}");
        if (player.StatusEffects != null)
            sb.AppendLine($"statusFX CanAct={player.StatusEffects.CanAct}");
    }

    void BuildBots(StringBuilder sb)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        int n = 0;
        for (int i = 0; i < gm.Hunters.Count; i++) AppendBot(sb, gm.Hunters[i], ref n);
        for (int i = 0; i < gm.Preys.Count;   i++) AppendBot(sb, gm.Preys[i],   ref n);
        if (n == 0) sb.AppendLine("no bots");
    }

    void AppendBot(StringBuilder sb, Character c, ref int n)
    {
        if (c == null) return;
        var bb = c.GetComponent<BotBrain>();
        if (bb == null) return;
        n++;
        var rb = c.GetComponent<Rigidbody2D>();
        var loco = c.GetComponent<BotLocomotion>();
        sb.AppendLine($"[{n}] {c.name} state={bb.FSM?.Current?.GetType().Name}");
        sb.AppendLine($"    pos={c.transform.position.ToString("F2")} vel={rb.linearVelocity.ToString("F2")}");
        if (loco != null)
            sb.AppendLine($"    steerDir={loco.GetSteeringDirection().ToString("F2")} wallLayer={loco.WallLayer.value}");
    }

    void BuildSystems(StringBuilder sb)
    {
        var gm = GameManager.Instance;
        if (gm == null) { sb.AppendLine("GameManager=NULL"); return; }
        sb.AppendLine($"matchState={gm.States?.Current?.GetType().Name}");
        sb.AppendLine($"hunters={gm.Hunters.Count} preys={gm.Preys.Count}");
        sb.AppendLine($"aliveHunters={gm.AliveHuntersCount} alivePreys={gm.AlivePreysCount} activePreys={gm.ActivePreysCount}");
        sb.AppendLine($"timeRemaining={gm.TimeRemaining:F1}s");
        var fow = FogOfWarManager.Instance;
        sb.AppendLine($"FoW={(fow != null ? "yes" : "NULL")}");
    }

    void BuildAstar(StringBuilder sb)
    {
#if ASTAR_EXISTS
        sb.AppendLine("ASTAR_EXISTS=DEFINED");
        // Check AstarPath via reflection: evitamos dependencia directa con su
        // assembly desde Diagnostics (que vive en Assembly-CSharp).
        var t = System.Type.GetType("Pathfinding.AstarPath, AstarPathfindingProject")
                ?? System.Type.GetType("Pathfinding.AstarPath");
        if (t != null)
        {
            var f = t.GetField("active", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var ap = f != null ? f.GetValue(null) : null;
            sb.AppendLine("AstarPath.active=" + (ap != null ? "yes" : "NULL"));
        }
        else sb.AppendLine("Pathfinding.AstarPath type not found");
#else
        sb.AppendLine("<color=red>ASTAR_EXISTS NOT DEFINED — bots usan fallback directo</color>");
        sb.AppendLine("Fix: Tools > Chase Game > Validate A* Defines Across Platforms");
#endif
    }

    void EnsureStyles()
    {
        if (_box == null)
        {
            _box = new GUIStyle(GUI.skin.box);
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, new Color(0, 0, 0, 0.85f));
            _bgTex.Apply();
            _box.normal.background = _bgTex;
        }
        if (_label == null)
        {
            _label = new GUIStyle(GUI.skin.label);
            _label.fontSize  = 12;
            _label.richText  = true;
            _label.wordWrap  = false;
            _label.normal.textColor = Color.white;
        }
    }
}
