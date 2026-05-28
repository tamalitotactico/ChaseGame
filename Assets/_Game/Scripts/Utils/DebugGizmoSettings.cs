/// <summary>
/// Toggles globales para los gizmos de debug. Cambiar en runtime via inspector
/// del primer CharacterDebugGizmos o desde codigo. Cada CharacterDebugGizmos
/// tambien tiene su propio bool para opt-out individual.
///
/// Phase 2: agregar editor window con checkboxes interactivos.
/// </summary>
public static class DebugGizmoSettings
{
    /// <summary>Toggle global. F1 lo activa/desactiva via DebugOverlayToggle.</summary>
    public static bool MasterEnabled        = true;

    public static bool ShowAttackRange      = true;
    public static bool ShowVisionRange      = true;
    public static bool ShowReviveRadius     = true;
    public static bool ShowAttackFlash      = true;
    public static bool ShowDamageFlash      = true;
    public static bool ShowAbilityCooldowns = true;
    public static bool ShowBotStateLabel    = true;
    public static bool ShowReviveBar        = true;
}
