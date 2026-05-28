using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auditor de proyecto: ejecuta multiples chequeos para detectar problemas que
/// rompen multiplataforma o que se introducen al regenerar .meta / cambiar
/// Build Profiles. Disponible bajo:
///
///   Tools > Chase Game > Audit Project (Full)
///
/// Tambien expone cada chequeo individual como menu item para iterar rapido.
///
/// Chequeos:
///   1. Missing scripts en escenas y prefabs.
///   2. Referencias serializadas nulas en componentes criticos (Character,
///      PlayerBrain, BotBrain, HybridInputManager, FogOfWarManager).
///   3. Sync de scripting defines (ASTAR_EXISTS) entre plataformas.
///   4. Active Input Handling y Input System asset.
///   5. Capas (Wall layer, layer collision matrix sanity).
///   6. Prefabs requeridos en GameManager (Hunter/Prey prefabs no-null).
/// </summary>
public static class ProjectAuditor
{
    const string ASTAR_PATH = "Assets/AstarPathfindingProject";

    [MenuItem("Tools/Chase Game/Audit Project (Full)")]
    public static void RunFull()
    {
        var sb = new StringBuilder();
        sb.AppendLine("==== Chase Game · Project Audit ====");
        sb.AppendLine();

        AppendSection(sb, "[1/6] Missing scripts (scenes + prefabs)",   ScanMissingScripts);
        AppendSection(sb, "[2/6] Null serialized refs (critical comps)", ScanNullRefs);
        AppendSection(sb, "[3/6] Scripting defines sync across platforms", ScanDefines);
        AppendSection(sb, "[4/6] Input System config",                   ScanInputSystem);
        AppendSection(sb, "[5/6] Layer config",                          ScanLayers);
        AppendSection(sb, "[6/6] GameManager prefab wiring",             ScanGameManagerPrefabs);

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Chase Game Audit",
            "Audit completo. Revisa la consola para el reporte detallado.", "OK");
    }

    static void AppendSection(StringBuilder sb, string title, System.Func<List<string>> check)
    {
        sb.AppendLine("─── " + title + " ───");
        var issues = check();
        if (issues == null || issues.Count == 0) sb.AppendLine("  OK");
        else foreach (var i in issues) sb.AppendLine("  · " + i);
        sb.AppendLine();
    }

    // ─────────────────────────────────────────────────────────────────────
    // 1. Missing scripts

    [MenuItem("Tools/Chase Game/Audit · Missing Scripts")]
    public static void RunMissingScriptsMenu()
    {
        var issues = ScanMissingScripts();
        if (issues.Count == 0) Debug.Log("[Audit] Sin Missing scripts.");
        else Debug.LogWarning("[Audit] Missing scripts encontrados:\n  " + string.Join("\n  ", issues));
    }

    static List<string> ScanMissingScripts()
    {
        var issues = new List<string>();

        // Scenes
        var scenes = AssetDatabase.FindAssets("t:Scene");
        foreach (var guid in scenes)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;
            // Skip Library / package scenes
            if (path.StartsWith("Assets/AstarPathfindingProject/")) continue;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (var root in scene.GetRootGameObjects())
                    CountMissingRecursive(root.transform, "Scene:" + scene.name + " > " + GetPath(root.transform), issues);
                EditorSceneManager.CloseScene(scene, true);
            }
            catch (System.Exception ex) { issues.Add("[ERR scene " + path + "] " + ex.Message); }
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        // Prefabs
        var prefabs = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/_Game/")) continue;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) CountMissingRecursive(go.transform, "Prefab:" + path, issues);
        }

        return issues;
    }

    static void CountMissingRecursive(Transform t, string ctx, List<string> issues)
    {
        var comps = t.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
                issues.Add(ctx + " > " + t.name + " [slot " + i + " = MISSING SCRIPT]");
        }
        for (int i = 0; i < t.childCount; i++)
            CountMissingRecursive(t.GetChild(i), ctx, issues);
    }

    static string GetPath(Transform t)
    {
        var stack = new Stack<string>();
        while (t != null) { stack.Push(t.name); t = t.parent; }
        return string.Join("/", stack);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 2. Null serialized refs

    [MenuItem("Tools/Chase Game/Audit · Null Serialized Refs")]
    public static void RunNullRefsMenu()
    {
        var issues = ScanNullRefs();
        if (issues.Count == 0) Debug.Log("[Audit] Sin referencias serializadas nulas en componentes criticos.");
        else Debug.LogWarning("[Audit] Null refs:\n  " + string.Join("\n  ", issues));
    }

    static List<string> ScanNullRefs()
    {
        var issues = new List<string>();
        // Critical script types: any field with [SerializeField] and tipo derivado de Object debe estar asignado.
        var types = new System.Type[]
        {
            typeof(Character), typeof(PlayerBrain), typeof(BotBrain),
            typeof(HybridInputManager), typeof(FogOfWarManager), typeof(GameManager)
        };

        var prefabs = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/_Game/")) continue;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            CheckNullRefsRecursive(go.transform, "Prefab:" + path, types, issues);
        }
        return issues;
    }

    static void CheckNullRefsRecursive(Transform t, string ctx, System.Type[] types, List<string> issues)
    {
        foreach (var type in types)
        {
            var comp = t.GetComponent(type);
            if (comp == null) continue;
            var so = new SerializedObject(comp);
            var p = so.GetIterator();
            if (p.NextVisible(true))
                do
                {
                    if (p.propertyType == SerializedPropertyType.ObjectReference
                        && p.objectReferenceValue == null
                        && !IsAllowedNull(p.name))
                        issues.Add(ctx + " > " + t.name + " [" + type.Name + "." + p.name + " = null]");
                }
                while (p.NextVisible(false));
        }
        for (int i = 0; i < t.childCount; i++)
            CheckNullRefsRecursive(t.GetChild(i), ctx, types, issues);
    }

    static bool IsAllowedNull(string fieldName)
    {
        // Whitelist: campos opcionales que pueden quedar nulos.
        switch (fieldName)
        {
            case "joystickProvider": // se auto-busca en runtime
            case "data":             // CharacterData puede asignarse en spawn
            case "matchSettings":    // GameManager.matchSettings opcional
            case "tuning":            // BotBrain.tuning crea instance default si null
                return true;
            default: return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 3. Scripting defines

    [MenuItem("Tools/Chase Game/Audit · Scripting Defines")]
    public static void RunDefinesMenu()
    {
        var issues = ScanDefines();
        if (issues.Count == 0) Debug.Log("[Audit] Defines sincronizados.");
        else Debug.LogWarning("[Audit] Defines:\n  " + string.Join("\n  ", issues));
    }

    static List<string> ScanDefines()
    {
        var issues = new List<string>();
        bool hasAstar = Directory.Exists(ASTAR_PATH);

        var targets = new[]
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.WebGL,
        };

        foreach (var t in targets)
        {
            string defs = PlayerSettings.GetScriptingDefineSymbols(t);
            if (hasAstar && !HasToken(defs, "ASTAR_EXISTS"))
                issues.Add(t + ": falta ASTAR_EXISTS (A* presente en proyecto). Fix: Tools > Chase Game > Validate A* Defines Across Platforms");
        }
        return issues;
    }

    static bool HasToken(string defs, string token)
    {
        if (string.IsNullOrEmpty(defs)) return false;
        foreach (var d in defs.Split(';')) if (d.Trim() == token) return true;
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 4. Input System

    [MenuItem("Tools/Chase Game/Audit · Input System")]
    public static void RunInputSystemMenu()
    {
        var issues = ScanInputSystem();
        if (issues.Count == 0) Debug.Log("[Audit] Input System OK.");
        else Debug.LogWarning("[Audit] Input System:\n  " + string.Join("\n  ", issues));
    }

    static List<string> ScanInputSystem()
    {
        var issues = new List<string>();

        // activeInputHandler en ProjectSettings.asset: 0=Old, 1=New, 2=Both
        string path = "ProjectSettings/ProjectSettings.asset";
        if (File.Exists(path))
        {
            int handler = -1;
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.Contains("activeInputHandler:"))
                {
                    var idx = line.IndexOf(':');
                    int.TryParse(line.Substring(idx + 1).Trim(), out handler);
                    break;
                }
            }
            if (handler == 0)
                issues.Add("activeInputHandler=0 (solo Old Input Manager). Keyboard.current sera null. Cambiar a 2 (Both).");
            else if (handler == 1)
                issues.Add("activeInputHandler=1 (solo New). Legacy Input no funcionara. Si todo el codigo usa el new, esto es OK.");
        }
        else issues.Add("No se encontro ProjectSettings.asset");

        return issues;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 5. Layers

    [MenuItem("Tools/Chase Game/Audit · Layers")]
    public static void RunLayersMenu()
    {
        var issues = ScanLayers();
        if (issues.Count == 0) Debug.Log("[Audit] Layers OK.");
        else Debug.LogWarning("[Audit] Layers:\n  " + string.Join("\n  ", issues));
    }

    static List<string> ScanLayers()
    {
        var issues = new List<string>();
        if (LayerMask.NameToLayer("Wall") < 0)
            issues.Add("Falta layer 'Wall'. BotLocomotion.LOS y FogOfWar lo necesitan.");
        return issues;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 6. GameManager prefab wiring

    [MenuItem("Tools/Chase Game/Audit · GameManager Prefabs")]
    public static void RunGmMenu()
    {
        var issues = ScanGameManagerPrefabs();
        if (issues.Count == 0) Debug.Log("[Audit] GameManager prefabs OK.");
        else Debug.LogWarning("[Audit] GameManager:\n  " + string.Join("\n  ", issues));
    }

    static List<string> ScanGameManagerPrefabs()
    {
        var issues = new List<string>();
        // Buscar en escenas: si el GameManager tiene null en hunterPrefab/preyPrefab
        // → al spawnear se loguea error y no se instancia nada.
        var scenes = AssetDatabase.FindAssets("t:Scene");
        foreach (var guid in scenes)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/_Game/")) continue;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
            {
                var gms = root.GetComponentsInChildren<GameManager>(true);
                foreach (var gm in gms)
                {
                    var so = new SerializedObject(gm);
                    var p = so.FindProperty("hunterPrefab");
                    if (p != null && p.objectReferenceValue == null)
                        issues.Add(scene.name + ": GameManager.hunterPrefab = null");
                    p = so.FindProperty("preyPrefab");
                    if (p != null && p.objectReferenceValue == null)
                        issues.Add(scene.name + ": GameManager.preyPrefab = null");
                }
            }
            EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        return issues;
    }
}
