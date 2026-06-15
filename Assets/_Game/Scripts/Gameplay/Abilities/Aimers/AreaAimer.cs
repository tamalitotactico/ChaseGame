using UnityEngine;

/// <summary>
/// Aimer para abilities de area (trampas, AoE colocadas en suelo).
/// Lee AimInput como offset desde el owner; clampea a maxRange.
/// </summary>
public sealed class AreaAimer : Aimer
{
    readonly float _maxRange;
    Vector2 _targetWorld;

    public override Vector2 CurrentDirection
    {
        get
        {
            Vector2 d = _targetWorld - Ctx.OwnerPosition;
            return d.sqrMagnitude > 0.0001f ? d.normalized : base.CurrentDirection;
        }
    }
    public override Vector2? CurrentTarget => _targetWorld;

    public AreaAimer(float maxRange)
    {
        _maxRange = maxRange;
    }

    protected override void OnBegin()
    {
        _targetWorld = Ctx.OwnerPosition;
    }

    public override void Tick(in BrainIntent intent)
    {
        Vector2 offset = intent.AimInput.sqrMagnitude > 0.01f
            ? intent.AimInput
            : intent.MoveInput;

        if (offset.sqrMagnitude > 0.01f)
        {
            Vector2 clamped = Vector2.ClampMagnitude(offset, 1f) * _maxRange;
            _targetWorld = Ctx.OwnerPosition + clamped;
        }
    }

    public override AimResult GetResult() => new AimResult
    {
        TargetPosition = _targetWorld,
        Direction      = (_targetWorld - Ctx.OwnerPosition).normalized,
        HasPosition    = true,
        HasDirection   = true
    };
}
