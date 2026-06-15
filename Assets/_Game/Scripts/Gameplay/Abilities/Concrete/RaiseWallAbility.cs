using UnityEngine;

/// <summary>
/// Habilidad 1 del Engineer: Raise Wall. Coloca una barra que relentiza a los hunters que la cruzan
/// (no bloquea; preys pasan). Cupo maxWalls (el mas antiguo se borra al exceder). Vida infinita hasta
/// que un hunter la toca -> hitLifetime.
///
/// Aim: AreaAimer (punto dentro de aimRange). El muro se orienta perpendicular a la direccion del aim.
/// </summary>
public class RaiseWallAbility : PlaceableAbility
{
    readonly RaiseWallAbilityData _d;

    public RaiseWallAbility(RaiseWallAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new AreaAimer(_d.IndicatorRange);

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        Vector2 ownerPos = ctx.OwnerPosition;

        // lifetime 0 = infinito hasta que un hunter lo toque.
        var go = SpawnPlaceable(ctx, _d.wallPrefab,
            new Vector3(ownerPos.x, ownerPos.y, 0f), Quaternion.identity, _d.maxWalls, 0f);

        if (go != null && go.TryGetComponent<RaiseWallPlaceable>(out var wall))
            wall.Setup(ownerPos, _d.maxLength, _d.slowAreaWidth, _d.slowMultiplier, _d.slowDuration,
                       _d.hitLifetime, _d.wallLayer, _d.hunterLayer);
    }
}
