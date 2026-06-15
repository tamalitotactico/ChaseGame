using UnityEngine;

/// <summary>
/// Habilidad 3 (definitiva) del Charmer: Assault. Canaliza un instante y luego salta al
/// ENEMIGO EN VISION mas cercano, aplicandole Fear.
///
/// Ultimate por carga de hits (slot R): el asset usa usesHitCharge=true; el AbilityController
/// la habilita al acumular golpes basicos. Si al ejecutar NO hay enemigo en rango de vision,
/// CanExecute devuelve false -> el controller cancela y DEVUELVE la carga (no se consume).
///
/// Aim: AimThenCastAimer (press -> aim, release -> canalizacion fija castTime). La direccion
/// del aim no afecta el destino (salta al enemigo, no a una direccion); solo da el patron de
/// canalizacion/inmovilizacion durante el cast.
/// </summary>
public class AssaultAbility : Ability
{
    readonly AssaultAbilityData _d;

    public AssaultAbility(AssaultAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new AimThenCastAimer(_d.castTime);

    public override void OnCastingBegan(in AbilityContext ctx)
    {
        if (ctx.Owner != null && _d.sfxOnCastStart != null)
            ServiceLocator.Resolve<IAudioService>()?.PlayAttached(_d.sfxOnCastStart, ctx.Owner.transform);
    }

    // Refund: sin enemigo en vision, la ult se cancela sin consumir la carga.
    public override bool CanExecute(in AbilityContext ctx, in AimResult aim)
        => FindNearestEnemyInVision(ctx.Owner, ctx.OwnerPosition) != null;

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var owner  = ctx.Owner;
        var target = FindNearestEnemyInVision(owner, ctx.OwnerPosition);
        if (owner == null || target == null) return;

        Vector2 landing = DashHelper.DashTo(owner, target.transform.position, _d.wallLayer, _d.wallPadding);

        if (target.StatusEffects != null)
        {
            Vector2 flee = ((Vector2)target.transform.position - landing).normalized;
            target.StatusEffects.Apply(new FearedEffect(_d.fearDuration, flee));
            if (_d.slowMultiplier < 1f)
                target.StatusEffects.Apply(new SlowedEffect(_d.fearDuration, _d.slowMultiplier));
        }

        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_d.sfxOnLanding, landing);
        if (_d.arrivalFXPrefab != null)
            VFXSpawner.PlayOnce(_d.arrivalFXPrefab, new Vector3(landing.x, landing.y, owner.transform.position.z));
    }

    Character FindNearestEnemyInVision(Character owner, Vector2 fromPos)
    {
        if (owner == null) return null;
        var world = ServiceLocator.Resolve<IWorldQueryService>();
        if (world == null) return null;

        var enemies = world.GetEnemiesOf(owner.Team);

        Character best = null;
        float bestSqr = _d.visionRange * _d.visionRange;
        for (int i = 0; i < enemies.Count; i++)
        {
            var c = enemies[i];
            if (c == null || !c.IsAlive) continue;
            float sqr = ((Vector2)c.transform.position - fromPos).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = c; }
        }
        return best;
    }
}
