using UnityEngine;

/// <summary>
/// Habilidad 1 del Werewolf: Ghost Wolf. Invoca un lobo autonomo (GhostWolfController) que
/// navega con A* al prey mas cercano y lo muerde una vez (slow). Direccion inicial = donde
/// apunto el hunter.
///
/// Aim: DirectionalAimer (tap = facing; drag = direccion). El lobo se spawnea via ISpawnService.
/// </summary>
public class GhostWolfAbility : Ability
{
    readonly GhostWolfAbilityData _d;

    public GhostWolfAbility(GhostWolfAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new DirectionalAimer();

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        if (_d.wolfPrefab == null || ctx.SpawnService == null) return;

        Vector2 dir = aim.HasDirection && aim.Direction.sqrMagnitude > 0.01f
            ? aim.Direction.normalized
            : ctx.FacingDirection;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

        var go = ctx.SpawnService.Spawn(
            _d.wolfPrefab,
            new Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
            Quaternion.FromToRotation(Vector3.right, dir));

        if (go != null && go.TryGetComponent<GhostWolfController>(out var wolf))
            wolf.Init(dir, ctx.Owner, _d.moveSpeed, _d.maxLifetime,
                      _d.biteSlowDuration, _d.biteSlowMultiplier, _d.biteRadius,
                      _d.aimBiasSeconds, _d.rePathInterval, _d.wallPadding,
                      _d.revealDuration, _d.pointerSprite, _d.pointerSize, _d.pointerColor);
    }
}
