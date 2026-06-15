using UnityEngine;

/// <summary>
/// Efecto de miedo: el objetivo huye en linea recta en la direccion opuesta a la fuente
/// (fijada al momento de aplicar el efecto) y no puede ejecutar ninguna accion.
///
/// La direccion de huida se calcula como (preyPos - sourcePos).normalized en el sitio
/// que crea el efecto (decoy, proyectil al impactar, AoE de ulti) y se mantiene constante
/// hasta que expira.
///
/// Uso: character.StatusEffects.Apply(new FearedEffect(2f, fleeDir));
/// </summary>
public class FearedEffect : StatusEffect
{
    readonly Vector2 _fleeDirection;

    public override bool BlocksActions => true;
    public override bool IsControlEffect => true;
    // BlocksMovement = false porque queremos que el motor APLIQUE el movimiento forzado.
    public override Vector2? ForceMoveInput => _fleeDirection;
    public override Color VisualTint     => new Color(0.65f, 0.2f, 0.85f, 0.6f);
    public override int   VisualPriority => 20;
    public override string IconId        => "fear";

    /// <param name="duration">Duracion del miedo en segundos.</param>
    /// <param name="fleeDirection">Direccion en que huira el objetivo. Se normaliza automaticamente; si es cero se usa Vector2.right como fallback.</param>
    public FearedEffect(float duration, Vector2 fleeDirection)
    {
        Duration  = Remaining = duration;
        _fleeDirection = fleeDirection.sqrMagnitude > 0.0001f
            ? fleeDirection.normalized
            : Vector2.right;
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
