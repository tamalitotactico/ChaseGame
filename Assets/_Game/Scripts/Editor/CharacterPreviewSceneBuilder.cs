using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ASTAR_EXISTS
using Pathfinding;
#endif

/// <summary>
/// Construye la escena de preview de personajes (00_CharacterPreview.unity) cableada:
/// camara ortografica + AudioListener, luz global 2D (URP), piso, AudioManager y el
/// CharacterPreviewRig con los prefabs base ya asignados.
///
/// Ejecutar: Tools > Chase Game > Build Character Preview Scene. Idempotente (sobreescribe).
/// </summary>
public static class CharacterPreviewSceneBuilder
{
    const string SCENE_PATH = "Assets/_Game/Scenes/00_CharacterPreview.unity";

    [MenuItem("Tools/Chase Game/Build Character Preview Scene")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().isDirty &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camara + listener
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.16f, 0.17f, 0.20f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();

        // Luz global 2D (sin esto los sprites Lit se ven negros en URP 2D)
        var lightGo = new GameObject("Global Light 2D");
        var light2d = lightGo.AddComponent<Light2D>();
        light2d.lightType = Light2D.LightType.Global;
        light2d.intensity = 1f;

        // Piso de referencia (opcional)
        var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Floor.prefab");
        if (floorPrefab != null)
        {
            var floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab);
            floor.transform.position   = Vector3.zero;
            floor.transform.localScale = new Vector3(20f, 14f, 1f);
            floor.name = "Floor";
        }

        // Audio (se auto-registra como IAudioService en Awake)
        new GameObject("AudioManager").AddComponent<AudioManager>();

        // Rig cableado
        var rigGo = new GameObject("CharacterPreviewRig");
        var rig = rigGo.AddComponent<CharacterPreviewRig>();
        rig.hunterPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Hunter.prefab");
        rig.preyPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Prey.prefab");
        rig.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Game/ScriptableObjects/Characters/HunterData.asset");

        if (rig.hunterPrefab == null || rig.preyPrefab == null)
            Debug.LogWarning("[CharacterPreview] No se encontraron Hunter.prefab/Prey.prefab; asignalos a mano en el rig.");

        BuildAstarGraph();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log($"[CharacterPreview] Escena creada en {SCENE_PATH}. Entra en Play y usa el inspector del CharacterPreviewRig.");
    }

    /// <summary>
    /// Coloca el helper PreviewAstarGraph (+ AstarPath via RequireComponent) en la escena. El grafo
    /// 2D (muros = no-caminable) se configura y escanea en Play (PreviewAstarGraph.Start), porque
    /// AstarPath.data solo esta inicializado en runtime. Asi Smell y el Ghost Wolf rodean muros aca.
    /// </summary>
    static void BuildAstarGraph()
    {
#if ASTAR_EXISTS
        if (Object.FindObjectOfType<PreviewAstarGraph>() != null) return;
        var go = new GameObject("A*");
        go.AddComponent<PreviewAstarGraph>(); // RequireComponent agrega AstarPath
        var astar = go.GetComponent<AstarPath>();
        if (astar != null) astar.scanOnStartup = true;
#else
        Debug.LogWarning("[CharacterPreview] ASTAR_EXISTS no definido; sin grafo A* en la escena de preview.");
#endif
    }
}
