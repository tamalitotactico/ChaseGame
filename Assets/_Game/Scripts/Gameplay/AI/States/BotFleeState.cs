using UnityEngine;

/// <summary>
/// Prey huyendo del Hunter visible mas cercano. Usa BotLocomotion.FindBestEscapeDirection
/// para evaluar varias direcciones candidatas (raycast) y elegir la mejor — evita
/// quedar pegado a esquinas/muros cuando la direccion directa de huida esta bloqueada.
///
/// Cuando BotLocomotion.CheckStuck detecta atasco, fuerza un destino perpendicular
/// (lateral) durante Tuning.fleeLateralBurstDuration para "despegarse" del muro.
///
/// Vuelve a Wander si pierde vision por Tuning.fleeLoseVisionTimeout.
/// </summary>
public class BotFleeState : IBotState
{
    float _noVisionTimer;
    float _repathTimer;
    bool  _abilityFired;
    float _lateralBurstRemaining;
    Vector2 _lateralDir;

    public void Enter(BotBrain bot)
    {
        _noVisionTimer = 0f;
        _repathTimer   = 0f;
        _abilityFired  = false;
        _lateralBurstRemaining = 0f;
        _lateralDir    = Vector2.zero;
    }

    public BrainIntent Tick(BotBrain bot, float dt)
    {
        var hunter = bot.CurrentTarget;
        if (hunter == null || !hunter.IsAlive)
        {
            bot.FSM.ChangeState(new BotWanderState());
            return default;
        }

        Vector2 myPos = bot.Position;
        Vector2 hunterPos = hunter.transform.position;
        float dist = Vector2.Distance(myPos, hunterPos);

        bool visible = bot.CanSee(hunter.transform);
        if (visible)
        {
            _noVisionTimer = 0f;
        }
        else
        {
            _noVisionTimer += dt;
            if (_noVisionTimer >= bot.Tuning.fleeLoseVisionTimeout)
            {
                bot.FSM.ChangeState(new BotWanderState());
                return default;
            }
        }

        Vector2 awayDir = dist > 0.01f ? (myPos - hunterPos) / dist : Vector2.right;

        // --- Burst lateral activo? (post-stuck) ---
        if (_lateralBurstRemaining > 0f)
        {
            _lateralBurstRemaining -= dt;
            // Destino lateral fijo durante el burst
            Vector2 target = myPos + _lateralDir * bot.Tuning.fleeDistance;
            bot.Loco.SetDestination(target);
        }
        else
        {
            // Repath periodico con escape-fan
            _repathTimer -= dt;
            if (_repathTimer <= 0f)
            {
                Vector2 escapeDir = BotLocomotion.FindBestEscapeDirection(
                    myPos, awayDir, bot.Tuning.fleeDistance, bot.Loco.WallLayer,
                    bot.Tuning.fleeFanSamples, bot.Tuning.fleeFanMaxAngle,
                    out float clearance);

                Vector2 fleeTarget = myPos + escapeDir * (clearance * 0.9f);
                bot.Loco.SetDestination(fleeTarget);
                _repathTimer = bot.Tuning.fleeRepathInterval;
            }
        }

        // Stuck recovery → burst lateral perpendicular al awayDir
        if (bot.Loco.CheckStuck(dt) && _lateralBurstRemaining <= 0f)
        {
            // Perpendicular con signo aleatorio
            Vector2 perp = new Vector2(-awayDir.y, awayDir.x);
            if (Random.value < 0.5f) perp = -perp;
            _lateralDir = perp;
            _lateralBurstRemaining = bot.Tuning.fleeLateralBurstDuration;
        }

        var intent = new BrainIntent
        {
            MoveInput = bot.Loco.GetSteeringDirection(),
            AimInput  = awayDir // mismo comportamiento que antes: apunta lejos del hunter
        };

        // Bot Prey usa slot 0 (instant) cuando hunter cerca — comportamiento existente
        if (!_abilityFired && visible && dist < bot.Tuning.fleeAbilityCastDistance)
        {
            intent.Slot0 = AbilityInputState.Pressed;
            _abilityFired = true;
        }

        return intent;
    }

    public void Exit(BotBrain bot) { }
}
