/// <summary>
/// Estado del flujo de partida (Lobby, Starting, Playing, Ending).
/// El GameManager es propietario del state machine y se pasa por contexto
/// para que los estados accedan a settings, listas de personajes, etc.
/// </summary>
public interface IMatchState
{
    void Enter(GameManager gm);
    void Tick(GameManager gm, float dt);
    void Exit(GameManager gm);
}
