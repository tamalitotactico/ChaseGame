using UnityEngine;

[CreateAssetMenu(fileName = "SanacionAbility", menuName = "ChaseGame/Abilities/Sanacion")]
public class SanacionAbilityData : AbilityData
{
    [Header("Cast")]
    [Tooltip("Segundos de canalizacion antes de curar (el Medic se inmoviliza).")]
    public float castTime = 2f;

    [Header("Heal")]
    [Tooltip("Radio del AoE de curacion (centrado en el Medic).")]
    public float aoeRadius = 3f;

    [Tooltip("Cantidad de vida curada (golpes).")]
    public int healAmount = 1;

    public override float IndicatorRadius => aoeRadius;

    public override AimStyle Aim => AimStyle.SelfAoE; // AoE alrededor del lanzador tras el cast

    public override Ability CreateRuntime() => new SanacionAbility(this);
}
