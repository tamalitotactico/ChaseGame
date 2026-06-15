/// <summary>
/// Habilidad 1 del Trapper: Smoke Trap. Coloca una trampa visible que solo dispara un hunter; al
/// dispararse genera un area que ciega (reduce vision) a los hunters por areaDuration. Cupo maxTraps.
///
/// Instant (se coloca en la posicion del prey, TapAimer).
/// </summary>
public class SmokeTrapAbility : PlaceableAbility
{
    readonly SmokeTrapAbilityData _d;

    public SmokeTrapAbility(SmokeTrapAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => null;

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        // lifetime 0 = armada indefinidamente hasta que un hunter la dispare.
        var go = SpawnPlaceable(ctx, _d.trapPrefab,
            new UnityEngine.Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
            UnityEngine.Quaternion.identity, _d.maxTraps, 0f);

        if (go != null && go.TryGetComponent<SmokeTrapPlaceable>(out var trap))
            trap.Setup(_d.triggerRadius, _d.areaRadius, _d.areaDuration,
                       _d.fovMultiplier, _d.blindRefresh, _d.characterMask);
    }
}
