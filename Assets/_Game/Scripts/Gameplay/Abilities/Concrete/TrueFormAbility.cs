/// <summary>
/// Habilidad 3 (definitiva) del Werewolf: True Form. Instant. Aplica al owner un
/// TrueFormEffect temporizado (haste + ataque basico letal + tint). La duracion es el campo
/// base 'duration' del AbilityData.
///
/// Ultimate por carga de hits (el asset usa usesHitCharge=true): se habilita al acumular
/// golpes basicos a preys. No requiere target -> no necesita CanExecute/refund.
/// </summary>
public class TrueFormAbility : Ability
{
    readonly TrueFormAbilityData _d;

    public TrueFormAbility(TrueFormAbilityData d) : base(d) { _d = d; }

    // Instant: el controller resuelve a TapAimer (fire al release).
    public override Aimer BeginActivation(in AbilityContext ctx) => null;

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var owner = ctx.Owner;
        if (owner == null || owner.StatusEffects == null) return;

        owner.StatusEffects.Apply(new TrueFormEffect(_d.duration, _d.hasteMultiplier, _d.tintColor));

        if (_d.castFXPrefab != null)
            VFXSpawner.PlayOnce(_d.castFXPrefab, owner.transform.position);
    }
}
