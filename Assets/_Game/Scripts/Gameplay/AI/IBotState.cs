/// <summary>
/// Estado de la FSM del bot. NO se confunde con ICharacterState (que vive en
/// Gameplay/States): ese refleja status visible del personaje (Idle/Move/Attack/
/// Injured/Dead) y aplica a player y bot por igual. IBotState es solo logica
/// interna del bot que produce su BrainIntent cada frame.
/// </summary>
public interface IBotState
{
    void Enter(BotBrain bot);
    BrainIntent Tick(BotBrain bot, float dt);
    void Exit(BotBrain bot);
}
