using UnityEngine;

/// <summary>
/// Hunter perdio el target. Va a la ultima posicion conocida y mira a su
/// alrededor durante Tuning.searchDuration. Vuelve a Chase si recupera vision,
/// o a Patrol si expira el tiempo.
/// </summary>
public class BotSearchState : IBotState
{
    float _timer;
    Vector3 _scanPoint;
    bool _reachedKnown;

    public void Enter(BotBrain bot)
    {
        _timer = 0f;
        _reachedKnown = false;
        bot.Loco.SetDestination(bot.LastKnownTargetPosition);
    }

    public BrainIntent Tick(BotBrain bot, float dt)
    {
        _timer += dt;

        // Recupera vision?
        var enemy = bot.FindNearestVisibleEnemy();
        if (enemy != null)
        {
            bot.CurrentTarget = enemy;
            bot.LastKnownTargetPosition = enemy.transform.position;
            bot.FSM.ChangeState(new BotChaseState());
            return default;
        }

        if (_timer >= bot.Tuning.searchDuration)
        {
            bot.FSM.ChangeState(new BotPatrolState());
            return default;
        }

        // Llego al punto conocido? Empezar a hacer barridos
        if (!_reachedKnown && bot.Loco.HasReachedDestination(0.8f))
        {
            _reachedKnown = true;
        }

        if (_reachedKnown)
        {
            // Mover en pequenos barridos alrededor del punto
            _scanPoint = bot.LastKnownTargetPosition + (Vector3)(Random.insideUnitCircle * bot.Tuning.searchScanRadius);
            bot.Loco.SetDestination(_scanPoint);
        }

        return new BrainIntent { MoveInput = bot.Loco.GetSteeringDirection() };
    }

    public void Exit(BotBrain bot) { }
}
