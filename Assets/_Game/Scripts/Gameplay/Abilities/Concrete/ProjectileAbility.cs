using UnityEngine;

/// <summary>
/// Proyectil direccional con preview (estilo Brawl Stars). Demuestra:
///   - Aim phase con DirectionalAimer (hold-aim-release).
///   - Spawn de entidad via ISpawnService (compatible con Fusion en Phase 3).
///   - Damage via IDamageable en colision.
/// </summary>
public class ProjectileAbility : Ability
{
    readonly ProjectileAbilityData _d;

    public ProjectileAbility(ProjectileAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new DirectionalAimer();

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        if (_d.projectilePrefab == null || ctx.SpawnService == null) return;

        Vector2 dir = aim.HasDirection ? aim.Direction : ctx.FacingDirection;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

        var go = ctx.SpawnService.Spawn(
            _d.projectilePrefab,
            new Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
            Quaternion.FromToRotation(Vector3.right, dir));

        ProjectileSetup.Apply(go, _d.ProjectileRadius);
        if (go != null && go.TryGetComponent<Projectile>(out var p))
            p.Init(dir, _d.speed, _d.range, _d.damage, ctx.Owner, _d.sfxOnHit);
    }
}
