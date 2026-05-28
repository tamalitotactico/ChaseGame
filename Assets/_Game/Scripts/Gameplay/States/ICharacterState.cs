/// <summary>
/// Estado de alto nivel del personaje (Idle, Move, Attack, Injured, Dead, Ghost...).
/// Su rol principal es servir de gate logico (que se puede hacer/no hacer) y de
/// driver para animaciones/audio en fases posteriores.
///
/// Phase 0: estados existen pero hacen poco; Character lee Health/Motor para
/// decidir transiciones. Phase 1+: las transiciones se centralizan aqui.
/// </summary>
public interface ICharacterState
{
    void Enter(Character c);
    void Tick(Character c, float dt);
    void Exit(Character c);
}
