using UnityEngine;

/// <summary>
/// Efecto de "Charm" (Enchant del Charmer): jale PROGRESIVO hacia un punto fijo del
/// mundo. Es el inverso del miedo: en vez de huir de la fuente, el objetivo es forzado
/// a CAMINAR HACIA un punto (la posicion del Charmer al momento del impacto). La
/// direccion se recalcula cada frame contra ese punto fijo, asi que el jale "sigue" al
/// objetivo aunque este se desvie. No puede ejecutar acciones mientras dura.
///
/// pullStrength escala la magnitud del input forzado (0..1): 1 = jale a velocidad plena,
/// valores menores = arrastre mas suave. Al llegar a arriveRadius del punto, deja de
/// empujar (queda quieto) para evitar jitter.
///
/// Uso: target.StatusEffects.Apply(new CharmedEffect(1f, charmerPos, 1f));
/// </summary>
public class CharmedEffect : StatusEffect
{
    readonly Vector2 _point;
    readonly float   _pullStrength;
    readonly float   _arriveRadius;
    Character _target;

    public override bool BlocksActions  => true;
    public override bool IsControlEffect => true;
    // BlocksMovement = false: queremos que el motor APLIQUE el movimiento forzado.
    public override Color VisualTint     => new Color(1f, 0.45f, 0.75f, 0.6f);
    public override int   VisualPriority => 20;
    public override string IconId        => "charm";

    public override System.Nullable<Vector2> ForceMoveInput
    {
        get
        {
            if (_target == null) return Vector2.zero;
            Vector2 to = _point - (Vector2)_target.transform.position;
            if (to.sqrMagnitude < _arriveRadius * _arriveRadius) return Vector2.zero;
            return to.normalized * _pullStrength;
        }
    }

    /// <param name="duration">Duracion del charm en segundos.</param>
    /// <param name="pullPoint">Punto fijo del mundo hacia el que se arrastra al objetivo.</param>
    /// <param name="pullStrength">Magnitud del jale [0..1]. 1 = velocidad plena.</param>
    /// <param name="arriveRadius">Distancia al punto a la que deja de empujar (anti-jitter).</param>
    public CharmedEffect(float duration, Vector2 pullPoint, float pullStrength = 1f, float arriveRadius = 0.3f)
    {
        Duration      = Remaining = duration;
        _point        = pullPoint;
        _pullStrength = Mathf.Clamp01(pullStrength);
        _arriveRadius = Mathf.Max(0.05f, arriveRadius);
    }

    public override void OnApply(Character target)  { _target = target; }
    public override void OnRemove(Character target) { _target = null; }
}
