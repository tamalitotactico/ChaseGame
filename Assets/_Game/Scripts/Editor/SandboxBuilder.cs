using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construye la escena de prueba Phase 0 (00_Sandbox.unity) desde cero, con todo
/// cableado: GameManager, Camera, Input, Canvas con HUD, spawns, walls.
///
/// Ejecutar via menu: Tools > Chase Game > Build Sandbox Scene.
/// Idempotente: si la escena ya existe la sobreescribe.
/// </summary>
public static class SandboxBuilder
{
    const string SCENE_PATH = "Assets/_Game/Scenes/00_Sandbox.unity";

    [MenuItem("Tools/Chase Game/Build Sandbox Scene")]
    public static void Build()
    {
        // 1. Confirmar
        if (!EditorUtility.DisplayDialog(
            "Build Sandbox Scene",
            "Esto crea (o sobreescribe) Assets/_Game/Scenes/00_Sandbox.unity. ¿Continuar?",
            "Si", "Cancelar"))
            return;

        // 2. Cargar assets
        var hunterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Hunter.prefab");
        var preyPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Prey.prefab");
        var wallPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Wall.prefab");
        var floorPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Floor.prefab");
        var matchSet     = AssetDatabase.LoadAssetAtPath<MatchSettings>("Assets/_Game/ScriptableObjects/Match/SandboxMatch.asset");

        if (hunterPrefab == null || preyPrefab == null || matchSet == null)
        {
            Debug.LogError("[SandboxBuilder] Faltan prefabs Hunter/Prey o SandboxMatch.asset");
            return;
        }

        // 3. Asegurar que la escena actual no tenga cambios sin guardar
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        }

        // Nueva escena vacia
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 4. Floor de fondo (20x14)
        if (floorPrefab != null)
        {
            var floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(20f, 14f, 1f);
            floor.name = "Floor";
        }

        // 5. Paredes perimetro + obstaculos internos para que A* tenga geometria interesante
        if (wallPrefab != null)
        {
            var wallsRoot = new GameObject("Walls");
            // Perimetro
            BuildWall(wallsRoot.transform, wallPrefab, new Vector3(0,  7f, 0), new Vector3(22f, 1f, 1f));
            BuildWall(wallsRoot.transform, wallPrefab, new Vector3(0, -7f, 0), new Vector3(22f, 1f, 1f));
            BuildWall(wallsRoot.transform, wallPrefab, new Vector3(-11f, 0, 0), new Vector3(1f, 14f, 1f));
            BuildWall(wallsRoot.transform, wallPrefab, new Vector3( 11f, 0, 0), new Vector3(1f, 14f, 1f));
            // Obstaculos internos (3 bloques que crean lineas de vision rotas)
            BuildWall(wallsRoot.transform, wallPrefab, new Vector3(-3f, 2.5f, 0), new Vector3(4f, 1f, 1f));
            BuildWall(wallsRoot.transform, wallPrefab, new Vector3( 3f,-2.5f, 0), new Vector3(4f, 1f, 1f));
            BuildWall(wallsRoot.transform, wallPrefab, new Vector3( 0f, 0f,   0), new Vector3(1f, 3f, 1f));
        }

        // 6. Spawn points
        var spawnsRoot = new GameObject("Spawns");
        var hunterSpawn = MakeSpawn("Hunter_Spawn", spawnsRoot.transform, new Vector3(-7f, 0f, 0f));
        var preySpawn0  = MakeSpawn("Prey_Spawn_0", spawnsRoot.transform, new Vector3( 7f, 3f, 0f));
        var preySpawn1  = MakeSpawn("Prey_Spawn_1", spawnsRoot.transform, new Vector3( 7f,-3f, 0f));

        // 7. Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<CameraFollow>();
        camGO.transform.position = new Vector3(0, 0, -10);

        // 8. Input. HybridInputManager.Awake auto-agrega KeyboardInputProvider
        // si no se le asigno uno en el inspector. Para evitar duplicados, agregamos
        // KeyboardInputProvider primero y luego cableamos el campo via SerializedObject.
        var inputGO = new GameObject("InputManager");
        var kbProvider = inputGO.AddComponent<KeyboardInputProvider>();
        var hybrid     = inputGO.AddComponent<HybridInputManager>();
        var soInput = new SerializedObject(hybrid);
        var kbProp = soInput.FindProperty("keyboardProvider");
        if (kbProp != null) kbProp.objectReferenceValue = kbProvider;
        soInput.ApplyModifiedPropertiesWithoutUndo();

        // 9. EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // 10. Canvas con HUD
        var canvasGO = BuildCanvas(out HUDController hud, out AbilityHUD ahud);

