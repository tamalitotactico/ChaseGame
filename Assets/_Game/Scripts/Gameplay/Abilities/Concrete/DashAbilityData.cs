using UnityEngine;

[CreateAssetMenu(fileName = "DashAbility", menuName = "ChaseGame/Abilities/Dash")]
public class DashAbilityData : AbilityData
{
    [Header("Dash")]
    [Tooltip("Velocidad aplicada como impulso (u/s).")]
    public float force = 18f;

    [Tooltip("Duracion del impulso en segundos.")]
    public float dashDuration = 0.15f;

    public override Ability CreateRuntime() => new DashAbility(this);
}
