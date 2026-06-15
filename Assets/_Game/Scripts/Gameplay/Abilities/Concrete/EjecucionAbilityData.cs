using UnityEngine;

[CreateAssetMenu(fileName = "EjecucionAbility", menuName = "ChaseGame/Abilities/Ejecucion")]
public class EjecucionAbilityData : AbilityData
{
    [Header("Cast / apuntado")]
    [Tooltip("Segundos de canalizacion antes del dash. El Drowned se inmoviliza durante este tiempo.")]
    public float castTime = 0.6f;

    [Tooltip("Alcance maximo del punto apuntado (radio de teleport).")]
    [SerializeField] float aimRange = 6f;

    [Tooltip("Layer mask de muros: el dash aterriza antes de un muro en el camino.")]
    public LayerMask wallLayer;
    [Tooltip("Margen para detenerse antes del muro.")]
    public float wallPadding = 0.3f;

    [Header("AoE en el destino")]
    [Tooltip("Radio del area de dano en el punto de llegada.")]
    public float aoeRadius = 3.5f;

    [Tooltip("Dano (golpes) a cada enemigo en el area.")]
    public int damage = 1;

    [Tooltip("Layers que reciben el dano (capa de personajes).")]
    public LayerMask targetLayers;

    [Tooltip("Prefab de particulas PlayOnce en el punto de llegada (opcional).")]
    public GameObject aoeFXPrefab;

    [Header("Haste si derriba")]
    [Tooltip("Duracion del haste al Drowned si el golpe derribo a alguien. 0 = sin haste.")]
    public float downHasteDuration = 1f;
    [Tooltip("Multiplicador de velocidad del haste (>= 1).")]
    public float downHasteMultiplier = 1.5f;

    [Header("Audio")]
    public AudioCue sfxOnLanding;

    public override float IndicatorRange  => aimRange;
    public override float IndicatorRadius => aoeRadius;

    public override AimStyle Aim => AimStyle.Area;
    public override IndicatorShape Shape => IndicatorShape.ArrowAoE; // dash a la posicion + AoE de aterrizaje

    public override Ability CreateRuntime() => new EjecucionAbility(this);
}
