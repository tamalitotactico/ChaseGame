using UnityEngine;

/// <summary>
/// Aimer compuesto para habilidades que apuntan a un PUNTO y luego canalizan: primero fase de aim
/// (offset desde el owner, clampeado a maxRange, como AreaAimer), al Released transiciona a fase de
/// cast (canalizacion de duracion fija con el punto capturado). Analogo a AimThenCastAimer pero el
/// resultado es una POSICION, no una direccion. Usado por Ejecucion (Drowned).
/// </summary>
public sealed class AreaThenCastAimer : Aimer
{
    enum Phase { Aim, Cast }

    readonly float _castDuration;
    readonly float _maxRange;

    Phase   _phase = Phase.Aim;
    Vector2 _targetWorld;
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

    // El indicador necesita el PUNTO apuntado (no solo direccion) para dibujar el AoE en el destino.
    public override Vector2 CurrentDirection
    {
        get
        {
            Vector2 d = _targetWorld - Ctx.OwnerPosition;
            return d.sqrMagnitude > 0.0001f ? d.normalized : base.CurrentDirection;
        }
    }
    public override Vector2? CurrentTarget => _targetWorld;

    public AreaThenCastAimer(float castDuration, float maxRange)
    {
        _castDuration = Mathf.Max(0f, castDuration);
        _maxRange     = maxRange;
    }

    protected override void OnBegin()
    {
        _phase       = Phase.Aim;
        _targetWorld = Ctx.OwnerPosition;
        _elapsed     = 0f;
    }

    public override void Tick(in BrainIntent intent)
    {
        if (_phase == Phase.Aim)
        {
            Vector2 offset = intent.AimInput.sqrMagnitude > 0.01f ? intent.AimInput : intent.MoveInput;
            if (offset.sqrMagnitude > 0.01f)
                _targetWorld = Ctx.OwnerPosition + Vector2.ClampMagnitude(offset, 1f) * _maxRange;
        }
        else
        {
            _elapsed += Time.deltaTime;
            if (!IsComplete && Ctx.Owner != null && Ctx.Owner.Motor != null)
                Ctx.Owner.Motor.Stop();
        }
    }

    public override ReleaseDecision HandleRelease(in BrainIntent intent)
    {
        if (_phase != Phase.Aim) return ReleaseDecision.Continue;
        _phase   = Phase.Cast;
        _elapsed = 0f;
        if (Ctx.Owner != null && Ctx.Owner.Motor != null) Ctx.Owner.Motor.Stop();
        return ReleaseDecision.Continue;
    }

    public override AimResult GetResult() => new AimResult
    {
        TargetPosition = _targetWorld,
        Direction      = (_targetWorld - Ctx.OwnerPosition).normalized,
        HasPosition    = true,
        HasDirection   = true
    };
}
