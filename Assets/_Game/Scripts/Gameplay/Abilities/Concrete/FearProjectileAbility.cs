using UnityEngine;

/// <summary>
/// Habilidad 2 del Hunter: lanza un proyectil con miedo + slow al impactar.
///
/// Dos modos de uso (selecciona el jugador con el gesto del boton):
///   - Tap sin drag: auto-target al Prey mas cercano (con homing si esta en data).
///   - Press + drag + release: dispara en la direccion del drag, sin homing.
///
/// La fase Aim usa DirectionalAimer; AimResult.Direction trae la direccion final
/// si el jugador arrastro mas alla del deadzone del boton.
/// </summary>
public class FearProjectileAbility : Ability
{
    readonly FearProjectileAbilityData _d;

    public FearProjectileAbility(FearProjectileAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new DirectionalAimer();

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        if (_d.projectilePrefab == null || ctx.SpawnService == null) return;

        // Drag explicito → direccion fija sin homing target.
        // Tap puro (sin drag) → auto-target nearest, con homing de la data.
        Character target = null;
        Vector2   dir;

        if (aim.Explicit)
        {
            dir = aim.Direction.normalized;
        }
        else
        {
            target = FindNearestEnemy(ctx.Owner, ctx.OwnerPosition, _d.range);
            dir = target != null
                ? ((Vector2)target.transform.position - ctx.OwnerPosition).normalized
                : (aim.HasDirection ? aim.Direction : ctx.FacingDirection);
        }
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

        var go = ctx.SpawnService.Spawn(
            _d.projectilePrefab,
            new Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
            Quaternion.FromToRotation(Vector3.right, dir));

        ProjectileSetup.Apply(go, _d.ProjectileRadius);
        if (go != null && go.TryGetComponent<FearProjectile>(out var fp))
            fp.Init(dir, _d.speed, _d.range, target, _d.homing, _d.homingTurnRateDeg,
                    _d.fearDuration, _d.slowDuration, _d.slowMultiplier, ctx.Owner, _d.sfxOnHit);
    }

    static Character FindNearestEnemy(Character owner, Vector2 fromPos, float maxRange)
    {
        var world = ServiceLocator.Resolve<IWorldQueryService>();
        if (world == null) return null;

        var candidates = world.GetEnemiesOf(owner.Team);

        Character best = null;
        float bestSqr = maxRange * maxRange;
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (c == null || !c.IsAlive) continue;
            float sqr = ((Vector2)c.transform.position - fromPos).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = c; }
        }
        return best;
    }
}
