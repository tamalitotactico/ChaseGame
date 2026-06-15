using UnityEngine;

[CreateAssetMenu(fileName = "BoosterPillsAbility", menuName = "ChaseGame/Abilities/Booster Pills")]
public class BoosterPillsAbilityData : AbilityData
{
    [Header("Buff al aliado")]
    [Tooltip("Duracion del haste al objetivo.")]
    public float hasteDuration = 1f;

    [Tooltip("Multiplicador de velocidad del haste (>= 1).")]
    public float hasteMultiplier = 1.4f;

    [Tooltip("Porcentaje del cooldown RESTANTE que se recorta a todas las habilidades del objetivo (0..100).")]
    [Range(0f, 100f)]
    public float cdReductionPct = 20f;

    [Tooltip("Alcance para apuntar al aliado.")]
    public float aimRange = 5f;

    public override float IndicatorRange  => aimRange;
    public override float IndicatorRadius => aimRange; // anillo de alcance al aliado (Shape.Ring)

    public override AimStyle Aim => AimStyle.AllyTarget;

    public override Ability CreateRuntime() => new BoosterPillsAbility(this);
}
