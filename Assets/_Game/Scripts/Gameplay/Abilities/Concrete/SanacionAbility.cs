using UnityEngine;

/// <summary>
/// Habilidad 2 del Medic: Sanacion. Canaliza castTime, luego cura healAmount al aliado NO-full mas
/// cercano dentro de aoeRadius. Si no hay aliado curable, se cura a si mismo; si tambien esta full,
/// la habilidad se pierde.
///
/// Aim: AimThenCastAimer (solo para la canalizacion; la direccion no se usa, es AoE centrada en el owner).
/// </summary>
public class SanacionAbility : Ability
{
    readonly SanacionAbilityData _d;

    public SanacionAbility(SanacionAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new AimThenCastAimer(_d.castTime);

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var owner = ctx.Owner;
        if (owner == null) return;

        Character target = FindNearestInjuredAlly(owner);
        if (target == null && !IsFull(owner)) target = owner; // fallback: a si mismo
        if (target == null) return;                            // todos full -> se pierde

        target.Health.Heal(_d.healAmount);
    }

    Character FindNearestInjuredAlly(Character owner)
    {
        var world = ServiceLocator.Resolve<IWorldQueryService>();
        if (world == null) return null;

        var allies = world.GetAlliesOf(owner.Team);
        Vector2 pos = owner.transform.position;
        float r2 = _d.aoeRadius * _d.aoeRadius;
        Character best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < allies.Count; i++)
        {
            var a = allies[i];
            if (a == null || a == owner || !a.IsAlive || IsFull(a)) continue;
            float sqr = ((Vector2)a.transform.position - pos).sqrMagnitude;
            if (sqr <= r2 && sqr < bestSqr) { bestSqr = sqr; best = a; }
        }
        return best;
    }

    static bool IsFull(Character c) =>
        c.Health != null && c.Health.CurrentHealth >= c.Health.MaxHealth;
}
