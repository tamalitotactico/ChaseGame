using UnityEngine;

/// <summary>
/// Buff de velocidad: multiplica el SpeedMultiplier del motor por un factor >= 1.
/// Usado por la definitiva del Hunter (TeleportSmash) para darle bonus de velocidad
/// tras teletransportarse.
///
/// Uso: character.StatusEffects.Apply(new HastedEffect(3f, 1.6f));
/// </summary>
public class HastedEffect : StatusEffect
{
    readonly float _multiplier;

    public override float SpeedModifier => _multiplier;
    public override string IconId       => "haste";

    /// <param name="duration">Duracion del buff en segundos.</param>
    /// <param name="speedMultiplier">Multiplicador de velocidad (clampado a >= 1).</param>
    public HastedEffect(float duration, float speedMultiplier = 1.5f)
    {
        Duration    = Remaining = duration;
        _multiplier = Mathf.Max(1f, speedMultiplier);
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
