/// <summary>Estado por defecto cuando el personaje esta vivo y sin movimiento.</summary>
public class IdleState : ICharacterState
{
    public void Enter(Character c) { }
    public void Exit(Character c)  { }

    public void Tick(Character c, float dt)
    {
        if (c.Motor != null && c.Motor.Velocity.sqrMagnitude > 0.05f)
            c.States.ChangeState(new MoveState());
    }
}
