using UnityEngine;

/// <summary>
/// Habilidad 2 del TrickyWizard: Disparo cegador. Proyectil que al impactar reduce el FOV del hunter
/// (BlindedEffect) y lo ralentiza (SlowedEffect).
///
/// Aim: DirectionalAimer (tap = facing; drag = direccion).
/// </summary>
public class BlindShotAbility : Ability
{
    readonly BlindShotAbilityData _d;

    public BlindShotAbility(BlindShotAbilityData d) : base(d) { _d = d; }

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
        if (go != null && go.TryGetComponent<BlindProjectile>(out var bp))
            bp.Init(dir, _d.speed, _d.range, _d.fovMultiplier, _d.fovDuration,
                    _d.slowDuration, _d.slowMultiplier, ctx.Owner, _d.sfxOnHit);
    }
}
