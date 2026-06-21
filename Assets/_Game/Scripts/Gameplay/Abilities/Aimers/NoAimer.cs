/// <summary>
/// Aimer no-op para abilities instant (Dash, SpeedBoost). El AbilityController
/// puede saltarse el ciclo de aim si una ability devuelve null en BeginActivation,
/// pero NoAimer existe como caso explicito si se quiere simetria.
/// </summary>
public sealed class NoAimer : Aimer
{
    public override void Tick(in BrainIntent _, float dt) { }

    public override AimResult GetResult() => new AimResult
    {
        Direction = Ctx.FacingDirection,
        HasDirection = Ctx.FacingDirection.sqrMagnitude > 0f
    };
}
