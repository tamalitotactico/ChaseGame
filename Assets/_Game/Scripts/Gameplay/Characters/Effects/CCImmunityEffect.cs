using UnityEngine;

/// <summary>
/// Buff de inmunidad a control (Repel del Charmer). Mientras este activo, el
/// StatusEffectController.Apply RECHAZA nuevos efectos de control (fear/slow/stun/charm)
/// sobre el owner. No bloquea buffs ni acciones; es puramente defensivo.
///
/// No es un efecto de control (IsControlEffect=false) asi que Repel no se auto-dispela
/// ni lo cuenta la inmunidad de otros.
///
/// Uso: owner.StatusEffects.Apply(new CCImmunityEffect(2f));
/// </summary>
public class CCImmunityEffect : StatusEffect
{
    public override bool GrantsCCImmunity => true;
    public override Color VisualTint     => new Color(1f, 0.85f, 0.3f, 0.4f);
    public override int   VisualPriority => 5;
    public override string IconId        => "immune";

    /// <param name="duration">Duracion de la inmunidad en segundos.</param>
    public CCImmunityEffect(float duration)
    {
        Duration = Remaining = duration;
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
