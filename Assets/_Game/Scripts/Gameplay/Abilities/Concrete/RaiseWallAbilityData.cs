using UnityEngine;

[CreateAssetMenu(fileName = "RaiseWallAbility", menuName = "ChaseGame/Abilities/Raise Wall")]
public class RaiseWallAbilityData : AbilityData
{
    [Header("Colocacion")]
    [Tooltip("Prefab del muro (SpriteRenderer + RaiseWallPlaceable).")]
    public GameObject wallPrefab;

    [Tooltip("Alcance maximo del punto de colocacion.")]
    [SerializeField] float aimRange = 4f;

    [Tooltip("Largo maximo del muro (en espacio abierto). En pasillos se recorta de pared a pared.")]
    public float maxLength = 5f;

    [Tooltip("Ancho de la zona de slow (grosor de la barra).")]
    public float slowAreaWidth = 0.6f;

    [Header("Slow al hunter")]
    [Tooltip("Multiplicador de velocidad del slow [0..1].")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    [Tooltip("Duracion del slow (se refresca cada frame mientras el hunter este dentro).")]
    public float slowDuration = 0.3f;

    [Header("Cupo / vida")]
    [Tooltip("Cupo de muros simultaneos por prey. Al exceder, se borra el mas antiguo.")]
    public int maxWalls = 2;

    [Tooltip("Vida (s) que se le fija al muro cuando un hunter lo toca por primera vez.")]
    public float hitLifetime = 5f;

    [Header("Layers")]
    [Tooltip("Muros del mapa: para auto-ajustar el largo (raycast).")]
    public LayerMask wallLayer;
    [Tooltip("Layer de los hunters: quienes reciben el slow.")]
    public LayerMask hunterLayer;

    public override float IndicatorRange  => aimRange;
    public override float IndicatorRadius => aimRange; // anillo de alcance de colocacion (Shape.Ring)

    public override AimStyle Aim => AimStyle.Area;

    public override Ability CreateRuntime() => new RaiseWallAbility(this);
}
