using UnityEngine;

/// <summary>
/// Aimer compuesto: primero fase de aim (jugador apunta con drag), al Released
/// transiciona a fase de cast (canalizacion de duracion fija con la direccion
/// capturada). Patron usado por TeleportSmash y otras habilidades que requieren
/// elegir direccion ANTES de canalizar.
///
/// Comportamiento Brawl Stars:
///   - Press → empieza fase de aim. La UI muestra preview pero NO la barra de
///     canalizacion (IsCasting es false en esta fase).
///   - Drag → actualiza direccion. IsCancellable=true (no se ha comprometido).
///   - Released con aim devuelto al centro → Cancel: la habilidad no se lanza.
///   - Released con aim apuntado (o tap puro) → transiciona a fase de cast con
///     la direccion final congelada. Owner queda inmovilizado, IsCancellable=false.
///   - Timer expira → IsComplete=true, AbilityController ejecuta.
///
/// La direccion se congela al pasar a fase de cast: drag posterior NO la cambia
/// (a diferencia del CastAimer viejo).
/// </summary>
public sealed class AimThenCastAimer : Aimer
{
    enum Phase { Aim, Cast }

    readonly float _castDuration;

    Phase   _phase = Phase.Aim;
    Vector2 _direction;
    bool    _everExplicit;
    bool    _currentlyAiming;
    float   _elapsed;

    public override bool  IsCasting        => _phase == Phase.Cast;
    public override bool  IsCancellable    => _phase == Phase.Aim;
    public override bool  IsComplete       => _phase == Phase.Cast && _elapsed >= _castDuration;
    public override float Progress         => _phase == Phase.Cast && _castDuration > 0f
                                              ? Mathf.Clamp01(_elapsed / _castDuration)
                                              : 0f;
    public override float RemainingSeconds => _phase == Phase.Cast
                                              ? Mathf.Max(0f, _castDuration - _elapsed)
                                              : 0f;

    public AimThenCastAimer(float castDuration)
    {
        _castDuration = Mathf.Max(0f, castDuration);
    }

    protected override void OnBegin()
    {
        _phase     = Phase.Aim;
        _direction = Ctx.FacingDirection.sqrMagnitude > 0f
            ? Ctx.FacingDirection
            : Vector2.right;
        _everExplicit    = false;
        _currentlyAiming = false;
        _elapsed         = 0f;
    }

    public override void Tick(in BrainIntent intent, float dt)
    {
        if (_phase == Phase.Aim)
        {
            Vector2 aim  = intent.AimInput;
            Vector2 move = intent.MoveInput;
            bool aimMatchesMove = (aim - move).sqrMagnitude < 0.0001f;
            bool explicitAim    = aim.sqrMagnitude > 0.01f && !aimMatchesMove;

            _currentlyAiming = explicitAim;
            if (explicitAim)
            {
                _direction    = aim.normalized;
                _everExplicit = true;
            }
            else if (move.sqrMagnitude > 0.01f)
            {
                _direction = move.normalized;
            }
        }
        else
        {
            _elapsed += dt;
            // Mantener al owner inmovil durante la canalizacion
            if (!IsComplete && Ctx.Owner != null && Ctx.Owner.Motor != null)
                Ctx.Owner.Motor.Stop();
        }
    }

    public override ReleaseDecision HandleRelease(in BrainIntent intent)
    {
        if (_phase != Phase.Aim) return ReleaseDecision.Continue;

        // Cancel gesture: apunto y devolvio el handle al centro.
        if (_everExplicit && !_currentlyAiming)
            return ReleaseDecision.Cancel;

        // Comprometerse: transicionar a fase de cast. La direccion queda fija.
        _phase   = Phase.Cast;
        _elapsed = 0f;
        if (Ctx.Owner != null && Ctx.Owner.Motor != null) Ctx.Owner.Motor.Stop();
        return ReleaseDecision.Continue;
    }

    public override AimResult GetResult() => new AimResult
    {
        Direction    = _direction,
        HasDirection = true,
        Explicit     = _everExplicit
    };

    public override Vector2 CurrentDirection => _direction;
}
