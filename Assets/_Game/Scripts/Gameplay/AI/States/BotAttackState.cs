using UnityEngine;

/// <summary>
/// Hunter ejecutando un ataque. Detiene el movimiento brevemente y dispara
/// AttackPressed un frame. CombatController limita por cooldown internamente.
/// Tras la ventana de ataque vuelve a Chase.
/// </summary>
public class BotAttackState : IBotState
{
    float _elapsed;
    bool  _attackTriggered;

    public void Enter(BotBrain bot)
    {
        _elapsed = 0f;
        _attackTriggered = false;
        bot.Loco.StopMovement();
    }

    public BrainIntent Tick(BotBrain bot, float dt)
    {
        var target = bot.CurrentTarget;
        if (target == null || !target.IsTargetable)
        {
            bot.FSM.ChangeState(new BotPatrolState());
            return default;
        }

        _elapsed += dt;

        BrainIntent intent = new BrainIntent
        {
            AimInput = ((Vector2)target.transform.position - (Vector2)bot.Position).normalized
        };

        if (!_attackTriggered)
        {
            intent.AttackPressed = true;
            _attackTriggered = true;
        }

        if (_elapsed >= bot.Tuning.attackLockDuration)
        {
            // Volver a Chase: re-evalua distancia y LOS
            bot.FSM.ChangeState(new BotChaseState());
        }

        return intent;
    }

    public void Exit(BotBrain bot) { }
}
