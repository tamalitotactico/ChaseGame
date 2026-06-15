using UnityEngine;

[CreateAssetMenu(fileName = "SmokeTrapAbility", menuName = "ChaseGame/Abilities/Smoke Trap")]
public class SmokeTrapAbilityData : AbilityData
{
    [Header("Trampa")]
    [Tooltip("Prefab de la trampa (SpriteRenderer + SmokeTrapPlaceable).")]
    public GameObject trapPrefab;

    [Tooltip("Radio en el que un hunter dispara la trampa.")]
    public float triggerRadius = 1f;

    [Tooltip("Radio del area de humo (cegado).")]
    public float areaRadius = 2.5f;

    [Tooltip("Segundos que dura el humo tras dispararse.")]
    public float areaDuration = 3f;

    [Header("Cegado")]
    [Tooltip("Fraccion de vision del hunter dentro del humo [0..1]. 0.4 = 40% de vision.")]
    [Range(0f, 1f)]
    public float fovMultiplier = 0.4f;

    [Tooltip("Duracion del BlindedEffect (se refresca cada frame mientras este dentro).")]
    public float blindRefresh = 0.3f;

    [Header("Cupo / deteccion")]
    [Tooltip("Cupo de trampas de humo simultaneas por prey.")]
    public int maxTraps = 2;

    [Tooltip("Mascara de personajes para la deteccion (el filtro real es por team). Default Everything.")]
    public LayerMask characterMask = ~0;

    public override float IndicatorRadius => areaRadius;

    public override AimStyle Aim => AimStyle.SelfAoE;

    public override Ability CreateRuntime() => new SmokeTrapAbility(this);
}
