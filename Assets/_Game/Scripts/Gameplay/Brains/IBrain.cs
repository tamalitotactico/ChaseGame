/// <summary>
/// Productor de intents. Implementaciones:
///  - PlayerBrain: lee teclado/joystick virtual.
///  - BotBrain: corre logica AI (FSM Phase 0, Utility AI Phase 2).
///  - RemoteBrain (Phase 3): lee NetworkInput sincronizado de un cliente remoto.
///  - ReplayBrain (futuro): reproduce intents grabados.
///
/// El Character pregunta una vez por frame y aplica el intent. Nunca conoce de
/// donde viene.
/// </summary>
public interface IBrain
{
    BrainIntent CaptureIntent();
}
