using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Asegura que el scripting define ASTAR_EXISTS este aplicado en TODAS las
/// plataformas que el proyecto soporta, no solo en Standalone.
///
/// Por que existe: el instalador de A* Pathfinding define ASTAR_EXISTS solo en
/// el grupo Standalone. Al cambiar al Build Profile Android (o cualquier otro),
/// el define desaparece, lo que strippea todos los #if ASTAR_EXISTS del codigo
/// (incluyendo BotLocomotion.SetDestination / GetSteeringDirection). Resultado:
/// los bots quedan inmoviles porque GetSteeringDirection devuelve Vector2.zero
/// del fallback #else.
///
/// Este script corre en cada recarga del editor y al cambiar de Build Profile,
/// y vuelve a aplicar el define a todas las plataformas si la carpeta de A*
/// existe. Tambien expone un MenuItem para forzar el sync manualmente.
///
/// IMPORTANTE: Hay que mantener este script tras un upgrade de A* — el upgrade
/// puede sobrescribir defines.
/// </summary>
[InitializeOnLoad]
public static class AstarDefineSync
{
    const string DEFINE     = "ASTAR_EXISTS";
    const string ASTAR_PATH = "Assets/AstarPathfindingProject";

    static AstarDefineSync()
    {
        // delayCall: ejecutar despues de que el editor inicialice — evita
        // race conditions con PlayerSettings durante el dominio reload.
        EditorApplication.delayCall += Sync;
    }

    /// <summary>Re-sync cuando cambia el active build target (cambio de Build Profile).
    /// Unity descubre esta interfaz automaticamente, sin necesidad de suscribirse al
    /// evento `activeBuildTargetChanged` (deprecado).</summary>
    class BuildTargetWatcher : IActiveBuildTargetChanged
    {
        public int callbackOrder => 0;
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget) => Sync();
    }

    [MenuItem("Tools/Chase Game/Validate A* Defines Across Platforms")]
    public static void SyncMenu() => Sync();

    static void Sync()
    {
        if (!Directory.Exists(ASTAR_PATH)) return;

        // Iteramos las plataformas que el proyecto realmente targetea. Si en el
        // futuro se agregan iOS, WebGL u otros, ampliar este arreglo.
        var targets = new[]
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.WebGL,
        };

        bool changed = false;
        foreach (var t in targets)
        {
            string defs = PlayerSettings.GetScriptingDefineSymbols(t);
            if (HasDefine(defs, DEFINE)) continue;
            string updated = string.IsNullOrEmpty(defs) ? DEFINE : defs + ";" + DEFINE;
            PlayerSettings.SetScriptingDefineSymbols(t, updated);
            Debug.Log($"[AstarDefineSync] Aplicado ASTAR_EXISTS a {t}");
            changed = true;
        }

        if (changed)
            Debug.LogWarning("[AstarDefineSync] Defines actualizados — el editor recompilara.");
    }

    static bool HasDefine(string defs, string token)
    {
        if (string.IsNullOrEmpty(defs)) return false;
        foreach (var d in defs.Split(';'))
            if (d.Trim() == token) return true;
        return false;
    }
}
