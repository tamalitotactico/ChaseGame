/// <summary>
/// Habilidad 1 del Medic: Booster Pills. Apuntable a un aliado (AllyTargetAimer): le da haste y le
/// RECORTA un % del cooldown restante de TODAS sus habilidades (instantaneo). Sin aliado apuntado, se
/// aplica a si mismo.
/// </summary>
public class BoosterPillsAbility : Ability
{
    readonly BoosterPillsAbilityData _d;

    public BoosterPillsAbility(BoosterPillsAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new AllyTargetAimer(_d.aimRange);

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        Character target = aim.HasTarget && aim.TargetEntity != null
            ? aim.TargetEntity.GetComponentInParent<Character>()
            : null;
        if (target == null) target = ctx.Owner; // fallback: a si mismo
        if (target == null) return;

        if (target.StatusEffects != null && _d.hasteDuration > 0f)
            target.StatusEffects.Apply(new HastedEffect(_d.hasteDuration, _d.hasteMultiplier));

        if (target.Abilities != null)
            target.Abilities.ReduceAllCooldowns(_d.cdReductionPct / 100f);
    }
}