        // 11. GameManager
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        var soGM = new SerializedObject(gm);
        soGM.FindProperty("matchSettings").objectReferenceValue   = matchSet;
        soGM.FindProperty("hunterPrefab").objectReferenceValue    = hunterPrefab;
        soGM.FindProperty("preyPrefab").objectReferenceValue      = preyPrefab;
        var hSpawnsProp = soGM.FindProperty("hunterSpawns");
        hSpawnsProp.arraySize = 1;
        hSpawnsProp.GetArrayElementAtIndex(0).objectReferenceValue = hunterSpawn.transform;
        var pSpawnsProp = soGM.FindProperty("preySpawns");
        pSpawnsProp.arraySize = 2;
        pSpawnsProp.GetArrayElementAtIndex(0).objectReferenceValue = preySpawn0.transform;
        pSpawnsProp.GetArrayElementAtIndex(1).objectReferenceValue = preySpawn1.transform;
        soGM.FindProperty("playerTeam").enumValueIndex = (int)CharacterTeam.Hunter;
        soGM.FindProperty("huntersTotal").intValue = 1;
        soGM.FindProperty("preysTotal").intValue   = 2;
        soGM.FindProperty("autoStart").boolValue   = true;
        soGM.ApplyModifiedPropertiesWithoutUndo();

        // 12. AstarPath con GridGraph 2D
        SetupAstar();

        // 13. Save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log("[SandboxBuilder] Sandbox scene creada en " + SCENE_PATH);

        // 14. Asegurar que la escena este en Build Settings
        AddToBuildSettings(SCENE_PATH);
    }

    const string TUNING_DIR        = "Assets/_Game/ScriptableObjects/BotTuning";
    const string HUNTER_TUNING_PATH = TUNING_DIR + "/HunterBotTuning.asset";
    const string PREY_TUNING_PATH   = TUNING_DIR + "/PreyBotTuning.asset";

    [MenuItem("Tools/Chase Game/Create Default Bot Tunings")]
    public static void CreateDefaultBotTunings()
    {
        if (!AssetDatabase.IsValidFolder(TUNING_DIR))
        {
            System.IO.Directory.CreateDirectory(TUNING_DIR);
            AssetDatabase.Refresh();
        }

        CreateTuningIfMissing(HUNTER_TUNING_PATH);
        CreateTuningIfMissing(PREY_TUNING_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log("[SandboxBuilder] BotTunings creados/verificados en " + TUNING_DIR);
    }

    static void CreateTuningIfMissing(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<BotTuningData>(path) != null) return;
        var so = ScriptableObject.CreateInstance<BotTuningData>();
        AssetDatabase.CreateAsset(so, path);
    }

    [MenuItem("Tools/Chase Game/Bake AI Components Into Selected Prefab")]
    public static void BakeBotPrefab()
    {
        var sel = Selection.activeGameObject;
        if (sel == null)
        {
            Debug.LogError("[SandboxBuilder] Seleccione un prefab de bot (Hunter/Prey) en el Project antes de bakear.");
            return;
        }

        // Editar el contenido del prefab via PrefabUtility
        string path = AssetDatabase.GetAssetPath(sel);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
        {
            Debug.LogError("[SandboxBuilder] La seleccion no es un asset prefab. Path=" + path);
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var character = root.GetComponent<Character>();
            if (character == null)
            {
                Debug.LogError("[SandboxBuilder] El prefab no tiene Character. Cancelado.");
                return;
            }

#if ASTAR_EXISTS
            if (root.GetComponent<Pathfinding.Seeker>() == null) root.AddComponent<Pathfinding.Seeker>();
            if (root.GetComponent<Pathfinding.AIPath>() == null) root.AddComponent<Pathfinding.AIPath>();
#endif
            if (root.GetComponent<BotLocomotion>() == null) root.AddComponent<BotLocomotion>();

            var botBrain = root.GetComponent<BotBrain>();
            if (botBrain == null) botBrain = root.AddComponent<BotBrain>();

            // Asignar tuning por team
            var soBrain = new SerializedObject(botBrain);
            var tuningProp = soBrain.FindProperty("tuning");
            if (tuningProp.objectReferenceValue == null)
            {
                string tuningPath = character is HunterCharacter ? HUNTER_TUNING_PATH : PREY_TUNING_PATH;
                var tuningAsset = AssetDatabase.LoadAssetAtPath<BotTuningData>(tuningPath);
                if (tuningAsset == null)
                {
                    CreateDefaultBotTunings();
                    tuningAsset = AssetDatabase.LoadAssetAtPath<BotTuningData>(tuningPath);
                }
                tuningProp.objectReferenceValue = tuningAsset;
                soBrain.ApplyModifiedPropertiesWithoutUndo();
            }

            // Prey: agregar Revivable
            if (character is PreyCharacter && root.GetComponent<RevivableComponent>() == null)
                root.AddComponent<RevivableComponent>();

            // Gizmos debug
            if (root.GetComponent<CharacterDebugGizmos>() == null)
                root.AddComponent<CharacterDebugGizmos>();

            // Visuals in-game
            if (root.GetComponent<CharacterVisuals>() == null)
                root.AddComponent<CharacterVisuals>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[SandboxBuilder] Bakeo OK en " + path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Chase Game/Configure A* Grid (2D)")]
    public static void ConfigureAstarGrid()
    {
#if ASTAR_EXISTS
        var astar = AstarPath.active;
        if (astar == null) { Debug.LogError("[SandboxBuilder] No hay AstarPath en la escena activa."); return; }
        var grid = astar.data.gridGraph;
        if (grid == null) { Debug.LogError("[SandboxBuilder] No se encontro GridGraph en AstarPath."); return; }
        // heightCheck=true + use2D=true hace que A* use Physics.Raycast (3D) para
        // detectar obstaculos, que no detecta Collider2D. Debe ser false en 2D.
        grid.collision.heightCheck = false;
        grid.collision.use2D       = true;
        // diameter demasiado pequeno (< nodeSize) puede no cubrir el collider del muro.
        // 0.9 * nodeSize garantiza que el overlap 2D alcanza el collider.
        if (grid.collision.diameter < 0.8f)
            grid.collision.diameter = 0.9f;
        grid.erodeIterations = 1;
        astar.Scan();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[SandboxBuilder] A* grid: heightCheck=false, use2D=true, diameter=" +
                  grid.collision.diameter + ", erodeIterations=1. Re-scaneado. Guarda la escena.");
