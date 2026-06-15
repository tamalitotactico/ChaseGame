using UnityEngine;

/// <summary>
/// Brain de PREVIEW (dev-only, no se usa en partidas). Deja que el Character Preview
/// Harness inyecte movimiento, ataque basico y activacion de habilidades de forma
/// programatica, reproduciendo el gesto press -> (hold/aim) -> release que espera el
/// AbilityController (estilo Brawl Stars: nada se ejecuta en el press, se dispara al release
/// o al completarse la canalizacion).
///
/// Lo agrega CharacterPreviewRig al personaje spawneado. Character lo consume via IBrain.
/// </summary>
public class CharacterPreviewBrain : MonoBehaviour, IBrain
{
    [Tooltip("Frames de 'hold/aim' entre el press y el release al disparar una habilidad. " +
             "Da tiempo a que el aimer capture la direccion y a que arranque la canalizacion.")]
    [SerializeField] int holdFrames = 8;

    Vector2 _move;
    Vector2 _aim = Vector2.right;
    bool    _attackQueued;

    int _abilitySlot = -1; // slot en curso (-1 = ninguno)
    int _abilityFrame;     // frames desde el press

    public void SetMove(Vector2 dir) => _move = dir;
    public void Stop()               => _move = Vector2.zero;
    public void QueueAttack()        => _attackQueued = true;

    /// <summary>Programa la activacion de un slot con una direccion de aim.</summary>
    public void QueueAbility(int slot, Vector2 aimDir)
    {
        _abilitySlot  = slot;
        _abilityFrame = 0;
        if (aimDir.sqrMagnitude > 0.0001f) _aim = aimDir.normalized;
    }

    public BrainIntent CaptureIntent()
    {
        var intent = new BrainIntent { MoveInput = _move, AimInput = _aim };

        if (_attackQueued) { intent.AttackPressed = true; _attackQueued = false; }

        if (_abilitySlot >= 0)
        {
            AbilityInputState state;
            if (_abilityFrame == 0)             state = AbilityInputState.Pressed;
            else if (_abilityFrame < holdFrames) state = AbilityInputState.Held;
            else                                 state = AbilityInputState.Released;

            SetSlot(ref intent, _abilitySlot, state);

            if (state == AbilityInputState.Released) _abilitySlot = -1;
            else                                     _abilityFrame++;
        }

        return intent;
    }

    static void SetSlot(ref BrainIntent intent, int slot, AbilityInputState s)
    {
        switch (slot)
        {
            case 0: intent.Slot0 = s; break;
            case 1: intent.Slot1 = s; break;
            case 2: intent.Slot2 = s; break;
        }
    }
}
