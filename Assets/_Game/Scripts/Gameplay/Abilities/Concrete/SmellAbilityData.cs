using UnityEngine;

[CreateAssetMenu(fileName = "SmellAbility", menuName = "ChaseGame/Abilities/Smell")]
public class SmellAbilityData : AbilityData
{
    [Header("Trail")]
    [Tooltip("Prefab del trail (LineRenderer + SmellTrail). Se instancia uno por prey.")]
    public GameObject trailPrefab;

    [Tooltip("Segundos que permanece visible el trail (revelado).")]
    public float revealDuration = 5f;

    [Tooltip("Ancho de la linea del trail.")]
    public float trailWidth = 0.15f;

    [Tooltip("Color del trail.")]
    public Color trailColor = new Color(0.4f, 1f, 0.4f, 0.8f);

    public override AimStyle Aim => AimStyle.None;

    public override Ability CreateRuntime() => new SmellAbility(this);
}
