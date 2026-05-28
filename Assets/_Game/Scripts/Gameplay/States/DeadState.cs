/// <summary>
/// Estado terminal. El personaje ignora input. Phase 1+ podra transicionar
/// a GhostState si el modo de juego lo permite.
/// </summary>
public class DeadState : ICharacterState
{
    public void Enter(Character c)
    {
        if (c.Motor != null) c.Motor.Stop();
    }

    public void Exit(Character c)  { }
    public void Tick(Character c, float dt) { }
}
