using System;

/// <summary>
/// Una instancia por Character. Posee el estado activo y dispara eventos al cambiar.
/// </summary>
public class CharacterStateMachine
{
    public ICharacterState Current { get; private set; }
    public event Action<ICharacterState, ICharacterState> OnStateChanged;

    readonly Character _owner;

    public CharacterStateMachine(Character owner)
    {
        _owner = owner;
    }

    public void ChangeState(ICharacterState next)
    {
        var prev = Current;
        prev?.Exit(_owner);
        Current = next;
        next?.Enter(_owner);
        OnStateChanged?.Invoke(prev, next);
    }

    public void Tick(float dt) => Current?.Tick(_owner, dt);
}
