using UnityEngine;

/// <summary>
/// Estado por defecto del Hunter sin target. Elige puntos aleatorios dentro de
/// Tuning.patrolRadius. Transiciona a Chase si ve un enemigo.
/// </summary>
public class BotPatrolState : IBotState
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
        // Deteccion: si veo enemigo, persigo (busca al mas cercano CON linea de vista)
        var enemy = bot.FindNearestVisibleEnemy();
        if (enemy != null)
        {
            bot.CurrentTarget = enemy;
            bot.LastKnownTargetPosition = enemy.transform.position;
            bot.FSM.ChangeState(new BotChaseState());
            return default;
        }

        // Patrol: pick new waypoint si llego o timeout
        _waypointTimer += dt;
        if (!_hasWaypoint || bot.Loco.HasReachedDestination(0.6f) || _waypointTimer >= bot.Tuning.patrolWaypointTimeout)
        {
            _waypoint = bot.Position + (Vector3)(Random.insideUnitCircle * bot.Tuning.patrolRadius);
            bot.Loco.SetDestination(_waypoint);
            _hasWaypoint = true;
            _waypointTimer = 0f;
        }

        bot.Loco.CheckStuck(dt);
        return new BrainIntent { MoveInput = bot.Loco.GetSteeringDirection() };
    }

    public void Exit(BotBrain bot) { }
}
