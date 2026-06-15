/// <summary>
/// Habilidad 2 del Trapper: Bear Trap. Coloca una trampa que al primer hunter lo aturde y revela su
/// posicion a los preys (World Target Pointer), luego desaparece. Cupo maxTraps.
///
/// Instant (se coloca en la posicion del prey, TapAimer).
/// </summary>
public class BearTrapAbility : PlaceableAbility
{
    readonly BearTrapAbilityData _d;

    public BearTrapAbility(BearTrapAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => null;

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var go = SpawnPlaceable(ctx, _d.trapPrefab,
            new UnityEngine.Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
            UnityEngine.Quaternion.identity, _d.maxTraps, 0f);

        if (go != null && go.TryGetComponent<BearTrapPlaceable>(out var trap))
            trap.Setup(_d.triggerRadius, _d.stunDuration, _d.revealDuration,
                       _d.pointerSprite, _d.pointerSize, _d.pointerColor, _d.characterMask);
    }
}
