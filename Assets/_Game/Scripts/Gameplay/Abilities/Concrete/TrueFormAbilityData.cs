using UnityEngine;

[CreateAssetMenu(fileName = "TrueFormAbility", menuName = "ChaseGame/Abilities/True Form")]
public class TrueFormAbilityData : AbilityData
{
    [Header("True Form")]
    [Tooltip("Multiplicador de velocidad durante la transformacion (>= 1).")]
    public float hasteMultiplier = 1.6f;

    [Tooltip("Tint de overlay durante la transformacion.")]
    public Color tintColor = new Color(0.6f, 0.1f, 0.1f, 0.6f);

    [Tooltip("VFX PlayOnce en la posicion del Werewolf al transformarse (opcional).")]
    public GameObject castFXPrefab;

    // La duracion de la transformacion usa el campo base 'duration' del AbilityData.
    // La letalidad va por el canal GrantsLethalAttack (modo DownInOne); ver TrueFormEffect.

    public override AimStyle Aim => AimStyle.None;

    public override Ability CreateRuntime() => new TrueFormAbility(this);
}
