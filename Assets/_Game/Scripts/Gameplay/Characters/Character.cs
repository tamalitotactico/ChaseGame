using UnityEngine;

/// <summary>
/// Cuerpo gameplay del personaje. NO conoce input ni AI: solo recibe BrainIntent
/// y aplica los sistemas (Motor, Health, Combat, Abilities).
///
/// Phase 0: hereda de MonoBehaviour. Phase 3: cambia a NetworkBehaviour de Fusion;
/// el codigo de Update y los pipes a sistemas no cambian.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CharacterHealth))]
[RequireComponent(typeof(AbilityController))]
public abstract class Character : MonoBehaviour, IDamageable
{
    [Header("Data")]
    [SerializeField]
    protected CharacterData data;

    public abstract CharacterTeam Team { get; }
    public CharacterData Data => data;

    public CharacterMotor Motor { get; private set; }
    public CharacterHealth Health { get; private set; }
    public AbilityController Abilities { get; private set; }
    public CombatController Combat { get; private set; }
    public RevivableComponent Revivable { get; private set; }

    public IAuthorityContext Authority { get; private set; } = LocalAuthority.Instance;
    public Vector2 FacingDirection { get; private set; } = Vector2.right;

    public CharacterStateMachine States { get; private set; }
    public StatusEffectController StatusEffects { get; private set; }

    IBrain _brain;

    protected virtual void Awake()
    {
        Motor = GetComponent<CharacterMotor>();
        Health = GetComponent<CharacterHealth>();
        Abilities = GetComponent<AbilityController>();
        Combat = GetComponent<CombatController>();
        Revivable = GetComponent<RevivableComponent>();
        StatusEffects = GetComponent<StatusEffectController>();
        States = new CharacterStateMachine(this);

        Health.OnDied += HandleDied;
    }

    protected virtual void Start()
    {
        Initialize();
        States.ChangeState(new IdleState());
    }

    /// <summary>Aplica los datos del CharacterData a los componentes.</summary>
    public virtual void Initialize()
    {
        if (data == null)
        {
            Debug.LogWarning($"[Character] {name} sin CharacterData asignado.", this);
            return;
        }

        Motor.Setup(data.baseSpeed);
        Health.Setup(data.maxHealth, data.invulnerabilityOnHit);
        Abilities.Bind(data.abilities);

        if (Combat != null && data.hasBasicAttack)
            Combat.Setup(data.attackRange, data.attackCooldown, data.attackDamage);
    }

    public void SetBrain(IBrain brain)
    {
        _brain = brain;
    }

    public void SetAuthority(IAuthorityContext auth)
    {
        Authority = auth ?? LocalAuthority.Instance;
    }

    protected virtual void Update()
    {
        if (!Authority.CanSimulate)
        {
            if (MovementTrace.Enabled)
                MovementTrace.Log("Authority", "{0} CanSimulate=false, skipping Update", name);
            return;
        }

        // Downed: el brain no produce intent (sin movimiento ni ataque). DownedState
        // tickea el Revivable y maneja transiciones a Idle (revivido) o Dead (bleed-out).
        if (IsDowned)
        {
            States.Tick(Time.deltaTime);
            return;
        }

        if (!Health.IsAlive)
        {
            States.Tick(Time.deltaTime);
            return;
        }

        BrainIntent intent = _brain != null ? _brain.CaptureIntent() : default;
        if (MovementTrace.Enabled)
            MovementTrace.Log("Character", "{0} brain={1} intent=({2:F2},{3:F2})",
                name, _brain != null ? _brain.GetType().Name : "null",
                intent.MoveInput.x, intent.MoveInput.y);

        if (StatusEffects != null && !StatusEffects.CanAct)
        {
            if (MovementTrace.Enabled)
                MovementTrace.Log("StatusFX", "{0} CanAct=false (clearing actions)", name);
            intent = intent.WithActionsCleared();
        }

        Vector2 moveInput = intent.MoveInput;
        if (StatusEffects != null)
        {
            var forced = StatusEffects.GetForceMoveInput();
            if (forced.HasValue)
            {
                if (MovementTrace.Enabled)
                    MovementTrace.Log("StatusFX", "{0} forced move=({1:F2},{2:F2})", name, forced.Value.x, forced.Value.y);
                moveInput = forced.Value;
            }
        }

        if (moveInput.sqrMagnitude > 0.01f)
            FacingDirection = moveInput.normalized;

        Motor.SetMoveInput(moveInput);
        if (MovementTrace.Enabled)
            MovementTrace.Log("Motor", "{0} setMove=({1:F2},{2:F2}) vel=({3:F2},{4:F2})",
                name, moveInput.x, moveInput.y, Motor.Velocity.x, Motor.Velocity.y);
        Abilities.Tick(in intent, Time.deltaTime);
        if (Combat != null && data != null && data.hasBasicAttack)
            Combat.Tick(in intent, Time.deltaTime);

        States.Tick(Time.deltaTime);
    }

    void HandleDied()
    {
        // Sin muerte real: si tiene RevivableComponent va a Downed (revivible
        // indefinido). Sin RevivableComponent (Hunter), solo se detiene el motor
        // y queda en su state actual — nunca se transiciona a DeadState.
        Motor.Stop();
        if (Revivable != null)
        {
            Revivable.BeginDown();
            EventBus.Publish(new CharacterDownedEvent { Character = this });
            States.ChangeState(new DownedState());
        }
    }

    // IDamageable
    public bool IsAlive => Health != null && Health.IsAlive;
    public bool IsDowned => Revivable != null && Revivable.IsDowned;
    // Downed ya no es objetivo de combate (KillFromFinish es no-op, sin
    // finishers). El revive lo maneja RevivableComponent escaneando Preys con
    // c.IsDowned directamente, sin pasar por IsTargetable. Si en el futuro se
    // reactiva DeadState/finishers, restaurar a (IsAlive || IsDowned).
    public bool IsTargetable => IsAlive;

    public void TakeDamage(in DamageInfo info)
    {
        // Sin muerte real: los downed son invulnerables (los finishers no aplican).
        // Health == null: prefab mal configurado sin CharacterHealth; evitar NRE.
        if (IsDowned || Health == null)
            return;

        if (Health.TryDamage(in info))
            EventBus.Publish(
                new CharacterDamagedEvent
                {
                    Character = this,
                    CurrentHealth = Health.CurrentHealth,
                    MaxHealth = Health.MaxHealth,
                }
            );
    }
}
