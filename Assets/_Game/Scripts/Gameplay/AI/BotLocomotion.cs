using UnityEngine;

#if ASTAR_EXISTS
using Pathfinding;
#endif

/// <summary>
/// Wrapper de A* Pathfinding para bots. Sibling de BotBrain en bots.
///
/// Diseno: AIPath calcula la ruta y expone desiredVelocity, pero NO mueve el
/// transform (updatePosition=false). BotBrain lee GetSteeringDirection() y la inyecta
/// en BrainIntent.MoveInput; CharacterMotor aplica la velocidad al Rigidbody2D.
///
/// Esto mantiene un solo path de movimiento (Motor -> RB2D) compatible con
/// players, bots y futuros NetworkBehaviours en Phase 3.
/// </summary>
public class BotLocomotion : MonoBehaviour
{
    [Header("LOS")]
    [Tooltip("Capa(s) que bloquean la linea de vision del bot. Si queda en 'Everything' al runtime, se resuelve automaticamente a la capa 'Wall'.")]
    [SerializeField] LayerMask wallLayer = ~0;

    public LayerMask WallLayer => wallLayer;

#if ASTAR_EXISTS
    AIPath _ai;
    AIPath AI
    {
        get
        {
            if (_ai == null) _ai = GetComponent<AIPath>();
            return _ai;
        }
    }
#endif

    bool    _hasDestination;
    Vector3 _directDestination; // usado por el fallback no-A* en GetSteeringDirection

    [Header("Stuck recovery")]
    [Tooltip("Segundos sin avanzar antes de forzar un re-path.")]
    [SerializeField] float stuckRecoveryTime  = 0.4f;
    [Tooltip("Desplazamiento minimo por frame para no considerarse atascado.")]
    [SerializeField] float stuckMoveThreshold = 0.08f;

    Vector3 _stuckCheckPos;
    float   _stuckTimer;

    void Awake()
    {
#if ASTAR_EXISTS
        var ai = AI;
        if (ai != null)
        {
            // updatePosition/Rotation=false: A* calcula la ruta y expone desiredVelocity
            // pero NO mueve el transform. CharacterMotor controla el Rigidbody2D.
            ai.updatePosition     = false;
            ai.updateRotation     = false;
            ai.enableRotation     = false;
            ai.gravity            = Vector3.zero;
            ai.canMove            = true;
            ai.orientation        = OrientationMode.YAxisForward;
            // Valor mas bajo → el bot sigue el path mas de cerca y no corta esquinas
            // diagonalmente. Default del prefab era 2, lo que causaba que el bot
            // mirara 2 unidades adelante y tratara de pasar por vertices de muros.
            ai.pickNextWaypointDist = 0.35f;
        }

        // Agregar SimpleSmoothModifier al Seeker si no existe para suavizar waypoints
        var seeker = GetComponent<Pathfinding.Seeker>();
        if (seeker != null && GetComponent<Pathfinding.SimpleSmoothModifier>() == null)
            gameObject.AddComponent<Pathfinding.SimpleSmoothModifier>();
#endif
        // El default ~0 (Everything) rompe LOS porque el raycast pega en el propio
        // collider del bot, suelo, etc. Resolver a la capa "Wall" si existe.
        if (wallLayer.value == -1)
        {
            int wall = LayerMask.NameToLayer("Wall");
            wallLayer = wall >= 0 ? (1 << wall) : 0;
        }

        _stuckCheckPos = transform.position;
    }

    /// <summary>Solicita ruta hacia 'pos'. AIPath repathea internamente segun su repathRate.
    /// Snapea 'pos' al nodo walkable mas cercano para evitar destinos inalcanzables.
    /// Si A* no esta disponible, guarda el destino directo para el fallback de steering.</summary>
    public void SetDestination(Vector3 pos)
    {
        _directDestination = pos;
        _hasDestination    = true;
#if ASTAR_EXISTS
        var ai = AI;
        if (ai != null)
        {
            if (AstarPath.active != null)
            {
                var nn = AstarPath.active.GetNearest(pos, NNConstraint.Default);
                if (nn.node != null) pos = (Vector3)nn.position;
            }
            ai.destination = pos;
            ai.isStopped   = false;
        }
#endif
    }

    public void StopMovement()
    {
#if ASTAR_EXISTS
        var ai = AI;
        if (ai != null) ai.isStopped = true;
#endif
        _hasDestination = false;
    }

