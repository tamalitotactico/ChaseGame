using UnityEngine;

[CreateAssetMenu(fileName = "AssaultAbility", menuName = "ChaseGame/Abilities/Assault")]
public class AssaultAbilityData : AbilityData
{
    [Header("Cast (Canalizacion)")]
    [Tooltip("Segundos de canalizacion antes del salto. El Charmer se inmoviliza durante este tiempo.")]
    public float castTime = 0.5f;

    [Header("Target / salto")]
    [Tooltip("Radio de busqueda del enemigo a saltar. Si no hay enemigo dentro: la ult se cancela y devuelve la carga.")]
    public float visionRange = 8f;

    [Tooltip("Layer mask de muros: el salto aterriza antes de un muro en el camino.")]
    public LayerMask wallLayer;

    [Tooltip("Margen para detenerse antes del muro al saltar.")]
    public float wallPadding = 0.3f;

    [Header("Efecto al llegar")]
    [Tooltip("Duracion del FearedEffect aplicado al enemigo alcanzado.")]
    public float fearDuration = 1f;

    [Tooltip("Multiplicador de velocidad del slow aplicado junto al fear [0..1]. Dura fearDuration.")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    [Tooltip("Prefab de particulas PlayOnce en el punto de llegada (opcional).")]
    public GameObject arrivalFXPrefab;

    [Header("Audio")]
    [Tooltip("Sonido al EMPEZAR la canalizacion.")]
    public AudioCue sfxOnCastStart;

    [Tooltip("Sonido al LLEGAR al enemigo (se reproduce en el punto de llegada).")]
    public AudioCue sfxOnLanding;

    public override float IndicatorRange  => visionRange;
    public override float IndicatorRadius => visionRange; // anillo de alcance (Shape.Ring)

    public override AimStyle Aim => AimStyle.Direction;
    public override IndicatorShape Shape => IndicatorShape.Ring; // muestra el radio de vision donde puede saltar

    public override Ability CreateRuntime() => new AssaultAbility(this);
}
