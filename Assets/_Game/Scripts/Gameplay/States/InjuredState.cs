/// <summary>
/// Estado breve tras recibir dano. Mientras dura los iframes del Health,
/// el personaje queda en este estado para gates de UI/animacion.
/// </summary>
public class InjuredState : ICharacterState
{
    public void Enter(Character c) { }
    public void Exit(Character c)  { }

    public void Tick(Character c, float dt)
    {
        if (!c.Health.IsInvulnerable)
            c.States.ChangeState(new IdleState());
    }
}
