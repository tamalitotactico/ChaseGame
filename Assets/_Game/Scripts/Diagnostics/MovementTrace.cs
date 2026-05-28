using UnityEngine;

/// <summary>
/// Trazado opcional del pipeline de movimiento. Activar con
/// MovementTrace.Enabled = true (o desde RuntimeDiagnosticsPanel) para imprimir
/// logs detallados de cada etapa: Character intent, Motor velocity, Brain
/// produccion, Input source, etc.
///
/// Uso desde codigo:
///   MovementTrace.Log("Character", "{0} intent=({1:F2},{2:F2})", name, mx, my);
///
/// Las llamadas son baratas cuando Enabled=false (un check booleano), y se
/// compilan completas en builds (no usa #if DEBUG) para poder activarse en
/// cualquier release si hace falta diagnosticar en dispositivo.
///
/// Para apagar logs en builds release definitivamente, agregar el define
/// CHASE_NO_TRACE en ProjectSettings y este archivo no emitira logs.
/// </summary>
public static class MovementTrace
{
    /// <summary>Master switch. Default off. Activar desde codigo o panel.</summary>
    public static bool Enabled = false;

    /// <summary>Filtro por categoria. Ej: "Character|Motor|Brain". Empty = todas.</summary>
    public static string CategoryFilter = "";

    public static void Log(string category, string fmt, params object[] args)
    {
#if !CHASE_NO_TRACE
        if (!Enabled) return;
        if (!string.IsNullOrEmpty(CategoryFilter) && !CategoryFilter.Contains(category)) return;
        Debug.Log("[" + category + "] " + string.Format(fmt, args));
#endif
    }

    public static void LogWarning(string category, string fmt, params object[] args)
    {
#if !CHASE_NO_TRACE
        if (!Enabled) return;
        if (!string.IsNullOrEmpty(CategoryFilter) && !CategoryFilter.Contains(category)) return;
        Debug.LogWarning("[" + category + "] " + string.Format(fmt, args));
#endif
    }
}
