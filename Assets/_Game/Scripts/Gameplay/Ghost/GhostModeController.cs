using UnityEngine;

/// <summary>
/// Orquesta el modo fantasma del JUGADOR LOCAL. Sistema paralelo: no toca DownedState ni
/// RevivableComponent. Vive en la Main Camera (junto a CameraFollow / CameraEffectsRig).
///
/// Al caer downed el jugador local: construye un fantasma en la pos del cuerpo, le da el control
/// (lee el HybridInputManager), reapunta la camara y la niebla al fantasma. El cuerpo queda como
/// ancla revivible. Al revivir (o terminar la partida) destruye el fantasma y devuelve camara y
/// vision al cuerpo.
///
/// SEAM red (consideracion MP: los fantasmas se ven entre si): SpawnGhost es el unico punto de
/// creacion; en multiplayer se ruteara por ISpawnService/Runner.Spawn y la visibilidad ghost-ve-ghost
/// se agregara con un componente de filtro en el fantasma. Local-only por ahora: 1 solo fantasma
/// (el tuyo), siempre visible para vos (no lleva CharacterFogVisibility, no pasa por la niebla).
/// </summary>
[DisallowMultipleComponent]
public class GhostModeController : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Factor sobre la velocidad base del prey. >1 = el fantasma es mas rapido.")]
    [SerializeField] float ghostSpeedBonus = 1.15f;

    [Header("Aspecto")]
    [Tooltip("Tinte translucido del fantasma (placeholder).")]
    [SerializeField] Color ghostTint = new Color(0.6f, 0.85f, 1f, 0.5f);
    [Tooltip("SEAM arte futuro: si se asigna, el fantasma usa este sprite en vez de copiar el del cuerpo.")]
    [SerializeField] Sprite ghostSpriteOverride;
    [Tooltip("Sorting order del sprite del fantasma. Debe ir SOBRE el FogOverlay (20).")]
    [SerializeField] int ghostSortingOrder = 25;

    CameraFollow       _camera;
    HybridInputManager _input;

    Character    _bodyCharacter; // cuerpo downed del jugador local
    VisionSource _bodyVision;
    GameObject   _ghost;

    void Awake()
    {
        _camera = GetComponent<CameraFollow>();
        if (_camera == null) _camera = Object.FindAnyObjectByType<CameraFollow>();
    }

    void OnEnable()
    {
        EventBus.Subscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Subscribe<CharacterRevivedEvent>(OnRevived);
        EventBus.Subscribe<MatchEndedEvent>(OnMatchEnded);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Unsubscribe<CharacterRevivedEvent>(OnRevived);
        EventBus.Unsubscribe<MatchEndedEvent>(OnMatchEnded);
        // Si quedamos deshabilitados con fantasma activo (cambio de escena/reset), limpiar.
        if (_ghost != null) ExitGhost();
    }

    static bool IsLocalPlayer(Character c) => c != null && c.GetComponent<PlayerBrain>() != null;

    void OnDowned(CharacterDownedEvent e)
    {
        if (!IsLocalPlayer(e.Character)) return;
        if (_ghost != null) return; // ya hay fantasma activo
        SpawnGhost(e.Character);
    }

    void OnRevived(CharacterRevivedEvent e)
    {
        if (_bodyCharacter == null || e.Character != _bodyCharacter) return;
        ExitGhost();
    }

    void OnMatchEnded(MatchEndedEvent e)
    {
        if (_ghost != null) ExitGhost();
    }

    /// <summary>Punto UNICO de creacion del fantasma (SEAM red: futuro ruteo a ISpawnService/Runner.Spawn).</summary>
    void SpawnGhost(Character body)
    {
        _bodyCharacter = body;
        _bodyVision = body.GetComponent<VisionSource>();
        if (_input == null) _input = Object.FindAnyObjectByType<HybridInputManager>();

        float baseSpeed = body.Motor != null ? body.Motor.MaxSpeed : 4f;
        float speed = baseSpeed * Mathf.Max(0.1f, ghostSpeedBonus);

        _ghost = new GameObject("PlayerGhost");
        _ghost.transform.position = body.transform.position;

        // Sprite del fantasma: arte dedicado si se asigno; si no, copia translucida del sprite del cuerpo.
        var sr = _ghost.AddComponent<SpriteRenderer>();
        var bodySr = body.GetComponentInChildren<SpriteRenderer>(true);
        sr.sprite = ghostSpriteOverride != null ? ghostSpriteOverride : (bodySr != null ? bodySr.sprite : null);
        if (bodySr != null)
        {
            sr.flipX = bodySr.flipX;
            sr.sortingLayerID = bodySr.sortingLayerID;
            _ghost.transform.localScale = bodySr.transform.lossyScale; // igualar tamaño rendizado
        }
        sr.color = ghostTint;
        sr.sortingOrder = ghostSortingOrder; // > FogOverlay (20): el fantasma nunca se oscurece

        // Vision propia con el radio NORMAL del prey: la vision completa viaja con el fantasma.
        var vs = _ghost.AddComponent<VisionSource>();
        vs.visionRadius = _bodyVision != null ? _bodyVision.VisionRadius : 5f;

        // Control local + puntero al cuerpo.
        var gc = _ghost.AddComponent<GhostController>();
        gc.Configure(speed, body, _input);

        var ptr = _ghost.AddComponent<GhostBodyPointer>();
        ptr.Configure(body);

        // Reapuntar camara y niebla al fantasma.
        if (_camera != null) _camera.SetTarget(_ghost.transform);
        if (FogOfWarManager.Instance != null) FogOfWarManager.Instance.SetPrimarySource(vs);
    }

    void ExitGhost()
    {
        // Devolver camara y niebla al cuerpo (si sigue existiendo).
        if (_bodyCharacter != null)
        {
            if (_camera != null) _camera.SetTarget(_bodyCharacter.transform);
            if (FogOfWarManager.Instance != null && _bodyVision != null)
                FogOfWarManager.Instance.SetPrimarySource(_bodyVision);
        }
        if (_ghost != null) { Destroy(_ghost); _ghost = null; }
        _bodyCharacter = null;
        _bodyVision = null;
    }
}
