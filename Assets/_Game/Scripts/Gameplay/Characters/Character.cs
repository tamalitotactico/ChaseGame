using UnityEngine;
#if FUSION2
using Fusion;
#endif

/// <summary>
/// Cuerpo gameplay del personaje. NO conoce input ni AI: solo recibe BrainIntent
/// y aplica los sistemas (Motor, Health, Combat, Abilities).
///
/// Con FUSION2 hereda de NetworkBehaviour: el host (StateAuthority) simula en FixedUpdateNetwork
/// (input via GetInput para humanos, BotBrain para bots) y NetworkRigidbody2D replica el movimiento.
/// El loop Update queda como path NO-red (preview harness / sin runner). Sin FUSION2 es MonoBehaviour
/// y todo corre en Update como antes.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CharacterHealth))]
[RequireComponent(typeof(AbilityController))]
#if FUSION2
public abstract class Character : NetworkBehaviour, IDamageable
#else
public abstract class Character : MonoBehaviour, IDamageable
#endif
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

    /// <summary>True si este Character es un proxy de red (cliente sin state authority): su FSM esta
    /// congelada y la animacion debe leerse del estado replicado. False en host/solo. La lee CharacterAnimator.</summary>
    public bool AnimFromNetwork =>
#if FUSION2
        IsNetworked && Object != null && !Object.HasStateAuthority;
#else
        false;
#endif

    /// <summary>Indice de AnimSet replicado desde el host (valido solo si AnimFromNetwork).</summary>
    public int NetworkedAnimSet =>
#if FUSION2
        NetAnimSet;
#else
        0;
