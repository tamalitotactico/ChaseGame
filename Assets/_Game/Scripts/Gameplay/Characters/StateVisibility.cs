using UnityEngine;

/// <summary>
/// Canal de visibilidad por ESTADO (camuflaje / invisibilidad), independiente de la niebla.
/// La regla vive en UN solo lugar (este helper) para que en red solo cambie la fuente de
/// "quien es el viewer / su bando".
///
/// Regla: un character con un efecto HidesFromEnemies es visible para un viewer si
///   (viewer es el propio character) OR (viewer es aliado) OR
///   (viewer es enemigo y [invisible: nunca] / [camuflaje: distancia &lt; revealRadius, con fade]).
/// El resultado es un ALPHA [0..1] que CharacterVisuals multiplica sobre el color del sprite.
///
/// Local (Phase 0): el viewer es el jugador local (PlayerBrain.Local). En red se cambia la
/// fuente del viewer por-cliente; la regla queda intacta. Se combina con la niebla porque
/// CharacterFogVisibility toggle-a el enabled del SR por separado (efectivamente el minimo).
/// </summary>
public static class StateVisibility
{
    /// <summary>Alpha de visibilidad para el viewer LOCAL (PlayerBrain.Local). 1 si no aplica.</summary>
    public static float AlphaFor(Character target) => AlphaFor(target, LocalViewer());

    /// <summary>Alpha de visibilidad de 'target' para 'viewer'. Testeable pasando el viewer.</summary>
    public static float AlphaFor(Character target, Character viewer)
    {
        if (target == null || target.StatusEffects == null) return 1f;
        var hide = target.StatusEffects.GetHidingEffect();
        if (hide == null) return 1f;                      // no esta oculto: alpha pleno

        if (viewer == null || viewer == target) return 1f; // soy yo o sin viewer conocido
        if (viewer.Team == target.Team) return 1f;          // aliado: siempre lo ve (con shader de stealth)

        float reveal = hide.EnemyRevealRadius;
        if (reveal <= 0f) return 0f;                        // invisible total para enemigos

        // Camuflaje: fade por distancia. 1 (pegado) -> 0 (en el borde del radio).
        float d = Vector2.Distance(viewer.transform.position, target.transform.position);
        return Mathf.Clamp01(1f - d / reveal);
    }

    /// <summary>
    /// True si 'viewer' puede PERCIBIR a 'target' considerando el ocultamiento por estado
    /// (lo consulta la IA: un bot NO debe poder ver/targetear a un invisible, ni a un camuflado
    /// fuera de su radio). Mismo criterio que AlphaFor pero binario: aliados/uno mismo siempre;
    /// invisible nunca para enemigos; camuflaje solo si el enemigo esta dentro del radio.
    /// </summary>
    public static bool IsPerceivableBy(Character target, Character viewer)
    {
        if (target == null) return false;
        if (target.StatusEffects == null) return true;
        var hide = target.StatusEffects.GetHidingEffect();
        if (hide == null) return true;                      // no esta oculto

        if (viewer == null || viewer == target) return true;
        if (viewer.Team == target.Team) return true;        // aliado

        float reveal = hide.EnemyRevealRadius;
        if (reveal <= 0f) return false;                     // invisible total
        // Camuflaje: perceptible solo dentro del radio de revelado.
        float dSqr = ((Vector2)viewer.transform.position - (Vector2)target.transform.position).sqrMagnitude;
        return dSqr <= reveal * reveal;
    }

    static Character LocalViewer()
    {
        var local = PlayerBrain.Local;
        return local != null ? local.GetComponent<Character>() : null;
    }
}
