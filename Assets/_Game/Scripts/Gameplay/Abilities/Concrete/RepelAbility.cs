/// <summary>
/// Habilidad 1 del Charmer: Repel. Dispel fuerte instantaneo de efectos negativos de
/// control sobre uno mismo (deja los buffs intactos) + inmunidad a control + haste.
///
/// Instant (BeginActivation null -> el AbilityController usa TapAimer: dispara al soltar).
/// El orden importa: primero se limpia el control activo, luego se aplica la inmunidad
/// (para que no se auto-rechace) y el haste.
/// </summary>
public class RepelAbility : Ability
{
    readonly RepelAbilityData _d;

    public RepelAbility(RepelAbilityData d) : base(d) { _d = d; }

    // Sin aim: instant. El controller resuelve a TapAimer (fire al release).
    public override Aimer BeginActivation(in AbilityContext ctx) => null;

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var owner = ctx.Owner;
        if (owner == null || owner.StatusEffects == null) return;

        if (_d.dispelControlEffects)
            owner.StatusEffects.DispelControlEffects();

        if (_d.immunityDuration > 0f)
            owner.StatusEffects.Apply(new CCImmunityEffect(_d.immunityDuration));

        if (_d.hasteDuration > 0f)
            owner.StatusEffects.Apply(new HastedEffect(_d.hasteDuration, _d.hasteMultiplier));
    }
}
