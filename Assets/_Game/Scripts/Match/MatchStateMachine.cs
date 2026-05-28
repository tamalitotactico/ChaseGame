using System;

/// <summary>State machine del flujo de partida. Una instancia por GameManager.</summary>
public class MatchStateMachine
{
    public IMatchState Current { get; private set; }
    public string CurrentName  => Current != null ? Current.GetType().Name : "None";

    public event Action<string, string> OnStateChanged;

    readonly GameManager _gm;

    public MatchStateMachine(GameManager gm)
    {
        _gm = gm;
    }

    public void ChangeState(IMatchState next)
    {
        string from = CurrentName;
        Current?.Exit(_gm);
        Current = next;
        next?.Enter(_gm);
        string to = CurrentName;
        OnStateChanged?.Invoke(from, to);
        EventBus.Publish(new MatchStateChangedEvent { From = from, To = to });
    }

    public void Tick(float dt) => Current?.Tick(_gm, dt);
}
