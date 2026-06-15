using UnityEngine;

/// <summary>
/// Brain bot Phase 1: FSM con pathfinding A*.
///
/// Arquitectura:
///   BotBrain (este componente)
///     - posee una BotStateMachine
///     - referencia un BotTuningData SO para todos los parametros de AI
///     - expone Self / Loco / perception helpers (FindNearestVisibleEnemy, CanSee, FindNearestDownedAlly)
///   BotLocomotion (sibling)
///     - envuelve AIPath/Seeker; calcula GetSteeringDirection()
///   Estados (BotPatrolState, BotChaseState, etc) producen el BrainIntent
///   y leen tuning via bot.Tuning.X
///
/// Hunter inicia en Patrol; Prey en Wander.
/// Phase 2: agregar Utility AI scoring sobre los estados.
/// </summary>
public class BotBrain : MonoBehaviour, IBrain
{
    [Header("Refs")]
    [SerializeField] Character self;

    [Header("Tuning")]
    [Tooltip("ScriptableObject con todos los parametros de AI. Editar el asset cambia comportamiento sin recompilar.")]
    [SerializeField] BotTuningData tuning;

    public Character        Self        => self;
    public BotLocomotion    Loco        { get; private set; }
    public BotStateMachine  FSM         { get; private set; }
    public BotTuningData    Tuning      => tuning;

    public Vector3 Position => transform.position;
    public Character CurrentTarget { get; set; }
    public Vector3   LastKnownTargetPosition { get; set; }

    /// <summary>Target downed teammate que este bot esta yendo a revivir.</summary>
    public Character ReviveTarget { get; set; }

    // true si el tuning fue creado en runtime (fallback) y por tanto este componente
    // es responsable de destruirlo en OnDestroy para no filtrar el ScriptableObject.
    bool _ownsTuning;

    // Consulta del mundo (listas por bando) desacoplada de GameManager.Instance.
    // Cacheada: el servicio se registra en GameManager.Awake antes de spawnear bots.
    IWorldQueryService _world;
    IWorldQueryService World => _world ??= ServiceLocator.Resolve<IWorldQueryService>();

    void Awake()
    {
        if (self == null) self = GetComponent<Character>();
        Loco = GetComponent<BotLocomotion>();
        FSM  = new BotStateMachine(this);

        if (tuning == null)
        {
            Debug.LogWarning($"[BotBrain] {name} sin BotTuningData asignado. Creando instance default en runtime. " +
                             "Asigna un asset al prefab para tunear desde inspector.", this);
            tuning = ScriptableObject.CreateInstance<BotTuningData>();
            _ownsTuning = true;
        }
    }

    void OnDestroy()
    {
        // Solo destruimos el tuning si lo creamos nosotros (fallback). Un asset
        // asignado desde el inspector es compartido y NO debe destruirse.
        if (_ownsTuning && tuning != null)
        {
            Destroy(tuning);
            tuning = null;
        }
    }

    void Start()
    {
        if (self == null) return;
        if (self.Team == CharacterTeam.Hunter)
            FSM.ChangeState(new BotPatrolState());
        else
            FSM.ChangeState(new BotWanderState());
    }

    public BrainIntent CaptureIntent()
    {
        if (self == null || !self.IsAlive) return default;
        return FSM.Tick(Time.deltaTime);
    }

    // -------- Perception helpers --------

