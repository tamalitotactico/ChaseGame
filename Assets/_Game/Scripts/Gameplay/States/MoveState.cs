/// <summary>Estado mientras el personaje se mueve por input.</summary>
public class MoveState : ICharacterState
{
    public void Enter(Character c) { }
    public void Exit(Character c)  { }

    public void Tick(Character c, float dt)
    {
        if (c.Motor != null && c.Motor.Velocity.sqrMagnitude < 0.01f)
            c.States.ChangeState(new IdleState());
    }
}
