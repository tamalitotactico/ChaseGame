using UnityEngine;

[CreateAssetMenu(fileName = "RepelAbility", menuName = "ChaseGame/Abilities/Repel")]
public class RepelAbilityData : AbilityData
{
    [Header("Dispel")]
    [Tooltip("Si true, remueve TODOS los efectos negativos de control activos sobre el owner " +
             "(fear/slow/stun/charm). Deja los buffs intactos. Granularidad por tipo: futura.")]
    public bool dispelControlEffects = true;

    [Header("CC Immunity")]
    [Tooltip("Segundos de inmunidad a nuevos efectos de control tras castear. 0 = sin inmunidad.")]
    public float immunityDuration = 2f;

    [Header("Self haste")]
    [Tooltip("Segundos de haste al owner. 0 = sin haste.")]
    public float hasteDuration = 2f;

    [Tooltip("Multiplicador de velocidad del haste (>= 1).")]
    public float hasteMultiplier = 1.4f;

    public override AimStyle Aim => AimStyle.None;

    public override Ability CreateRuntime() => new RepelAbility(this);
}