    /// <summary>
    /// Devuelve el enemigo VISIBLE con mayor SCORE (no solo el mas cercano).
    /// Scoring:
    ///   base = -distSq                      (mas cerca = mejor)
    ///   +tuning.targetBonusDowned   si IsDowned
    ///   +tuning.targetBonusWounded  si HP < max
    ///   +tuning.targetBonusIsolated si no hay aliados en targetIsolationRadius
    ///
    /// Mantiene los filtros previos: vision range, LOS, IsTargetable, Prey ignora Hunter downed.
    /// </summary>
    public Character FindNearestVisibleEnemy()
    {
        if (self == null) return null;
        var world = World;
        if (world == null) return null;
        var list = world.GetEnemiesOf(self.Team);

        Character best = null;
        float bestScore = float.MinValue;
        float visSqr = tuning.visionRange * tuning.visionRange;
        float isoSqr = tuning.targetIsolationRadius * tuning.targetIsolationRadius;
        Vector2 myPos = Position;

        // Para evaluar aislamiento necesitamos la lista de aliados del target.
        // El target es enemigo de self, asi que sus aliados son el mismo bando enemigo.
        var allyListForTarget = list;

        foreach (var c in list)
        {
            // IsTargetable ahora retorna false para downed → estos quedan
            // excluidos automaticamente. Un Hunter ya no se queda pegado a un
            // Prey derribado: el filtro re-evaluado en cada Patrol/Chase/Attack
            // tick devuelve null o el siguiente Prey vivo.
            if (c == null || !c.IsTargetable) continue;
            // Ocultamiento por estado: un invisible no se percibe; un camuflado solo dentro de su radio.
            if (!StateVisibility.IsPerceivableBy(c, self)) continue;
            Vector2 cp = c.transform.position;
            float sqr = (cp - myPos).sqrMagnitude;
            if (sqr > visSqr) continue;
            if (Loco != null && !Loco.HasLineOfSight(myPos, cp)) continue;

            float score = -sqr;

            // targetBonusDowned eliminado: con el sistema sin finishers no
            // tiene sentido priorizar downed. El campo en BotTuningData queda
            // como dead-data inocuo.

            if (c.Health != null && c.Health.CurrentHealth < c.Health.MaxHealth)
                score += tuning.targetBonusWounded;

            if (IsIsolated(c, allyListForTarget, isoSqr))
                score += tuning.targetBonusIsolated;

            if (score > bestScore)
            {
                bestScore = score;
                best = c;
            }
        }
        return best;
    }

    static bool IsIsolated(Character target, System.Collections.Generic.IReadOnlyList<Character> allies, float isoSqr)
    {
        if (allies == null) return true;
        Vector2 tp = target.transform.position;
        for (int i = 0; i < allies.Count; i++)
        {
            var a = allies[i];
            if (a == null || a == target || !a.IsAlive) continue;
            if (((Vector2)a.transform.position - tp).sqrMagnitude < isoSqr) return false;
        }
        return true;
    }

    /// <summary>
    /// Devuelve el aliado downed mas cercano dentro de visionRange. Solo para Prey bots.
    /// Sin requisito de LOS (los aliados emiten una "señal" simulada).
    /// </summary>
    public Character FindNearestDownedAlly()
    {
        if (self == null || self.Team != CharacterTeam.Prey) return null;
        var world = World;
        if (world == null) return null;

        Character closest = null;
        float minSqr = float.MaxValue;
        float visSqr = tuning.visionRange * tuning.visionRange;
        Vector2 myPos = Position;
        foreach (var c in world.GetAlliesOf(self.Team))
        {
            if (c == null || c == self || !c.IsDowned) continue;
            Vector2 cp = c.transform.position;
            float sqr = (cp - myPos).sqrMagnitude;
            if (sqr > visSqr || sqr >= minSqr) continue;
            minSqr = sqr;
            closest = c;
        }
        return closest;
    }

    public bool CanSee(Transform t)
    {
        if (t == null || Loco == null) return false;
        Vector2 from = Position;
        Vector2 to   = t.position;
        if ((to - from).sqrMagnitude > tuning.visionRange * tuning.visionRange) return false;
        // Si el objetivo se oculto (invisible/camuflaje fuera de radio) el bot pierde el contacto.
        if (t.TryGetComponent<Character>(out var tc) && !StateVisibility.IsPerceivableBy(tc, self)) return false;
        return Loco.HasLineOfSight(from, to);
    }
}
