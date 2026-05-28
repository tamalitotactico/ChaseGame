/// <summary>Estado breve mientras se ejecuta el basic attack (Phase 1+).</summary>
public class AttackState : ICharacterState
{
    readonly float _duration;
    float _elapsed;

    public AttackState(float duration)
    {
        _duration = duration;
    }

    public void Enter(Character c) { _elapsed = 0f; }
    public void Exit(Character c)  { }

    public void Tick(Character c, float dt)
    {
        _elapsed += dt;
        if (_elapsed >= _duration)
            c.States.ChangeState(new IdleState());
    }
}
