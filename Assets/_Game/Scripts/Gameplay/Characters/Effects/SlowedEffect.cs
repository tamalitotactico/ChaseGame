using UnityEngine;

/// <summary>
/// Efecto de ralentizacion: reduce la velocidad del personaje por un multiplicador configurable.
/// No bloquea el movimiento ni las acciones, solo los hace mas lentos.
///
/// Uso: character.StatusEffects.Apply(new SlowedEffect(3f, 0.45f)); // 3s al 45% de velocidad
/// </summary>
public class SlowedEffect : StatusEffect
{
    readonly float _multiplier;

    public override float SpeedModifier => _multiplier;

    /// <param name="duration">Duracion en segundos.</param>
    /// <param name="speedMultiplier">Fraccion de la velocidad base [0..1]. Default 0.45 = 45%.</param>
    public SlowedEffect(float duration, float speedMultiplier = 0.45f)
    {
        Duration    = Remaining = duration;
        _multiplier = Mathf.Clamp01(speedMultiplier);
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
