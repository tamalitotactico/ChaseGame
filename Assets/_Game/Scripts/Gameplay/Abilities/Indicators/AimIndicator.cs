using UnityEngine;

/// <summary>
/// Estado de aim que el AbilityIndicatorView pasa al indicador cada frame. Incluye la posicion
/// apuntada (Target) ademas de la direccion, para que las habilidades de AREA dibujen su AoE en el
/// punto real (no a maxRange). CastProgress alimenta el fill de canalizacion.
/// </summary>
public struct AimIndicatorState
{
    public Vector2 Origin;        // posicion del caster
    public Vector2 Direction;     // direccion de aim actual (normalizada)
    public Vector2 Target;        // punto apuntado (solo valido si HasTarget)
    public bool    HasTarget;     // true para aimers de area/posicion
    public float   CastProgress;  // 0..1 progreso de canalizacion (0 si no canaliza)
}

/// <summary>
/// Componente visual que se muestra durante el aim de una habilidad. AbilityIndicatorView gestiona
/// el lifecycle automaticamente (instancia / Tick / destruye) — no instanciar desde la ability.
///
/// La forma y el tamano se derivan de AbilityData (ResolvedShape + IndicatorRange/Radius/Width/
/// ProjectileRadius). La implementacion procedural (ProceduralIndicator) dibuja la forma exacta por
/// SDF en un quad escalado a unidades de mundo.
/// </summary>
public abstract class AimIndicator : MonoBehaviour
{
    /// <summary>Owner y data de la ability activa. Setup pesado de una sola vez.</summary>
    public virtual void Begin(Character owner, AbilityData data) { }

    /// <summary>Cada frame durante el aim.</summary>
    public abstract void Tick(in AimIndicatorState state);

    /// <summary>Llamado antes de Destroy. Usar para cleanup (VFX, audio, etc).</summary>
    public virtual void End() { }
}
