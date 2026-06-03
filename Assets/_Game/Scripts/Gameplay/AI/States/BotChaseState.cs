using UnityEngine;

/// <summary>
/// Hunter persiguiendo al target. Cuando entra en attackRange transiciona a
/// AttackState. Si pierde LOS por mas de chaseLosTimeout entra a SearchState.
///
/// Mejoras tacticas vs version inicial:
///   - Interception: predice donde estara el target en leadTime y va alli en vez de
///     a su posicion actual (target.Motor.Velocity).
///   - Habilidades data-driven: itera Tuning.abilityRules y dispara la primera regla
///     cuyas condiciones se cumplen. Cada regla tiene cooldown interno + cooldown
///     global compartido (anti-spam).
/// </summary>
public class BotChaseState : IBotState
{
    const int MaxSlots = 3;

    float   _losLostTimer;
    float   _repathTimer;
    float   _globalAbilityCdRemaining;
    readonly float[] _slotCdRemaining = new float[MaxSlots];
    // Slot que espera un Released en el proximo frame (-1 = ninguno).
    // El AbilityController cancela el aimer si recibe None tras Pressed; se necesita
    // emitir Released para que HandleRelease() dispare la habilidad.
    int     _pendingReleaseSlot = -1;

    public void Enter(BotBrain bot)
    {
        _losLostTimer = 0f;
        _repathTimer  = 0f;
        _globalAbilityCdRemaining = 0f;
        for (int i = 0; i < MaxSlots; i++) _slotCdRemaining[i] = 0f;
        _pendingReleaseSlot = -1;
    }

    public BrainIntent Tick(BotBrain bot, float dt)
    {
        var target = bot.CurrentTarget;
        if (target == null || !target.IsTargetable)
        {
            bot.FSM.ChangeState(new BotPatrolState());
            return default;
        }

        // Decrementar cooldowns
        _globalAbilityCdRemaining = Mathf.Max(0f, _globalAbilityCdRemaining - dt);
        for (int i = 0; i < MaxSlots; i++)
            _slotCdRemaining[i] = Mathf.Max(0f, _slotCdRemaining[i] - dt);

        Vector2 myPos = bot.Position;
        Vector2 targetPos = target.transform.position;
        float dist = Vector2.Distance(myPos, targetPos);

        bool canSee = bot.CanSee(target.transform);
        if (canSee)
        {
            _losLostTimer = 0f;
            bot.LastKnownTargetPosition = targetPos;
        }
        else
        {
            _losLostTimer += dt;
            if (_losLostTimer >= bot.Tuning.chaseLosTimeout)
            {
                bot.FSM.ChangeState(new BotSearchState());
                return default;
            }
        }

        // Attack range
        if (canSee && dist <= bot.Tuning.attackRange && bot.Self.Combat != null)
        {
            bot.FSM.ChangeState(new BotAttackState());
            return default;
        }

        // Repath con interception
        _repathTimer -= dt;
        if (_repathTimer <= 0f)
        {
            Vector2 destination;
            if (canSee && target.Motor != null && target.Motor.Velocity.sqrMagnitude > 0.1f)
            {
                float mySpeed = Mathf.Max(0.5f, bot.Self.Motor != null ? bot.Self.Motor.MaxSpeed : 4f);
                float leadTime = Mathf.Min(dist / mySpeed, bot.Tuning.huntLeadTimeMax);
                destination = targetPos + target.Motor.Velocity * leadTime;
            }
            else
            {
                destination = canSee ? targetPos : (Vector2)bot.LastKnownTargetPosition;
            }
            bot.Loco.SetDestination(destination);
            _repathTimer = bot.Tuning.chaseRepathInterval;
        }

        bot.Loco.CheckStuck(dt);

        var intent = new BrainIntent
        {
            MoveInput = bot.Loco.GetSteeringDirection(),
            AimInput  = (targetPos - myPos).normalized
        };

        // Habilidades data-driven: emitir Released pendiente o intentar activar.
        if (_pendingReleaseSlot >= 0)
        {
            SetSlotInput(ref intent, _pendingReleaseSlot, AbilityInputState.Released);
            _pendingReleaseSlot = -1;
        }
        else
        {
            TryFireAbility(bot, target, dist, canSee, myPos, targetPos, ref intent);
        }

        return intent;
    }

    void TryFireAbility(BotBrain bot, Character target, float dist, bool canSee,
                        Vector2 myPos, Vector2 targetPos, ref BrainIntent intent)
    {
        if (_globalAbilityCdRemaining > 0f) return;
        var rules = bot.Tuning.abilityRules;
        if (rules == null || rules.Count == 0) return;

        var ac = bot.Self.Abilities;
        if (ac == null || ac.Abilities == null) return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule.slot < 0 || rule.slot >= MaxSlots) continue;
            if (_slotCdRemaining[rule.slot] > 0f) continue;
            if (rule.slot >= ac.Abilities.Length || ac.Abilities[rule.slot] == null) continue;
            if (!ac.Abilities[rule.slot].IsReady) continue;
            if (dist < rule.minDistance || dist > rule.maxDistance) continue;
            if (rule.requiresLineOfSight && !canSee) continue;
            if (!MatchesCondition(bot, target, rule.condition, myPos, targetPos)) continue;

            // Frame actual: Pressed. Frame siguiente: Released (via _pendingReleaseSlot).
            SetSlotInput(ref intent, rule.slot, AbilityInputState.Pressed);
            _pendingReleaseSlot         = rule.slot;
            _slotCdRemaining[rule.slot] = rule.internalCooldown;
            _globalAbilityCdRemaining   = bot.Tuning.globalAbilityCooldown;
            break;
        }
    }

    static void SetSlotInput(ref BrainIntent intent, int slot, AbilityInputState state)
    {
        switch (slot)
        {
            case 0: intent.Slot0 = state; break;
            case 1: intent.Slot1 = state; break;
            case 2: intent.Slot2 = state; break;
        }
    }

    static bool MatchesCondition(BotBrain bot, Character target, TargetCondition cond,
                                 Vector2 myPos, Vector2 targetPos)
    {
        switch (cond)
        {
            case TargetCondition.None:
                return true;

            case TargetCondition.TargetMoving:
                return target.Motor != null
                    && target.Motor.Velocity.sqrMagnitude
                       > bot.Tuning.targetMovingVelocityThreshold
                       * bot.Tuning.targetMovingVelocityThreshold;

            case TargetCondition.TargetWounded:
                var h = target.Health;
                return h != null && h.CurrentHealth < h.MaxHealth;

            case TargetCondition.TargetIsolated:
            {
                float r2 = bot.Tuning.targetIsolationRadius * bot.Tuning.targetIsolationRadius;
                var world = ServiceLocator.Resolve<IWorldQueryService>();
                if (world == null) return true;
                var allies = world.GetTeam(target.Team);
                if (allies == null) return true;
                for (int i = 0; i < allies.Count; i++)
                {
                    var a = allies[i];
                    if (a == null || a == target || !a.IsAlive) continue;
                    if (((Vector2)a.transform.position - targetPos).sqrMagnitude < r2)
                        return false;
                }
                return true;
            }

            case TargetCondition.TargetFleeingStraight:
            {
                if (target.Motor == null) return false;
                Vector2 vel = target.Motor.Velocity;
                if (vel.sqrMagnitude < 0.1f) return false;
                Vector2 awayFromMe = (targetPos - myPos).normalized;
                float dot = Vector2.Dot(vel.normalized, awayFromMe);
                return dot >= bot.Tuning.targetFleeingStraightDot;
            }
        }
        return false;
    }

    public void Exit(BotBrain bot) { }
}
