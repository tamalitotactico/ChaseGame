using UnityEngine;

/// <summary>
/// Aimer que apunta a un ALIADO: resuelve el aliado vivo mas cercano al punto apuntado (offset desde el
/// owner, clampeado a aimRange) via IWorldQueryService.GetAlliesOf. Excluye al propio owner; si no hay
/// aliado en rango, GetResult devuelve sin target y la habilidad cae a si misma (self). Usado por Booster
/// Pills (Medic).
/// </summary>
public sealed class AllyTargetAimer : Aimer
{
    readonly float _range;
    Character _target;

    public AllyTargetAimer(float range) { _range = Mathf.Max(0.1f, range); }

    public override void Tick(in BrainIntent intent)
    {
        Vector2 point = Ctx.OwnerPosition;
        Vector2 input = intent.AimInput.sqrMagnitude > 0.01f ? intent.AimInput
                       : (intent.MoveInput.sqrMagnitude > 0.01f ? intent.MoveInput : Vector2.zero);
        if (input.sqrMagnitude > 0.01f)
            point = Ctx.OwnerPosition + Vector2.ClampMagnitude(input, 1f) * _range;

        _target = FindNearestAlly(point);
    }

    Character FindNearestAlly(Vector2 point)
    {
        var owner = Ctx.Owner;
        var world = ServiceLocator.Resolve<IWorldQueryService>();
        if (owner == null || world == null) return null;

        var allies = world.GetAlliesOf(owner.Team);
        Character best = null;
        float bestSqr = _range * _range;
        for (int i = 0; i < allies.Count; i++)
        {
            var a = allies[i];
            if (a == null || a == owner || !a.IsAlive) continue;
            float sqr = ((Vector2)a.transform.position - point).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = a; }
        }
        return best;
    }

    public override AimResult GetResult() => new AimResult
    {
        TargetEntity = _target != null ? _target.transform : null,
        HasTarget    = _target != null
    };
}
