using UnityEngine;

/// <summary>
/// Snapshot por frame de lo que el cerebro (player/bot/remote/replay) quiere hacer.
/// Es el unico canal de comunicacion entre Brain y Character.
///
/// Diseno orientado a NetworkInput de Photon Fusion: campos planos, sin referencias,
/// serializables. En Phase 3 esta misma struct sera lo que se mande por red.
/// </summary>
public struct BrainIntent
{
    public Vector2 MoveInput;          // -1..1 normalizado (joystick o WASD)
    public bool    AttackPressed;      // tap en boton de ataque (1 frame)
    public AbilityInputState Slot0;    // tipicamente Q
    public AbilityInputState Slot1;    // tipicamente E
    public AbilityInputState Slot2;    // tipicamente R (solo Hunter)
    public Vector2 AimInput;           // direccion durante hold de ability (drag/right-stick)

    /// <summary>
    /// Devuelve una copia del intent con todas las acciones (ataque, habilidades) en cero
    /// pero conservando el input de movimiento y aim. Usado por StatusEffectController
    /// cuando el personaje tiene un efecto que bloquea acciones (stun).
    /// </summary>
    public BrainIntent WithActionsCleared() => new BrainIntent
    {
        MoveInput = MoveInput,
        AimInput  = AimInput,
        // AttackPressed, Slot0-2 quedan en default (false / None)
    };
}

/// <summary>
/// Estado de un slot de habilidad. Soporta tap-to-cast y hold-to-aim-release-to-fire.
/// </summary>
public enum AbilityInputState : byte
{
    None     = 0,
    Pressed  = 1, // frame en que se presiono
    Held     = 2, // se sigue presionando
    Released = 3  // frame en que se solto
}
