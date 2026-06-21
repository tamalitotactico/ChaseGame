using UnityEngine;

/// <summary>
/// Spawna los personajes y corre la cuenta atras.
/// Al terminar, transiciona a PlayingState.
/// </summary>
public class StartingState : IMatchState
{
    float _countdown;
    int   _lastSecondPublished = -1;

    public void Enter(GameManager gm)
    {
        gm.SpawnMatchEntities();
        _countdown = gm.Settings != null ? gm.Settings.countdownDuration : 3f;
        _lastSecondPublished = -1;
    }

    public void Tick(GameManager gm, float dt)
    {
        _countdown -= dt;
        int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(_countdown));
        gm.CountdownDisplay = secondsLeft; // expuesto para replicacion host->cliente (timer sync)
        if (secondsLeft != _lastSecondPublished)
        {
            _lastSecondPublished = secondsLeft;
            EventBus.Publish(new CountdownTickEvent { SecondsLeft = secondsLeft });
        }
        if (_countdown <= 0f)
            gm.States.ChangeState(new PlayingState());
    }

    public void Exit(GameManager gm) { }
}
