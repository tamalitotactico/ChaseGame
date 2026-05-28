using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Asegura que los shaders criticos del juego (FoW, etc.) esten en
/// GraphicsSettings.AlwaysIncludedShaders. Sin esto, Unity strippea shaders
/// referenciados solo via Shader.Find en builds, lo que rompe el FoW en
/// Android/iOS (RT queda vacia, fog overlay no funciona).
///
/// Corre en cada recarga del editor + un MenuItem para forzar sync.
/// </summary>
[InitializeOnLoad]
public static class AlwaysIncludedShaderSync
{
    static readonly string[] REQUIRED_SHADERS =
    {
        "Hidden/FogVisionMesh",
        "Game/FogOverlay",
    };

    static AlwaysIncludedShaderSync()
    {
        EditorApplication.delayCall += Sync;
    }

    [MenuItem("Tools/Chase Game/Validate Always Included Shaders")]
    public static void SyncMenu() => Sync();

    static void Sync()
    {
        var gs = GraphicsSettings.GetGraphicsSettings();
        if (gs == null) return;

        var so = new SerializedObject(gs);
        var list = so.FindProperty("m_AlwaysIncludedShaders");
        if (list == null) { Debug.LogWarning("[ShaderSync] m_AlwaysIncludedShaders no encontrado."); return; }

        // Set de shaders ya presentes (por GUID/instance)
        var presentNames = new HashSet<string>();
        for (int i = 0; i < list.arraySize; i++)
        {
            var s = list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if (s != null) presentNames.Add(s.name);
        }

        bool changed = false;
        foreach (var name in REQUIRED_SHADERS)
        {
            if (presentNames.Contains(name)) continue;
            var shader = Shader.Find(name);
            if (shader == null)
            {
                Debug.LogWarning($"[ShaderSync] Shader '{name}' no existe en el proyecto. Skip.");
                continue;
            }
            int idx = list.arraySize;
            list.InsertArrayElementAtIndex(idx);
            list.GetArrayElementAtIndex(idx).objectReferenceValue = shader;
            Debug.Log($"[ShaderSync] Agregado '{name}' a Always Included Shaders.");
            changed = true;
        }

        if (changed)
        {
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.LogWarning("[ShaderSync] GraphicsSettings actualizado. Los shaders estaran disponibles en builds.");
        }
    }
}
