using UnityEngine;

/// <summary>
/// Sistema de efectos de camara: screen shake (trauma-based) + zoom dinamico,
/// sumados ENCIMA de la posicion suavizada de CameraFollow.FollowPosition (no de
/// transform.position, para evitar feedback con el lerp de seguimiento).
///
/// Trauma model (estilo GDC "Math for Game Programmers: Juicing Your Cameras"):
/// el shake no es ruido puro proporcional a una intensidad fija, sino que se
/// acumula como "trauma" (0..1), decae solo con el tiempo, y el offset final
/// se escala por trauma^2 (la sacudida se siente "violenta" al inicio y se
/// desvanece rapido, en vez de un temblor lineal constante). El offset usa
/// Perlin noise (no Random puro) para que el movimiento se sienta organico
/// y no como parpadeo aleatorio frame a frame.
///
/// Se ejecuta despues de CameraFollow (DefaultExecutionOrder) y es quien escribe
/// transform.position final cuando esta presente (ver CameraFollow.LateUpdate).
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public class CameraEffectsRig : MonoBehaviour
{
    [Header("Shake (trauma-based)")]
    [Tooltip("Trauma perdido por segundo (1 = se vacia en 1s desde el maximo).")]
    [SerializeField] float traumaDecayPerSecond = 1.4f;
    [Tooltip("Desplazamiento maximo de camara en unidades de mundo, a trauma=1.")]
    [SerializeField] float maxShakeOffset = 0.5f;
    [Tooltip("Rotacion (roll) maxima en grados, a trauma=1. 0 = sin roll.")]
    [SerializeField] float maxShakeRoll = 1.5f;
    [Tooltip("Velocidad de muestreo del ruido Perlin (mas alto = temblor mas rapido).")]
    [SerializeField] float noiseFrequency = 22f;

    [Header("Zoom dinamico")]
    [Tooltip("Velocidad de interpolacion del orthographic size hacia el objetivo.")]
    [SerializeField] float zoomSmoothSpeed = 4f;

    [Header("Miedo (FearedEffect en el jugador local)")]
    [Tooltip("Golpe de trauma inicial al recibir miedo.")]
    [SerializeField] float fearTraumaBurst = 0.5f;
    [Tooltip("Piso de trauma mantenido mientras dura el miedo (sacudida continua de desorientacion).")]
    [SerializeField] float fearTraumaFloor = 0.18f;
    [Tooltip("Factor de zoom mientras el jugador esta con miedo (<1 = acercar, claustrofobico).")]
    [SerializeField] float fearZoomFactor = 0.93f;

    Camera           _cam;
    CameraFollow     _follow;
    float            _trauma;
    float            _baseOrthoSize;
    float            _zoomTarget;
    float            _zoomOverrideUntil; // Time.time; mientras > Time.time, _zoomTarget manda sobre el base
    bool             _feared;            // el jugador local tiene FearedEffect activo
    readonly float[] _noiseSeeds = new float[3];

    void Awake()
    {
        _cam    = GetComponent<Camera>();
        _follow = GetComponent<CameraFollow>();
        for (int i = 0; i < _noiseSeeds.Length; i++)
            _noiseSeeds[i] = Random.Range(0f, 1000f);
    }

    void Start()
    {
        _baseOrthoSize = _cam.orthographicSize;
        _zoomTarget    = _baseOrthoSize;
    }

    void OnEnable()
    {
        EventBus.Subscribe<CharacterDamagedEvent>(OnCharacterDamaged);
        EventBus.Subscribe<CharacterDownedEvent>(OnCharacterDowned);
        EventBus.Subscribe<CharacterRevivedEvent>(OnCharacterRevived);
        EventBus.Subscribe<StatusEffectAppliedEvent>(OnStatusApplied);
        EventBus.Subscribe<StatusEffectRemovedEvent>(OnStatusRemoved);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterDamagedEvent>(OnCharacterDamaged);
        EventBus.Unsubscribe<CharacterDownedEvent>(OnCharacterDowned);
        EventBus.Unsubscribe<CharacterRevivedEvent>(OnCharacterRevived);
        EventBus.Unsubscribe<StatusEffectAppliedEvent>(OnStatusApplied);
        EventBus.Unsubscribe<StatusEffectRemovedEvent>(OnStatusRemoved);
    }

    // ---- API publica (para abilities/VFX que quieran disparar efectos directamente) ----

    /// <summary>Suma trauma (0..1, se clampea). Valores tipicos: 0.2 golpe leve, 0.5 impacto fuerte, 0.8+ derribo.</summary>
    public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

    /// <summary>Atajo expresivo: dispara una sacudida que decae sola. La duracion percibida
    /// depende de traumaDecayPerSecond; 'duration' aqui solo escala cuanto trauma se inyecta.</summary>
    public void Shake(float intensity, float duration = 0.2f) => AddTrauma(Mathf.Clamp01(intensity) * Mathf.Max(0.1f, duration) * traumaDecayPerSecond);

    /// <summary>Lleva el orthographic size a 'size' suavemente. Pasa 0 o negativo para volver al tamaño base.</summary>
    public void ZoomTo(float size, float holdSeconds = -1f)
    {
        _zoomTarget = size > 0f ? size : _baseOrthoSize;
        _zoomOverrideUntil = holdSeconds > 0f ? Time.time + holdSeconds : float.PositiveInfinity;
    }

    /// <summary>Cancela cualquier zoom activo y vuelve gradualmente al tamaño base.</summary>
    public void ResetZoom()
    {
        _zoomTarget = _baseOrthoSize;
        _zoomOverrideUntil = 0f;
    }

    // ---- Eventos de gameplay -> efectos ----

    void OnCharacterDamaged(CharacterDamagedEvent e)
    {
        if (!IsLocalPlayer(e.Character)) return;
        AddTrauma(0.22f);
    }

    void OnCharacterDowned(CharacterDownedEvent e)
    {
        if (!IsLocalPlayer(e.Character)) return;
        AddTrauma(0.65f);
        ZoomTo(_baseOrthoSize * 0.82f); // acercamiento dramatico, se mantiene hasta revivir
    }

    void OnCharacterRevived(CharacterRevivedEvent e)
    {
        if (!IsLocalPlayer(e.Character)) return;
        ResetZoom();
    }

    // Miedo: SOLO afecta la camara si el feared es el prey que controla el jugador local (PlayerBrain).
    // El miedo de otro prey (bot) NO sacude mi camara. Reacciona al EFECTO (no a cada habilidad), asi
    // cubre remanente, teleport-smash y cualquier fuente futura de miedo automaticamente.
    void OnStatusApplied(StatusEffectAppliedEvent e)
    {
        if (!(e.Effect is FearedEffect) || !IsLocalPlayer(e.Character)) return;
        _feared = true;
        AddTrauma(fearTraumaBurst);
        ZoomTo(_baseOrthoSize * fearZoomFactor);
    }

    void OnStatusRemoved(StatusEffectRemovedEvent e)
    {
        if (!(e.Effect is FearedEffect) || !IsLocalPlayer(e.Character)) return;
        _feared = false;
        ResetZoom();
    }

    static bool IsLocalPlayer(Character c) => c != null && c.GetComponent<PlayerBrain>() != null;

    // ---- Loop ----

    void LateUpdate()
    {
        // Decae el trauma con el tiempo (no con el numero de golpes: asi sacudidas
        // sucesivas se acumulan sin pelear contra un timer de "duracion" fijo).
        _trauma = Mathf.Max(0f, _trauma - traumaDecayPerSecond * Time.deltaTime);

        // Mientras dura el miedo, mantener un piso de trauma: sacudida continua sutil que vende
        // la desorientacion durante toda la duracion del efecto (no solo el golpe inicial).
        if (_feared) _trauma = Mathf.Max(_trauma, fearTraumaFloor);

        Vector3 basePos = _follow != null ? _follow.FollowPosition
                                          : new Vector3(transform.position.x, transform.position.y, 0f);
        basePos.z = transform.position.z;

        Vector3 shakeOffset = Vector3.zero;
        float   shakeRoll   = 0f;
        if (_trauma > 0.0001f)
        {
            // trauma^2: arranca fuerte y se desvanece rapido (se siente "violento", no "tembloroso").
            float falloff = _trauma * _trauma;
            float t = Time.time * noiseFrequency;
            float nx = Mathf.PerlinNoise(_noiseSeeds[0], t) * 2f - 1f;
            float ny = Mathf.PerlinNoise(_noiseSeeds[1], t) * 2f - 1f;
            float nr = Mathf.PerlinNoise(_noiseSeeds[2], t) * 2f - 1f;
            shakeOffset = new Vector3(nx, ny, 0f) * (maxShakeOffset * falloff);
            shakeRoll   = nr * (maxShakeRoll * falloff);
        }

        transform.position = basePos + shakeOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, shakeRoll);

        // Zoom: si hay un "hold" con tiempo limite y ya expiro, vuelve sola al tamaño base.
        if (_zoomOverrideUntil != float.PositiveInfinity && Time.time >= _zoomOverrideUntil && _zoomOverrideUntil > 0f)
        {
            _zoomTarget = _baseOrthoSize;
            _zoomOverrideUntil = 0f;
        }
        if (_cam.orthographic)
        {
            float zt = 1f - Mathf.Exp(-zoomSmoothSpeed * Time.deltaTime);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _zoomTarget, zt);
        }
    }
}
