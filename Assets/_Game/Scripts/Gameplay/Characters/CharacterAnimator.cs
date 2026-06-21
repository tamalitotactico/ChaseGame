using UnityEngine;

/// <summary>
/// Control de animacion 8-direccional dirigido por el ESTADO LOGICO del Character.
/// No conoce input ni AI: lee Character.States.Current (Idle/Move/Attack/Downed/...) para
/// elegir el SET de animacion, y Character.FacingDirection para el octante (8 direcciones).
///
/// El Animator Controller es solo una bolsa de estados, SIN transiciones: este componente
/// decide cual reproducir via Animator.Play(hash). Convencion de nombres de estado:
///   {Set}_{Dir}   con Set in {Idle,Run,Attack,Downed,Dead,Injured}
///                 y  Dir in {E,NE,N,NW,W,SW,S,SE}.   Ej: Run_NE, Idle_S.
///
/// Si un estado/octante no existe en el controller, cae a idleFallback (ej. Idle_S).
/// Asi, con solo los 8 Run_* + Idle_S actuales ya funciona, y se "enciende" solo al agregar
/// mas clips (idles direccionales, attack, downed/fantasma) sin tocar este codigo.
///
/// Setup:
///   - Animator en el MISMO GameObject que el SpriteRenderer (los clips keyean SpriteRenderer.sprite).
///   - Este componente en el root del Character (encuentra el Animator via GetComponentInChildren).
///   - El controller no necesita transiciones; default state = Idle_S.
///
/// Nota Phase 3: la animacion sigue la FSM, que solo tickea cuando el Character simula
/// (Authority.CanSimulate). Un personaje remoto no-simulado quedaria en su ultimo estado;
/// en networking se driveara desde el estado replicado.
/// </summary>
[RequireComponent(typeof(Character))]
public class CharacterAnimator : MonoBehaviour
{
    enum AnimSet { Idle = 0, Run, Attack, Downed, Dead, Injured }

    static readonly string[] SetPrefix =
        { "Idle_", "Run_", "Attack_", "Downed_", "Dead_", "Injured_" };

    // Orden por octante de atan2 (CCW desde +X): 0=E,1=NE,2=N,3=NW,4=W,5=SW,6=S,7=SE
    static readonly string[] DirSuffix =
        { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

    [SerializeField] Animator animator;
    [Tooltip("Estado a reproducir cuando el clip pedido no existe en el controller (ej. Idle_S).")]
    [SerializeField] string idleFallback = "Idle_S";

    Character _char;

    // Tabla resuelta [set, octante] -> hash de estado del Animator (o fallback si no existe).
    int[,] _stateHash;
    int    _fallbackHash;

    ICharacterState _lastLogicState;
    AnimSet _set = AnimSet.Idle;
    int     _current; // hash actualmente reproduciendose (evita re-Play por frame)

    void Awake()
    {
        _char = GetComponent<Character>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        RebuildStateTable();
    }

    /// <summary>
    /// (Re)resuelve la tabla [set, octante] -> hash de estado contra el controller ACTUAL del Animator.
    /// Llamar tras intercambiar runtimeAnimatorController en runtime (skins, ver GameManager.SpawnOne):
    /// HasState depende del controller asignado, asi que sin re-resolver el sprite no animaria.
    /// </summary>
    public void RebuildStateTable()
    {
        _fallbackHash = Animator.StringToHash(idleFallback);

        int sets = SetPrefix.Length;
        _stateHash = new int[sets, 8];
        for (int s = 0; s < sets; s++)
        {
            for (int d = 0; d < 8; d++)
            {
                int h = Animator.StringToHash(SetPrefix[s] + DirSuffix[d]);
                // HasState una sola vez aqui (no por frame). Si el clip no existe aun, fallback.
                _stateHash[s, d] = (animator != null && animator.HasState(0, h)) ? h : _fallbackHash;
            }
        }
        _current = 0; // fuerza re-Play en el proximo Update con la nueva tabla
    }

    void Update()
    {
        if (animator == null || _char == null) return;

        // En un proxy de red (cliente, sin state authority) la FSM esta congelada: el SET
        // viene replicado desde el host (NetworkedAnimSet). En host/solo se deriva de la FSM local.
        if (_char.AnimFromNetwork)
        {
            _set = (AnimSet)Mathf.Clamp(_char.NetworkedAnimSet, 0, SetPrefix.Length - 1);
        }
        else
        {
            // SET segun el estado logico. Solo recomputa el mapeo cuando cambia la instancia
            // de estado (no hace type-dispatch cada frame en regimen estable).
            var logic = _char.States != null ? _char.States.Current : null;
            if (!ReferenceEquals(logic, _lastLogicState))
            {
                _lastLogicState = logic;
                _set = MapSet(logic);
            }
        }

        int oct  = DirToOctant(_char.FacingDirection);
        int hash = _stateHash[(int)_set, oct];
        if (hash != _current)
        {
            _current = hash;
            animator.Play(hash);
        }
    }

    /// <summary>Indice del AnimSet (0..N) para un estado logico. Lo usa el host para replicar la
    /// animacion a los clientes (Character escribe (byte)SetIndexFor(States.Current)).</summary>
    public static int SetIndexFor(ICharacterState s) => (int)MapSet(s);

    static AnimSet MapSet(ICharacterState s)
    {
        switch (s)
        {
            case MoveState _:    return AnimSet.Run;
            case AttackState _:  return AnimSet.Attack;
            case DownedState _:  return AnimSet.Downed;
            case DeadState _:    return AnimSet.Dead;
            case InjuredState _: return AnimSet.Injured;
            default:             return AnimSet.Idle; // IdleState o null
        }
    }

    // atan2 da angulo CCW desde +X; /45 redondeado -> octante 0..7.
    static int DirToOctant(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return 6; // South por defecto (idle inicial)
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (ang < 0f) ang += 360f;
        return Mathf.RoundToInt(ang / 45f) % 8;
    }
}
