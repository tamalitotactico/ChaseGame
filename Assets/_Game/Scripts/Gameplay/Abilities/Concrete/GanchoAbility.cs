using UnityEngine;

/// <summary>
/// Habilidad 1 del Drowned: Gancho. Proyectil recto que, al primer enemigo, lo atrae al punto medio
/// entre el cazador y el prey (solo se mueve el prey) y le aplica Slow.
///
/// Aim: DirectionalAimer (tap = facing; drag = direccion).
/// </summary>
public class GanchoAbility : Ability
{
    readonly GanchoAbilityData _d;

    public GanchoAbility(GanchoAbilityData d) : base(d) { _d = d; }

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
        if (go != null && go.TryGetComponent<HookProjectile>(out var hook))
            hook.Init(dir, _d.speed, _d.range, _d.pullDuration,
                      _d.slowDuration, _d.slowMultiplier, ctx.Owner, _d.sfxOnHit);
    }
}
