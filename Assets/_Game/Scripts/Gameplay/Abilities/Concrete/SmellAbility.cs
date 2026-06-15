using System.Collections.Generic;
using UnityEngine;

#if ASTAR_EXISTS
using Pathfinding;
#endif

/// <summary>
/// Habilidad 2 del Werewolf: Smell. Al lanzar, calcula con A* la ruta optima del Werewolf a
/// CADA prey y la dibuja como un trail que revela el camino. Snapshot: las rutas se calculan
/// una vez al cast (no se recalculan) y persisten revealDuration segundos.
///
/// Instant (TapAimer). Si A* no esta disponible (sin grafo en escena), cae a una linea recta
/// hunter->prey como fallback.
/// </summary>
public class SmellAbility : Ability
{
    readonly SmellAbilityData _d;

    public SmellAbility(SmellAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => null;

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        if (_d.trailPrefab == null || ctx.SpawnService == null) return;
        var world = ServiceLocator.Resolve<IWorldQueryService>();
        if (world == null || ctx.Owner == null) return;

        var preys = world.GetEnemiesOf(ctx.Owner.Team);
        Vector3 start = ctx.OwnerPosition;

        for (int i = 0; i < preys.Count; i++)
        {
            var prey = preys[i];
            if (prey == null || !prey.IsAlive) continue;

            var points = GetPathPoints(start, prey.transform.position);
            var go = ctx.SpawnService.Spawn(_d.trailPrefab, Vector3.zero, Quaternion.identity);
            if (go != null && go.TryGetComponent<SmellTrail>(out var trail))
                trail.Setup(points, _d.revealDuration, _d.trailWidth, _d.trailColor);
        }
    }

    static List<Vector3> GetPathPoints(Vector3 start, Vector3 end)
    {
#if ASTAR_EXISTS
        if (AstarPath.active != null)
        {
            var p = ABPath.Construct(start, end, null);
            AstarPath.StartPath(p);
            p.BlockUntilCalculated();
            if (!p.error && p.vectorPath != null && p.vectorPath.Count > 0)
                return p.vectorPath;
        }
#endif
        return new List<Vector3> { start, end };
    }
}
