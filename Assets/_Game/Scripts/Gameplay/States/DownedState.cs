using UnityEngine;

/// <summary>
/// Prey con HP=0 pero aun salvable. Movimiento deshabilitado (motor speed=0).
/// El RevivableComponent del Character maneja la logica de revive/bleed-out y
/// dispara eventos OnRevived / OnBleedOut. Este state escucha y transiciona.
///
/// Phase 1+: agregar crawling lento (motor.SpeedMultiplier = 0.3f) y animacion.
/// </summary>
public class DownedState : ICharacterState
{
    RevivableComponent _rev;
    bool _subscribed;
    bool _shouldRevive;

    public void Enter(Character c)
    {
        if (c.Motor != null)
        {
            c.Motor.Stop();
            c.Motor.SpeedMultiplier = 0f;
        }

        _rev = c.Revivable;
        if (_rev != null && !_subscribed)
        {
            _rev.OnRevived += OnRevived;
            _subscribed = true;
        }
    }

    public void Tick(Character c, float dt)
    {
        if (_rev != null) _rev.Tick(dt);

        if (_shouldRevive)
        {
            // Restaurar speed y volver a vida normal
            if (c.Motor != null) c.Motor.SpeedMultiplier = 1f;
            c.States.ChangeState(new IdleState());
            return;
        }
    }

    public void Exit(Character c)
    {
        if (_rev != null && _subscribed)
        {
            _rev.OnRevived -= OnRevived;
            _subscribed = false;
        }
        // Asegurar que speed quede restaurada si salimos por cualquier via
        if (c.Motor != null) c.Motor.SpeedMultiplier = 1f;
    }

    void OnRevived() => _shouldRevive = true;
}
