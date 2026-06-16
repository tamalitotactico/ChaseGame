#if FUSION2
using Fusion;
using UnityEngine;

/// <summary>
/// Input replicado por Fusion. Espejo plano de <see cref="BrainIntent"/> (que ya fue disenado como
/// struct sin referencias justamente para esto). El PlayerBrain local lo llena en el callback OnInput
/// (ver FusionInputCollector) y el Character lo lee en FixedUpdateNetwork via GetInput, reconstruyendo
/// un BrainIntent para alimentar el mismo pipe de simulacion de siempre.
///
/// AbilityInputState viaja como byte (0..3). En el Hito 1 solo se consume Move/Aim; los slots/attack
/// quedan listos para cuando el combate/abilities se networkeen (hito siguiente).
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public Vector2 Move;
    public Vector2 Aim;
    public byte    Attack; // 0/1
    public byte    Slot0;  // AbilityInputState
    public byte    Slot1;
    public byte    Slot2;

    public BrainIntent ToIntent() => new BrainIntent
    {
        MoveInput     = Move,
        AimInput      = Aim,
        AttackPressed = Attack != 0,
        Slot0 = (AbilityInputState)Slot0,
        Slot1 = (AbilityInputState)Slot1,
        Slot2 = (AbilityInputState)Slot2,
    };

    public static NetworkInputData FromIntent(in BrainIntent i) => new NetworkInputData
    {
        Move   = i.MoveInput,
        Aim    = i.AimInput,
        Attack = (byte)(i.AttackPressed ? 1 : 0),
        Slot0  = (byte)i.Slot0,
        Slot1  = (byte)i.Slot1,
        Slot2  = (byte)i.Slot2,
    };
}
#endif
