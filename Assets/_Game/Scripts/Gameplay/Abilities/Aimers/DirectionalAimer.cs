using UnityEngine;

/// <summary>
/// Aimer para abilities direccionales (proyectiles, dash apuntado). Lee AimInput
/// del BrainIntent; si no hay aim activo, usa MoveInput; si tampoco hay, mantiene
/// la direccion de facing.
///
/// Comportamiento Brawl Stars al soltar:
///   - Si el jugador nunca apunto explicitamente (tap puro) → Fire con la
///     direccion de fallback (facing/movimiento). Permite uso rapido sin drag.
///   - Si el jugador apunto y mantuvo el drag al soltar → Fire en esa direccion.
///   - Si el jugador apunto y devolvio el handle al centro (cancel gesture) →
///     Cancel: la habilidad no se ejecuta.
///
/// El flag AimResult.Explicit es true solo cuando el jugador confirmo aim activo
/// al momento del release (apuntado real, no fallback).
/// </summary>
public sealed class DirectionalAimer : Aimer
{
    Vector2 _direction;
    bool    _everExplicit;     // true si en algun momento del hold el aim fue explicito
    bool    _currentlyAiming;  // true si el AimInput de este frame es explicito

    protected override void OnBegin()
    {
        _direction = Ctx.FacingDirection.sqrMagnitude > 0f
            ? Ctx.FacingDirection
            : Vector2.right;
        _everExplicit    = false;
        _currentlyAiming = false;
    }

    public override void Tick(in BrainIntent intent, float dt)
    {
        Vector2 aim  = intent.AimInput;
        Vector2 move = intent.MoveInput;
        bool aimMatchesMove = (aim - move).sqrMagnitude < 0.0001f;

        bool explicitAim = aim.sqrMagnitude > 0.01f && !aimMatchesMove;
        _currentlyAiming = explicitAim;

        if (explicitAim)
        {
            _direction    = aim.normalized;
            _everExplicit = true;
        }
        else if (move.sqrMagnitude > 0.01f)
        {
            _direction = move.normalized;
            // movimiento del joystick principal NO cuenta como aim explicito
        }
        // si no hay input, mantiene la ultima direccion conocida
    }

    public override ReleaseDecision HandleRelease(in BrainIntent intent)
    {
        // Cancel gesture: el jugador apunto y devolvio el handle al centro.
        if (_everExplicit && !_currentlyAiming)
            return ReleaseDecision.Cancel;

        // Tap puro o drag confirmado: disparar.
        return ReleaseDecision.Fire;
    }

    public override AimResult GetResult() => new AimResult
    {
        Direction    = _direction,
        HasDirection = true,
        Explicit     = _everExplicit && _currentlyAiming
    };

    public override Vector2 CurrentDirection => _direction;
}
