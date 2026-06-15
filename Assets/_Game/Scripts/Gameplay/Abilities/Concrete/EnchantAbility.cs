using UnityEngine;

/// <summary>
/// Habilidad 2 del Charmer: Enchant. Lanza un proyectil que PERFORA y aplica Charm (jale
/// progresivo hacia la posicion del Charmer al momento del impacto, fear invertido) a cada
/// enemigo que toca.
///
/// Aim: DirectionalAimer (tap = facing/movimiento; drag = direccion apuntada). El proyectil
/// NO hace homing: viaja recto y atraviesa objetivos hasta maxTargets (0 = ilimitado), muro
/// o rango maximo.
/// </summary>
public class EnchantAbility : Ability
{
    readonly EnchantAbilityData _d;

    public EnchantAbility(EnchantAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new DirectionalAimer();

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        if (_d.projectilePrefab == null || ctx.SpawnService == null) return;

        Vector2 dir = aim.HasDirection && aim.Direction.sqrMagnitude > 0.01f
            ? aim.Direction.normalized
            : ctx.FacingDirection;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

        var go = ctx.SpawnService.Spawn(
            _d.projectilePrefab,
            new Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
            Quaternion.FromToRotation(Vector3.right, dir));

        ProjectileSetup.Apply(go, _d.ProjectileRadius);
        if (go != null && go.TryGetComponent<CharmProjectile>(out var cp))
            cp.Init(dir, _d.speed, _d.range, _d.maxTargets,
                    _d.charmDuration, _d.charmPullStrength, _d.slowMultiplier, ctx.Owner, _d.sfxOnHit);
    }
}
