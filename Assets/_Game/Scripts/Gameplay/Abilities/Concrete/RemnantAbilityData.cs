using UnityEngine;

[CreateAssetMenu(fileName = "RemnantAbility", menuName = "ChaseGame/Abilities/Remnant")]
public class RemnantAbilityData : AbilityData
{
    [Header("Decoy")]
    public GameObject decoyPrefab;

    [Tooltip("Tiempo máximo si nunca se activa")]
    public float decoyDuration = 5f;

    [Header("Activation")]
    [Tooltip("Radio que activa la trampa")]
    public float activationRadius = 1.5f;

    [Header("Effect")]
    [Tooltip("Radio donde se aplican efectos")]
    public float effectRadius = 4f;

    [Tooltip("Tiempo miedo")]
    public float fearDuration = 1.5f;

    [Tooltip("Tiempo slow")]
    public float slowDuration = 2f;

    [Range(0f, 1f)]
    public float slowMultiplier = .5f;

    [Tooltip("Tiempo que permanece activa luego de dispararse")]
    public float activeDuration = 1.5f;

    public LayerMask preyLayer;

    public override float IndicatorRadius => effectRadius;

    public override Ability CreateRuntime() => new RemnantAbility(this);
}
