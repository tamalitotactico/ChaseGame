using UnityEngine;

[CreateAssetMenu(fileName = "NadarAbility", menuName = "ChaseGame/Abilities/Nadar")]
public class NadarAbilityData : AbilityData
{
    [Header("Nadar (camuflaje + haste)")]
    [Tooltip("Multiplicador de velocidad mientras dura el camuflaje (>= 1).")]
    public float hasteMultiplier = 1.4f;

    [Tooltip("Distancia a la que un enemigo empieza a verlo (fade dentro del radio). Menor = mas sigiloso.")]
    public float revealRadius = 4f;

    // La duracion usa el campo base 'duration' del AbilityData.

    public override AimStyle Aim => AimStyle.None;

    public override Ability CreateRuntime() => new NadarAbility(this);
}
