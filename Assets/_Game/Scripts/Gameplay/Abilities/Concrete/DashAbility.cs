using UnityEngine;

/// <summary>
/// Impulso instantaneo en la direccion de movimiento (o facing si no hay input).
/// Caso de uso: ability sin aim. AbilityController la ejecuta directo en Pressed.
/// </summary>
public class DashAbility : Ability
{
    readonly DashAbilityData _d;

    public DashAbility(DashAbilityData d) : base(d) { _d = d; }

    // Sin aim phase: AbilityController ejecuta inmediatamente.
    public override Aimer BeginActivation(in AbilityContext ctx) => null;

    public override void Execute(in AbilityContext ctx, in AimResult _)
    {
        Vector2 dir = ctx.MoveDirection.sqrMagnitude > 0.01f
            ? ctx.MoveDirection.normalized
            : ctx.FacingDirection;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

        ctx.Owner?.Motor?.ApplyImpulse(dir * _d.force, _d.dashDuration);
    }
}
