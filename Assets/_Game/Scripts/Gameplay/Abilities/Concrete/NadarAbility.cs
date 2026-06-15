/// <summary>
/// Habilidad 2 del Drowned: Nadar. Instant. Aplica al owner un CamouflageEffect (camuflaje por
/// distancia + haste). Atacar o usar otra habilidad lo rompe (lo maneja BreakActionSensitiveEffects
/// en Combat/Abilities; se llama ANTES de Execute, asi que este camuflaje recien aplicado sobrevive).
/// </summary>
public class NadarAbility : Ability
{
    readonly NadarAbilityData _d;

    public NadarAbility(NadarAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => null; // instant (TapAimer)

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var owner = ctx.Owner;
        if (owner == null || owner.StatusEffects == null) return;
        owner.StatusEffects.Apply(new CamouflageEffect(_d.duration, _d.hasteMultiplier, _d.revealRadius));
    }
}
