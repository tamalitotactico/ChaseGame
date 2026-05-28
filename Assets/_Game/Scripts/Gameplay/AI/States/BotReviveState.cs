using UnityEngine;

/// <summary>
/// Prey bot yendo a revivir a un aliado downed (bot.ReviveTarget).
/// Pathfindea hasta entrar en reviveDetectionRadius y se queda quieto ahi:
/// la proximidad alcanza para que el RevivableComponent del target lo cuente
/// como reviver y avance el ReviveProgress automaticamente.
///
/// Aborta si:
///  - El target dejo de estar downed (revivido o muerto)
///  - Aparece un Hunter visible → entra a Flee con el Hunter como CurrentTarget
/// </summary>
public class BotReviveState : IBotState
{
    const float APPROACH_REPATH_INTERVAL = 0.3f;

    float _repathTimer;

    public void Enter(BotBrain bot)
    {
        _repathTimer = 0f;
        if (bot.ReviveTarget != null)
            bot.Loco.SetDestination(bot.ReviveTarget.transform.position);
    }

    public BrainIntent Tick(BotBrain bot, float dt)
    {
        var target = bot.ReviveTarget;
        if (target == null || !target.IsDowned)
        {
            bot.ReviveTarget = null;
            bot.FSM.ChangeState(new BotWanderState());
            return default;
        }

        // Abort si veo Hunter: huir es prioridad.
        var hunter = bot.FindNearestVisibleEnemy();
        if (hunter != null)
        {
            bot.CurrentTarget = hunter;
            bot.ReviveTarget = null;
            bot.FSM.ChangeState(new BotFleeState());
            return default;
        }

        float dist = Vector2.Distance(bot.Position, target.transform.position);
        bool inRange = dist <= bot.Tuning.reviveDetectionRadius;

        if (inRange)
        {
            // Quedarse encima del downed: la proximidad alcanza para revivir
            bot.Loco.StopMovement();
            return new BrainIntent { MoveInput = Vector2.zero };
        }

        _repathTimer -= dt;
        if (_repathTimer <= 0f)
        {
            bot.Loco.SetDestination(target.transform.position);
            _repathTimer = APPROACH_REPATH_INTERVAL;
        }

        return new BrainIntent { MoveInput = bot.Loco.GetSteeringDirection() };
    }

    public void Exit(BotBrain bot) { }
}