#else
        Debug.LogWarning("[SandboxBuilder] ASTAR_EXISTS no definido.");
#endif
    }

    [MenuItem("Tools/Chase Game/Add AstarPath to Current Scene")]
    public static void AddAstarToCurrentScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[SandboxBuilder] No hay escena activa.");
            return;
        }
        SetupAstar();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SandboxBuilder] AstarPath agregado y escena guardada (" + scene.name + ").");
    }

    static void SetupAstar()
    {
#if ASTAR_EXISTS
        // Eliminar Pathfinder previo si existe (idempotencia)
        var prev = GameObject.Find("Pathfinder");
        if (prev != null) Object.DestroyImmediate(prev);

        var astarGO = new GameObject("Pathfinder");
        var astar   = astarGO.AddComponent<AstarPath>();

        // En edit mode, ConfigureReferencesInternal NO corre automaticamente
        // (solo en Play). Lo llamamos a mano para inicializar colorSettings y data;
        // sin esto OnDrawGizmos lanza NullReferenceException.
        astar.ConfigureReferencesInternal();

        // Crear GridGraph en modo 2D (plano XY)
        var gg = astar.data.AddGraph(typeof(Pathfinding.GridGraph)) as Pathfinding.GridGraph;
        if (gg != null)
        {
            gg.is2D     = true; // rota a (-90, 270, 90) para alinear con XY
            gg.center   = Vector3.zero;
            gg.SetDimensions(44, 30, 0.5f);
            gg.collision.use2D       = true;
            gg.collision.diameter    = 0.9f;
            gg.collision.mask        = LayerMask.GetMask("Wall");
            gg.collision.heightCheck = false;
        }

        astar.Scan();
#else
        Debug.LogWarning("[SandboxBuilder] ASTAR_EXISTS no definido; salto setup A*.");
#endif
    }

    static void BuildWall(Transform parent, GameObject wallPrefab, Vector3 pos, Vector3 scale)
    {
        var w = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefab);
        w.transform.SetParent(parent, false);
        w.transform.position = pos;
        w.transform.localScale = scale;
    }

    static GameObject MakeSpawn(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.AddComponent<SpawnPoint>();
        return go;
    }

    static GameObject BuildCanvas(out HUDController hud, out AbilityHUD ahud)
    {
        var canvasGO = new GameObject("Canvas", typeof(RectTransform));
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // HUD root
        hud = canvasGO.AddComponent<HUDController>();

        // Hearts (3 corazones, esquina superior izquierda)
        var heartsRoot = MakeUI("Hearts", canvasGO.transform);
        SetRT(heartsRoot, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1), new Vector2(20, -20), new Vector2(180, 50));
        var heartObjs = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var h = MakeUI("Heart" + i, heartsRoot.transform);
            SetRT(h, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 55, 0), new Vector2(50, 50));
            var img = h.AddComponent<Image>();
            img.color = Color.red;
            heartObjs[i] = h;
        }

        // Timer (esquina superior centro)
        var timerGO = MakeUI("Timer", canvasGO.transform);
        SetRT(timerGO, new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0,-30), new Vector2(200, 60));
        var timerText = timerGO.AddComponent<TextMeshProUGUI>();
        timerText.text = "00:00";
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.fontSize = 36;

        // Countdown panel (centro)
        var countdownPanel = MakeUI("CountdownPanel", canvasGO.transform);
        SetRT(countdownPanel, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(300, 300));
        var cBg = countdownPanel.AddComponent<Image>();
        cBg.color = new Color(0,0,0,0.6f);
        var countdownTextGO = MakeUI("CountdownText", countdownPanel.transform);
        SetRT(countdownTextGO, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);
        var countdownText = countdownTextGO.AddComponent<TextMeshProUGUI>();
        countdownText.text = "3";
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.fontSize = 120;
        countdownText.color = Color.white;

        // Result panel (centro, oculto)
        var resultPanel = MakeUI("ResultPanel", canvasGO.transform);
        SetRT(resultPanel, new Vector2(0.2f,0.3f), new Vector2(0.8f,0.7f), new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);
        var rBg = resultPanel.AddComponent<Image>();
        rBg.color = new Color(0,0,0,0.8f);
        var resultTextGO = MakeUI("ResultText", resultPanel.transform);
        SetRT(resultTextGO, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);
        var resultText = resultTextGO.AddComponent<TextMeshProUGUI>();
        resultText.text = "";
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontSize = 60;
        resultText.color = Color.white;

        // Asignar refs al HUDController
        var soHud = new SerializedObject(hud);
        var heartsProp = soHud.FindProperty("heartIcons");
        heartsProp.arraySize = 3;
        for (int i = 0; i < 3; i++) heartsProp.GetArrayElementAtIndex(i).objectReferenceValue = heartObjs[i];
        soHud.FindProperty("timerText").objectReferenceValue       = timerText;
        soHud.FindProperty("countdownPanel").objectReferenceValue  = countdownPanel;
        soHud.FindProperty("countdownText").objectReferenceValue   = countdownText;
        soHud.FindProperty("resultPanel").objectReferenceValue     = resultPanel;
        soHud.FindProperty("resultText").objectReferenceValue      = resultText;
        soHud.ApplyModifiedPropertiesWithoutUndo();

        // AbilityHUD
        var abilityPanel = MakeUI("AbilityPanel", canvasGO.transform);
        SetRT(abilityPanel, new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(0,40), new Vector2(280, 90));
        ahud = abilityPanel.AddComponent<AbilityHUD>();

        var slotRoots = new GameObject[3];
        var slotFills = new Image[3];
        var slotLabels = new TextMeshProUGUI[3];
        string[] keys = { "Q", "E", "R" };
        float[] xs = { -90, 0, 90 };
        for (int i = 0; i < 3; i++)
        {
            var slot = MakeUI("Slot_" + keys[i], abilityPanel.transform);
            SetRT(slot, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(xs[i],0), new Vector2(80,80));
            var bg = slot.AddComponent<Image>();
            bg.color = new Color(0,0,0,0.5f);
            slot.AddComponent<Button>();

            var fillGO = MakeUI("Fill", slot.transform);
            SetRT(fillGO, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(-8,-8));
            var fill = fillGO.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillAmount = 1f;
            fill.color = new Color(1f, 0.8f, 0f, 0.7f);

            var lblGO = MakeUI("Label", slot.transform);
            SetRT(lblGO, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);
            var lbl = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text = keys[i];
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.fontSize = 28;
            lbl.color = Color.white;

            slotRoots[i]  = slot;
            slotFills[i]  = fill;
            slotLabels[i] = lbl;
        }

        var soAhud = new SerializedObject(ahud);
        var slotsProp = soAhud.FindProperty("slots");
        slotsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            var elem = slotsProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("root").objectReferenceValue         = slotRoots[i];
            elem.FindPropertyRelative("cooldownFill").objectReferenceValue = slotFills[i];
            elem.FindPropertyRelative("keyLabel").objectReferenceValue     = slotLabels[i];
        }
        soAhud.ApplyModifiedPropertiesWithoutUndo();

        // Phase 0: input por teclado (Q/E/R via PlayerBrain). Los Button siguen
        // ahi para feedback visual de tap, pero hold-aim-release necesita
        // EventTrigger PointerDown/Up. Se cablea en Phase 1 cuando se haga el
        // pase de mobile UX.

        return canvasGO;
    }

    static GameObject MakeUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void SetRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void AddToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return; // ya esta

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
        list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
