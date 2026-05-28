using UnityEngine;

/// <summary>
/// Configuracion estatica de una habilidad. Cada subclase concreta define sus
/// parametros propios y produce su Ability runtime via CreateRuntime().
/// Las SO no guardan estado runtime; cooldowns, charges, etc viven en la
/// instancia Ability creada por el AbilityController.
///
/// Para agregar un indicador de rango a una ability nueva: ver AimIndicator.cs.
/// </summary>
public abstract class AbilityData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Timings")]
    [Tooltip("Segundos entre activaciones consecutivas.")]
    public float cooldown = 5f;

    [Tooltip("Segundos que dura el efecto activo (0 = instantaneo).")]
    public float duration = 0f;

    [Header("Audio (opcional)")]
    [Tooltip("Sonido al iniciar el aim (feedback inmediato de que la habilidad esta activa).")]
    public AudioCue sfxOnAimStart;

    [Tooltip("Sonido en el momento exacto del cast / ejecucion.")]
    public AudioCue sfxOnCast;

    [Header("Indicator (opcional)")]
    [Tooltip("Prefab visual que se muestra durante el aim (ArrowIndicator, CircleIndicator, etc). " +
             "Si es null, la ability no muestra preview. AbilityIndicatorView lo " +
             "instancia al OnAimingStarted y lo destruye al OnAimingStopped.")]
    public GameObject indicatorPrefab;

    [Header("Indicator geometry (fallback)")]
    [Tooltip("Rango/distancia maxima. Las subclases concretas sobreescriben IndicatorRange " +
             "para apuntar a su propio campo de gameplay (range, teleportDistance, etc).")]
    [SerializeField] protected float indicatorRange = 5f;

    [Tooltip("Radio del AoE. Las subclases sobreescriben IndicatorRadius con su campo real.")]
    [SerializeField] protected float indicatorRadius = 1.5f;

    [Tooltip("Ancho del indicador direccional (flecha) en unidades de mundo. " +
             "Solo aplica a ArrowIndicator / TeleportIndicator.")]
    [SerializeField] protected float indicatorWidth = 1f;

    /// <summary>Rango efectivo leido por los indicadores cada Tick. Subclases lo sobreescriben.</summary>
    public virtual float IndicatorRange  => indicatorRange;
    /// <summary>Radio efectivo leido por los indicadores cada Tick. Subclases lo sobreescriben.</summary>
    public virtual float IndicatorRadius => indicatorRadius;
    /// <summary>Ancho del indicador direccional leido cada Tick. Subclases pueden sobreescribir.</summary>
    public virtual float IndicatorWidth  => indicatorWidth;

    /// <summary>Crea una instancia runtime con su estado propio (cooldown, etc).</summary>
    public abstract Ability CreateRuntime();
}
