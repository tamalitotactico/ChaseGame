/// <summary>
/// Estado final. Publica MatchEndedEvent para que la UI muestre resultado.
/// Phase 1+: timer para volver a Lobby o cargar nueva escena.
/// </summary>
public class EndingState : IMatchState
{
    readonly CharacterTeam _winner;
    readonly string _reason;

    public EndingState(CharacterTeam winner, string reason)
    {
        _winner = winner;
        _reason = reason;
    }

    public void Enter(GameManager gm)
    {
        EventBus.Publish(new MatchEndedEvent { WinningTeam = _winner, Reason = _reason });
        ServiceLocator.Resolve<IMusicService>()?.PlayMusic(gm.EndingMusic, fadeDuration: 1.5f);
    }

    public void Tick(GameManager gm, float dt) { }

    public void Exit(GameManager gm)
    {
        ServiceLocator.Resolve<IMusicService>()?.StopMusic();
    }
}
