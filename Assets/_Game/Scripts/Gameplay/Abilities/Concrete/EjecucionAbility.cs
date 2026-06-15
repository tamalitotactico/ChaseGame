using UnityEngine;

/// <summary>
/// Habilidad 3 (definitiva) del Drowned: Ejecucion. Apunta un punto, canaliza castTime, luego
/// teletransporta (dash) a esa posicion infligiendo 1 golpe de dano a todos los enemigos en aoeRadius.
/// Si DERRIBA a un player con el golpe, el Drowned gana haste.
///
/// Ultimate por carga de hits (el asset usa usesHitCharge=true). No requiere target (apunta a un
/// punto del suelo), asi que no usa CanExecute/refund. Reusa DashHelper (respeta muros).
/// </summary>
public class EjecucionAbility : Ability
{
    readonly EjecucionAbilityData _d;
    static readonly Collider2D[] _hitBuffer = new Collider2D[24];

    public EjecucionAbility(EjecucionAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) =>
        new AreaThenCastAimer(_d.castTime, _d.IndicatorRange);

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var owner = ctx.Owner;
        if (owner == null) return;

        Vector2 dest = aim.HasPosition ? (Vector2)aim.TargetPosition : ctx.OwnerPosition;
        Vector2 landing = DashHelper.DashTo(owner, dest, _d.wallLayer, _d.wallPadding);

        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_d.sfxOnLanding, landing);
        if (_d.aoeFXPrefab != null)
            VFXSpawner.PlayOnce(_d.aoeFXPrefab, new Vector3(landing.x, landing.y, owner.transform.position.z));

        var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        filter.SetLayerMask(_d.targetLayers);
        int count = Physics2D.OverlapCircle(landing, _d.aoeRadius, filter, _hitBuffer);

        bool downedSomeone = false;
        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];
            if (col == null) continue;
            var c = col.GetComponentInParent<Character>();
            if (c == null || c == owner) continue;
            if (owner != null && c.Team == owner.Team) continue;
            if (!c.IsAlive) continue;

            c.TakeDamage(new DamageInfo
            {
                Amount    = _d.damage,
                Source    = owner,
                Origin    = landing,
                Direction = ((Vector2)c.transform.position - landing).normalized
            });

            if (c.IsDowned) downedSomeone = true;
        }

        if (downedSomeone && owner.StatusEffects != null && _d.downHasteDuration > 0f)
            owner.StatusEffects.Apply(new HastedEffect(_d.downHasteDuration, _d.downHasteMultiplier));
    }
}
