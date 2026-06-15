using UnityEngine;

/// <summary>
/// Como se apunta la habilidad. FUENTE UNICA DE VERDAD: define el modo de input de la UI
/// (RequiresAim) y la forma por defecto del indicador. Debe ser coherente con el Aimer que
/// devuelve el Ability.BeginActivation correspondiente.
///   None       = instant tap, sin apuntar y sin footprint visible (buffs propios).
///   Direction  = drag direccional (proyectiles, dash apuntado).
///   Area       = drag a una posicion dentro de un alcance (colocar en el suelo).
///   AllyTarget = drag para elegir un aliado dentro de un alcance.
///   SelfAoE    = tap, pero con un radio propio/colocado que SI se dibuja (auras, trampas).
/// </summary>
public enum AimStyle { None, Direction, Area, AllyTarget, SelfAoE }

/// <summary>
/// Forma visual del indicador. Auto = derivar de AimStyle (None->None, Direction->Arrow,
/// Area->Ring, AllyTarget->Ring, SelfAoE->AoE). Sobreescribir solo en casos especiales
/// (TeleportSmash/Ejecucion->ArrowAoE; Assault->Ring).
/// </summary>
public enum IndicatorShape { Auto, None, Arrow, Cone, Ring, AoE, ArrowAoE }

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

    [Header("Carga por hits (ultimates de hunter)")]
    [Tooltip("Si true, la habilidad NO usa cooldown: se habilita al acumular 'hitsRequired' golpes " +
             "BASICOS a preys. El contador se resetea al ejecutarla. Usado por las ult (slot R) de hunter.")]
    public bool usesHitCharge = false;
    [Tooltip("Golpes basicos necesarios para habilitar la ult (si usesHitCharge). El campo 'cooldown' se ignora.")]
    public int  hitsRequired = 2;

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

    /// <summary>
    /// Radio del proyectil en unidades de mundo: FUENTE UNICA del tamano. ProjectileSetup lo aplica
    /// al collider de impacto, al wall-sensor (mitad) y al sprite; el indicador direccional usa su
    /// diametro como ancho. &lt;=0 = la habilidad no dispara proyectil (usa IndicatorWidth de fallback).
    /// </summary>
    public virtual float ProjectileRadius => 0f;

    /// <summary>Medio-angulo del cono en GRADOS (solo Shape.Cone). El indicador lo convierte a radianes.</summary>
    public virtual float IndicatorConeHalfAngle => 30f;

    [Header("Aim / Indicator")]
    [Tooltip("Color emisivo del indicador. Alpha 0 = usar el color por defecto del bando " +
             "(resuelto por el render). HDR para que el bloom lo haga brillar.")]
    [ColorUsage(true, true)] public Color indicatorColor = new Color(0f, 0f, 0f, 0f);

    /// <summary>
    /// Como se apunta esta habilidad. Cada subclase concreta lo declara. Es la fuente unica de
    /// verdad para la UI (RequiresAim) y para la forma por defecto del indicador.
    /// </summary>
    public abstract AimStyle Aim { get; }

    /// <summary>Forma visual del indicador. Por defecto Auto (derivada de Aim). Override solo si difiere.</summary>
    public virtual IndicatorShape Shape => IndicatorShape.Auto;

    /// <summary>True si la UI debe permitir drag-aim. Solo Direction/Area/AllyTarget arrastran.</summary>
    public bool RequiresAim =>
        Aim == AimStyle.Direction || Aim == AimStyle.Area || Aim == AimStyle.AllyTarget;

    /// <summary>Forma del indicador ya resuelta (Auto -> forma concreta segun Aim).</summary>
    public IndicatorShape ResolvedShape
    {
        get
        {
            if (Shape != IndicatorShape.Auto) return Shape;
            switch (Aim)
            {
                case AimStyle.Direction:  return IndicatorShape.Arrow;
                case AimStyle.Area:       return IndicatorShape.Ring;
                case AimStyle.AllyTarget: return IndicatorShape.Ring;
                case AimStyle.SelfAoE:    return IndicatorShape.AoE;
                default:                  return IndicatorShape.None;
            }
        }
    }

    /// <summary>Crea una instancia runtime con su estado propio (cooldown, etc).</summary>
    public abstract Ability CreateRuntime();
}
