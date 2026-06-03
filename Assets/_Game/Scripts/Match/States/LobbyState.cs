/// <summary>
/// Estado inicial. En Phase 0 sandbox se omite (autoStart pasa directo a Starting).
/// Phase 1+: aqui se mostrara seleccion de rol; en Phase 3 se hara matchmaking.
/// </summary>
public class LobbyState : IMatchState
{
    public void Enter(GameManager gm) => EventBus.Publish(new LobbyEnteredEvent());
    public void Tick(GameManager gm, float dt) { }
    public void Exit(GameManager gm) => EventBus.Publish(new LobbyExitedEvent());
}
