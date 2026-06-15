using UnityEngine;

/// <summary>
/// Efecto de aturdimiento: el personaje no puede moverse ni atacar durante la duracion.
///
/// Uso: character.StatusEffects.Apply(new StunnedEffect(2f));
/// </summary>
public class StunnedEffect : StatusEffect
{
    public override bool BlocksMovement => true;
    public override bool BlocksActions  => true;
    public override bool IsControlEffect => true;
    public override Color VisualTint     => new Color(1f, 0.9f, 0.1f, 0.55f);
    public override int   VisualPriority => 30;
    public override string IconId        => "stun";

    public StunnedEffect(float duration)
    {
        Duration = Remaining = duration;
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
