/// <summary>
/// Habilidad 2 del Engineer: Beacon. Coloca una baliza en su posicion (instant) que da haste a los
/// aliados dentro del area por su duracion; al salir/expirar el boost dura exitBoostDuration mas. El
/// hunter la rompe de 1 golpe. Cupo maxBeacons.
/// </summary>
public class BeaconAbility : PlaceableAbility
{
    readonly BeaconAbilityData _d;

    public BeaconAbility(BeaconAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => null; // en su posicion (TapAimer)

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var go = SpawnPlaceable(ctx, _d.beaconPrefab,
            new UnityEngine.Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
            UnityEngine.Quaternion.identity, _d.maxBeacons, _d.duration);

        if (go != null && go.TryGetComponent<BeaconPlaceable>(out var beacon))
            beacon.SetupBeacon(_d.areaRadius, _d.hasteMultiplier, _d.exitBoostDuration, _d.beaconHealth);
    }
}
