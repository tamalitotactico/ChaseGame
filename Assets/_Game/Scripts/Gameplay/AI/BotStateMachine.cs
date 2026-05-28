using System;

/// <summary>State machine del bot. Una instancia por BotBrain.</summary>
public class BotStateMachine
{
    public IBotState Current { get; private set; }
    public event Action<IBotState, IBotState> OnStateChanged;

    readonly BotBrain _owner;

    public BotStateMachine(BotBrain owner)
    {
        _owner = owner;
    }

    public void ChangeState(IBotState next)
    {
        var prev = Current;
        prev?.Exit(_owner);
        Current = next;
        next?.Enter(_owner);
        OnStateChanged?.Invoke(prev, next);
    }

    public BrainIntent Tick(float dt) => Current != null ? Current.Tick(_owner, dt) : default;
}