    /// <summary>Direccion normalizada del proximo paso de la ruta (Vector2.zero si llego o sin ruta).
    /// Si A* no esta disponible (ASTAR_EXISTS no definido), cae a steering directo
    /// hacia el destino — esto evita que los bots queden completamente inmoviles
    /// si el scripting define se pierde al cambiar de Build Profile.</summary>
    public Vector2 GetSteeringDirection()
    {
#if ASTAR_EXISTS
        var ai = AI;
        if (ai == null || !_hasDestination) return Vector2.zero;
        Vector2 v = ai.desiredVelocity;
        if (v.sqrMagnitude > 0.01f) return v.normalized;
        return Vector2.zero;
#else
        // Fallback: A* no compilado en este Build Profile. Movimiento directo
        // hacia el destino (sin pathfinding). El bot puede quedar atascado en
        // muros pero al menos no esta congelado.
        if (!_hasDestination) return Vector2.zero;
        Vector2 dir = (Vector2)_directDestination - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.04f) return Vector2.zero;
        return dir.normalized;
#endif
    }

    public bool HasReachedDestination(float threshold = 0.5f)
    {
#if ASTAR_EXISTS
        var ai = AI;
        if (ai == null) return true;
        return ai.reachedEndOfPath || ai.remainingDistance < threshold;
#else
        // Fallback no-A*: distancia directa al destino guardado.
        if (!_hasDestination) return true;
        return ((Vector2)_directDestination - (Vector2)transform.position).sqrMagnitude < threshold * threshold;
#endif
    }

    /// <summary>
    /// Detecta si el bot lleva mas de stuckRecoveryTime sin avanzar y fuerza
    /// re-path. Llamar desde BotBrain states cada Tick. Devuelve true si
    /// se detecto atasco y se forzo recuperacion.
    /// </summary>
    public bool CheckStuck(float dt)
    {
#if ASTAR_EXISTS
        if (!_hasDestination)
        {
            _stuckTimer    = 0f;
            _stuckCheckPos = transform.position;
            return false;
        }

        float moved = Vector2.Distance(transform.position, _stuckCheckPos);
        if (moved < stuckMoveThreshold)
        {
            _stuckTimer += dt;
            if (_stuckTimer >= stuckRecoveryTime)
            {
                _stuckTimer    = 0f;
                _stuckCheckPos = transform.position;
                AI?.SearchPath();
                return true;
            }
        }
        else
        {
            _stuckTimer    = 0f;
            _stuckCheckPos = transform.position;
        }
#endif
        return false;
    }

    /// <summary>
    /// Sincroniza la simulatedPosition interna de AIPath con la posicion real del
    /// transform (que mueve el Rigidbody2D via CharacterMotor). Sin esto, AIPath
    /// avanza su posicion simulada de forma independiente y el gizmo (junto con la
    /// logica de path-following) se separa del bot real.
    /// Teleport con clearPath=false no toca el path ni el transform (updatePosition=false).
    /// </summary>
    void LateUpdate()
    {
#if ASTAR_EXISTS
        var ai = AI;
        if (ai != null) ai.Teleport(transform.position, false);
#endif
    }

    /// <summary>Raycast 2D contra wallLayer. True si no hay pared entre 'from' y 'to'.</summary>
    public bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;
        var hit = Physics2D.Raycast(from, dir / dist, dist, wallLayer);
        return hit.collider == null;
    }

    /// <summary>
    /// Evalua N direcciones candidatas (abanico alrededor de awayDir) y devuelve la mejor
    /// segun el clearance contra muros, sesgada hacia direcciones alineadas con awayDir.
    /// Usado por BotFleeState para no quedar atascado contra una pared cuando la direccion
    /// directa de huida esta bloqueada.
    ///
    /// score = clearance * (0.6 + 0.4 * dot(candidate, awayDir))
    ///   → siempre prefiere alejarse, pero acepta lateral si la directa esta tapada.
    /// </summary>
    public static Vector2 FindBestEscapeDirection(
        Vector2 origin, Vector2 awayDir, float maxDist, LayerMask wallMask,
        int samples, float maxAngleDeg, out float bestClearance)
    {
        bestClearance = 0f;
        if (samples < 1) samples = 1;
        if (awayDir.sqrMagnitude < 0.0001f) awayDir = Vector2.right;
        else awayDir = awayDir.normalized;

        Vector2 bestDir   = awayDir;
        float   bestScore = -1f;

        // Genera angulos simetricos alrededor de 0: 0, +step, -step, +2step, -2step, ...
        // Para samples=9, maxAngle=150: 0, ±37.5, ±75, ±112.5, ±150 (5 magnitudes, signed)
        int halfPairs = Mathf.Max(1, samples / 2);
        float angleStep = maxAngleDeg / halfPairs;

        for (int i = 0; i <= halfPairs; i++)
        {
            for (int sign = -1; sign <= 1; sign += 2)
            {
                if (i == 0 && sign == -1) continue; // 0° solo una vez
                float angle = i * angleStep * sign;
                Vector2 candidate = Rotate(awayDir, angle * Mathf.Deg2Rad);

                var hit = Physics2D.Raycast(origin, candidate, maxDist, wallMask);
                float clearance = hit.collider != null ? hit.distance : maxDist;

                float bias  = Vector2.Dot(candidate, awayDir); // 1 directo opuesto, -1 hacia el peligro
                float score = clearance * (0.6f + 0.4f * bias);

                if (score > bestScore)
                {
                    bestScore     = score;
                    bestDir       = candidate;
                    bestClearance = clearance;
                }
            }
        }
        return bestDir;
    }

    static Vector2 Rotate(Vector2 v, float rad)
    {
        float s = Mathf.Sin(rad), c = Mathf.Cos(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