#endif

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

    /// <summary>
    /// Reemplaza el CharacterData en runtime y re-inicializa todos los sistemas (stats,
    /// abilities, ataque basico) + reconstruye la tabla del CharacterAnimator. Lo usa el
    /// Character Preview Harness (Tools > Chase Game) y, a futuro, GameManager para inyectar
    /// el personaje equipado sobre el prefab base sin necesidad de 8 prefabs distintos.
    /// </summary>
    public void SetData(CharacterData newData)
    {
        data = newData;
        Initialize();
        var anim = GetComponentInChildren<CharacterAnimator>();
        if (anim != null) anim.RebuildStateTable();
    }

    public void SetAuthority(IAuthorityContext auth)
    {
        Authority = auth ?? LocalAuthority.Instance;
    }

    protected virtual void Update()
    {
#if FUSION2
        // En red, simulacion = FixedUpdateNetwork y presentacion/sync = Render(). Update no hace nada.
        if (IsNetworked) return;
#endif
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

#if FUSION2
    // --- Path de RED (Fusion). Activo solo cuando el objeto esta spawneado por un runner. ---

    bool IsNetworked => Object != null && Object.IsValid;

    /// <summary>Marcado por GameManager.SpawnNetworkedCharacter (onBeforeSpawned) antes de Spawned:
    /// indica que este personaje es un bot, para que el host le agregue BotBrain.</summary>
    public bool NetworkIsBot { get; set; }

    // --- Estado replicado ---
    // El host (StateAuthority) escribe al final de cada FixedUpdateNetwork; los clientes lo leen por
    // POLLING en Render() (Fusion 2 no tiene el callback OnChanged/Changed<T> de Fusion 1; se usa
    // ChangeDetector o, como aqui, lectura idempotente cada frame de presentacion).
    [Networked] int   SyncedHealth        { get; set; }
    [Networked] bool  SyncedIsDowned      { get; set; }
    [Networked] float SyncedReviveProgress { get; set; } // 0..1, para la barra de revive en el cliente

    // Cooldowns replicados para el HUD del cliente local (segundos restantes, 0..Data.cooldown).
    [Networked] float Cooldown0 { get; set; }
    [Networked] float Cooldown1 { get; set; }
    [Networked] float Cooldown2 { get; set; }

    // Animacion + efectos de estado replicados (para que el cliente, con la FSM congelada y
    // StatusEffectController vacio, vea animaciones e iconos/tint). Host escribe en FixedUpdateNetwork.
    [Networked] byte    NetAnimSet    { get; set; } // indice de AnimSet (CharacterAnimator.SetIndexFor)
    [Networked] int     NetStatusMask { get; set; } // mascara de tipos de efecto activos
    [Networked] float   NetBlindMult  { get; set; } // magnitud del blind (1 = sin blind) para FOV local
    [Networked] Vector2 NetFacing     { get; set; } // direccion de mirada (octante de animacion) replicada

    /// <summary>HOST: escribe la animacion (set de la FSM) y la mascara de efectos activos para que
    /// los clientes (FSM congelada, StatusEffectController vacio) reproduzcan animaciones e iconos/tint.</summary>
    void WriteNetVisualState()
    {
        NetAnimSet    = (byte)CharacterAnimator.SetIndexFor(States != null ? States.Current : null);
        NetStatusMask = StatusEffects != null ? StatusEffects.GetActiveTypeMask() : 0;
        NetBlindMult  = StatusEffects != null ? StatusEffects.GetBlindMultiplier() : 1f;
        NetFacing     = FacingDirection;
        SyncedReviveProgress = (Revivable != null && Revivable.IsDowned) ? Revivable.ReviveProgress : 0f;
    }

    /// <summary>Aplica el estado replicado en clientes no-host. Idempotente: NetworkSync ignora
    /// valores sin cambio y solo dispara eventos en transiciones reales.</summary>
    void ApplySyncedState()
    {
        Health?.NetworkSync(SyncedHealth);
        StatusEffects?.SyncFromMask(NetStatusMask, NetBlindMult);

        bool nowDowned = SyncedIsDowned;
        bool wasDowned = IsDowned; // lee Revivable.IsDowned
        if (nowDowned && !wasDowned && Revivable != null && !Revivable.IsDowned)
        {
            // Puede que el sync de health ya lo haya activado via HandleDied; el guard lo evita.
            Revivable.BeginDown();
            EventBus.Publish(new CharacterDownedEvent { Character = this });
            States.ChangeState(new DownedState());
        }
        else if (!nowDowned && wasDowned)
        {
            // Revive sincronizado desde el host. CRITICO: limpiar el estado downed del Revivable, o
            // wasDowned seguiria true y este branch se re-dispararia cada frame (particulas masivas de
            // revive + corazon roto permanente). El heal real llega via SyncedHealth.
            Revivable.NetworkClearDowned();
            States.ChangeState(new IdleState());
            EventBus.Publish(new CharacterRevivedEvent { Character = this });
        }

        // Barra de progreso de revive en el cliente (el host tickea el revive; aqui lo reflejamos).
        if (nowDowned && Revivable != null)
        {
            EventBus.Publish(new ReviveProgressChangedEvent
            {
                Character         = this,
                Progress          = SyncedReviveProgress,
                BleedOutRemaining = Revivable.BleedOutDuration, // bleed-out desactivado: full
                HasReviver        = SyncedReviveProgress > 0.001f,
            });
        }
    }

    public override void Spawned()
    {
        SetAuthority(new FusionAuthority(Object));
        Motor.SetNetworkedDriven(true);

        // Por si Spawned corre antes que Start: asegurar init de stats/abilities.
        if (data != null) Initialize();

        // Registrar en las listas del match (world-query/bots/condiciones de victoria).
        var gm = GameManager.Instance;
        if (gm != null)
        {
            var list = Team == CharacterTeam.Hunter ? gm.Hunters : gm.Preys;
            if (!list.Contains(this)) list.Add(this);
        }

        // FoW: TODOS los characters necesitan CharacterFogVisibility para que la niebla del viewer
        // local los oculte. En Solo lo agrega FogOfWarManager via CharacterSpawnedEvent (que GameManager
        // publica para todos); en red ese evento solo se publica para el jugador local, asi que aqui lo
        // aseguramos para todos (bots y remotos incluidos) o quedarian siempre visibles en el cliente.
        if (GetComponent<CharacterFogVisibility>() == null)
            gameObject.AddComponent<CharacterFogVisibility>();

        // Bots: solo el host (StateAuthority) corre la AI. ConfigureBot agrega BotBrain + AI.
        if (Object.HasStateAuthority && NetworkIsBot)
            gm?.ConfigureBot(gameObject);

        if (Object.HasInputAuthority)
        {
            // Setup del jugador LOCAL de este cliente: PlayerBrain (fuente de input para
            // FusionInputCollector via PlayerBrain.Local), indicador de aim y aviso a camara/HUD.
            if (GetComponent<PlayerBrain>() == null) gameObject.AddComponent<PlayerBrain>();
            var indicator = GetComponent<AbilityIndicatorView>() ?? gameObject.AddComponent<AbilityIndicatorView>();
            if (gm != null && gm.IndicatorRegistry != null) indicator.SetRegistry(gm.IndicatorRegistry);

            // Cliente puro (input pero NO state authority): la simulacion de abilities corre en el host.
            // Para que el indicador de aim se vea localmente, el AbilityController corre en modo preview
            // (fase de aim local, sin ejecutar). El host es el unico que ejecuta. El host-jugador NO entra
            // aqui (tiene StateAuthority) y usa el Tick normal de FixedUpdateNetwork.
            if (!Object.HasStateAuthority) Abilities?.SetPreviewOnly(true);

            EventBus.Publish(new CharacterSpawnedEvent { Character = this });
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Host (StateAuthority) simula; los clientes reciben via NetworkRigidbody2D.
        if (!Object.HasStateAuthority) return;

        float dt = Runner.DeltaTime;

        if (IsDowned || (Health != null && !Health.IsAlive))
        {
            Motor.SetMoveInput(Vector2.zero);
            Motor.NetworkTick(dt);
            States.Tick(dt);
            SyncedHealth   = Health != null ? Health.CurrentHealth : 0;
            SyncedIsDowned = IsDowned;
            WriteNetVisualState();
            return;
        }

        // Input: humano -> input replicado (GetInput); bot (host) -> BotBrain directo.
        BrainIntent intent;
        if (GetInput(out NetworkInputData netInput)) intent = netInput.ToIntent();
        else intent = _brain != null ? _brain.CaptureIntent() : default;

        if (StatusEffects != null && !StatusEffects.CanAct)
            intent = intent.WithActionsCleared();

        Vector2 moveInput = intent.MoveInput;
        if (StatusEffects != null)
        {
            var forced = StatusEffects.GetForceMoveInput();
            if (forced.HasValue) moveInput = forced.Value;
        }

        // Facing autoritativo desde el input de movimiento (se replica via NetFacing): el cliente NO
        // puede derivarlo de la velocidad del RB proxy (es ~0), por eso la animacion miraba a la derecha.
        if (moveInput.sqrMagnitude > 0.01f) FacingDirection = moveInput.normalized;

        Motor.SetMoveInput(moveInput);
        Motor.NetworkTick(dt);
        Abilities.Tick(in intent, dt);
        if (Combat != null && data != null && data.hasBasicAttack)
            Combat.Tick(in intent, dt);
        States.Tick(dt);

        // Replicar estado de salud/downed + cooldowns + animacion/efectos al final del tick autoritativo.
        SyncedHealth   = Health != null ? Health.CurrentHealth : 0;
        SyncedIsDowned = IsDowned;
        var abs = Abilities?.Abilities;
        if (abs != null)
        {
            Cooldown0 = abs.Length > 0 && abs[0] != null ? abs[0].CooldownRemaining : 0f;
            Cooldown1 = abs.Length > 1 && abs[1] != null ? abs[1].CooldownRemaining : 0f;
            Cooldown2 = abs.Length > 2 && abs[2] != null ? abs[2].CooldownRemaining : 0f;
        }
        WriteNetVisualState();
    }

    /// <summary>
    /// Render de Fusion: corre tras la simulacion, una vez por frame de Unity, en TODOS los peers.
    /// Aqui va toda la presentacion y el consumo de estado replicado:
    ///  - Facing derivado de la velocidad replicada (correcto en remotos: NetworkRigidbody2D la replica).
    ///  - Clientes no-host: aplican salud/downed replicados.
    ///  - Jugador local en cliente no-host: empuja cooldowns replicados al HUD.
    /// </summary>
    public override void Render()
    {
        if (!IsNetworked) return;

        if (Object.HasStateAuthority)
        {
            // Host: facing desde la velocidad real del RB (refina el seteado en FixedUpdateNetwork).
            if (Motor != null)
            {
                Vector2 v = Motor.Velocity;
                if (v.sqrMagnitude > 0.01f) FacingDirection = v.normalized;
            }
        }
        else
        {
            // Cliente: facing REPLICADO (la velocidad del RB proxy es ~0 -> la animacion miraba siempre
            // a la derecha). NetFacing lo escribe el host desde el input de movimiento.
            if (NetFacing.sqrMagnitude > 0.01f) FacingDirection = NetFacing;

            ApplySyncedState();

            if (Object.HasInputAuthority && Abilities != null)
            {
                Abilities.PushCooldownForDisplay(0, Cooldown0);
                Abilities.PushCooldownForDisplay(1, Cooldown1);
                Abilities.PushCooldownForDisplay(2, Cooldown2);

                // Preview local de aim: reusa el ultimo intent del PlayerBrain (NO recapturar: consume
                // edges que necesita la replicacion de input). Dibuja el indicador sin ejecutar.
                // dt = Time.deltaTime: Render corre por-frame, asi la barra de channeling avanza en tiempo real.
                var pb = PlayerBrain.Local;
                if (pb != null) Abilities.DriveAimPreview(pb.LastIntent, Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Punto de entrada de emotes: en red envia el RPC a TODOS (incl. emisor); en Solo dispara
    /// el EventBus localmente. EmoteWheelHUD llama esto en lugar de publicar EventBus directamente.
    /// </summary>
    public void TriggerEmoteForNetwork(string emoteId, bool fromGhost, Vector3 ghostPos)
    {
        if (IsNetworked)
        {
            RPC_Emote(emoteId, fromGhost, ghostPos);
            return;
        }
        EventBus.Publish(new EmoteUsedEvent
        {
            Source    = this,
            EmoteId   = emoteId,
            FromGhost = fromGhost,
            GhostPos  = ghostPos,
            BodyPos   = transform.position,
        });
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    void RPC_Emote(string emoteId, bool fromGhost, Vector3 ghostPos)
    {
        EventBus.Publish(new EmoteUsedEvent
        {
            Source    = this,
            EmoteId   = emoteId,
            FromGhost = fromGhost,
            GhostPos  = ghostPos,
            BodyPos   = transform.position,
        });
    }
#endif

    void HandleDied()
    {
        Motor.Stop();
        // Guard !IsDowned: en MP la sincronizacion de red puede llegar doble (via
        // NetworkSync y via SyncedIsDowned); el primero que llega gana.
        if (Revivable != null && !Revivable.IsDowned)
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
        {
            EventBus.Publish(
                new CharacterDamagedEvent
                {
                    Character = this,
                    CurrentHealth = Health.CurrentHealth,
                    MaxHealth = Health.MaxHealth,
                }
            );

            // Solo ataques basicos que conectan cuentan para la carga de ultimates de hunter.
            if (info.FromBasicAttack && info.Source != null)
                EventBus.Publish(new BasicAttackLandedEvent { Attacker = info.Source, Victim = this });
        }
    }
}
