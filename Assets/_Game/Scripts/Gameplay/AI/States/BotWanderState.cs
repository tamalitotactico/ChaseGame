using UnityEngine;

/// <summary>
/// Estado por defecto del Prey sin amenaza visible. Pasea con waypoints
/// aleatorios. Prioridades de transicion:
///   1. Veo Hunter visible → Flee
///   2. Veo aliado downed → Revive
///   3. Patrol normal
/// </summary>
public class BotWanderState : IBotState
{
    Vector3 _waypoint;
    float   _waypointTimer;
    bool    _hasWaypoint;

    public void Enter(BotBrain bot)
    {
        _hasWaypoint = false;
        _waypointTimer = 0f;
    }

    public BrainIntent Tick(BotBrain bot, float dt)
    {
        // 1. Hunter visible? Huir
        var enemy = bot.FindNearestVisibleEnemy();
        if (enemy != null)
        {
            bot.CurrentTarget = enemy;
            bot.FSM.ChangeState(new BotFleeState());
            return default;
        }

        // 2. Aliado downed cerca? Ir a revivir
        var downed = bot.FindNearestDownedAlly();
        if (downed != null)
        {
            bot.ReviveTarget = downed;
            bot.FSM.ChangeState(new BotReviveState());
            return default;
        }

        // 3. Wander
        _waypointTimer += dt;
        if (!_hasWaypoint || bot.Loco.HasReachedDestination(0.6f) || _waypointTimer >= bot.Tuning.wanderWaypointTimeout)
        {
            _waypoint = bot.Position + (Vector3)(Random.insideUnitCircle * bot.Tuning.wanderRadius);
            bot.Loco.SetDestination(_waypoint);
            _hasWaypoint = true;
            _waypointTimer = 0f;
        }

        bot.Loco.CheckStuck(dt);
        return new BrainIntent { MoveInput = bot.Loco.GetSteeringDirection() };
    }

    public void Exit(BotBrain bot) { }
}
