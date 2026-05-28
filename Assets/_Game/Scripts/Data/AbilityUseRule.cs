using UnityEngine;

/// <summary>
/// Regla declarativa de uso de habilidad para bots. Una lista de reglas vive en
/// BotTuningData.abilityRules; el bot itera y dispara la primera regla cuyas
/// condiciones se cumplen y cuyo cooldown interno este listo.
///
/// Permite definir comportamientos por personaje editando el SO en el inspector,
/// sin tocar codigo. Para un nuevo Hunter con kit distinto basta otro BotTuning
/// con otras reglas.
/// </summary>
[System.Serializable]
public class AbilityUseRule
{
    [Tooltip("Slot de habilidad (0, 1, 2).")]
    [Range(0, 2)]
    public int slot;

    [Tooltip("Distancia minima al target para activar la regla.")]
    public float minDistance = 0f;

    [Tooltip("Distancia maxima al target para activar la regla.")]
    public float maxDistance = 100f;

    [Tooltip("Cooldown interno entre activaciones de ESTA regla. Anti-spam.")]
    public float internalCooldown = 4f;

    [Tooltip("Si true, requiere linea de vista al target.")]
    public bool requiresLineOfSight = true;

    [Tooltip("Condicion adicional sobre el target.")]
    public TargetCondition condition = TargetCondition.None;

    [TextArea(2, 4)]
    [Tooltip("Documentacion legible — proposito de la regla.")]
    public string note;
}

/// <summary>
/// Condicion sobre el target evaluada por el bot al decidir si activar una habilidad.
/// </summary>
public enum TargetCondition
{
    /// <summary>Sin condicion adicional.</summary>
    None,

    /// <summary>El target se esta moviendo (|velocity| > umbral).</summary>
    TargetMoving,

    /// <summary>El target tiene menos del 100% de HP.</summary>
    TargetWounded,

    /// <summary>No hay aliados del target dentro de un radio (definido en Tuning).</summary>
    TargetIsolated,

    /// <summary>El target esta huyendo en linea recta opuesta al bot.</summary>
    TargetFleeingStraight
}
