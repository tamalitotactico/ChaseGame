using UnityEngine;

/// <summary>
/// Habilidad 1 del Hunter: deja un señuelo en su posicion actual. El señuelo emite
/// un aura de miedo que asusta a Prey que se acerquen.
/// Instant cast (sin Aimer).
/// </summary>
public class RemnantAbility : Ability
{
    readonly RemnantAbilityData _d;

    public RemnantAbility(RemnantAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => null;

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        if (_d.decoyPrefab == null || ctx.SpawnService == null) return;

        var go = ctx.SpawnService.Spawn(
            _d.decoyPrefab,
            new Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
            Quaternion.identity);

        if (go != null && go.TryGetComponent<RemnantDecoy>(out var decoy))
            decoy.Setup(_d.decoyDuration, _d.activationRadius, _d.effectRadius,
                        _d.fearDuration, _d.slowDuration, _d.slowMultiplier,
                        _d.activeDuration, ctx.Owner, _d.preyLayer);
    }
}
