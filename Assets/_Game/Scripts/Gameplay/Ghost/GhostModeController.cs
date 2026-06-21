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
    [Header("Spawn")]
    [Tooltip("Prefab del fantasma (SpriteRenderer + GhostController + GhostBodyPointer). Si es null, se " +
             "construye en runtime. Asignarlo rutea el spawn por ISpawnService (prep Fusion Runner.Spawn).")]
    [SerializeField] GameObject ghostPrefab;

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

    [Header("Puntero al cuerpo (editable aqui porque el fantasma se construye en runtime)")]
    [Tooltip("Sprite del puntero/flecha al cuerpo. Vacio = triangulo generado por defecto.")]
    [SerializeField] Sprite pointerSprite;
    [Tooltip("Tamaño del puntero en px.")]
    [SerializeField] float pointerSize = 44f;
    [Tooltip("Color del puntero en estado normal.")]
    [SerializeField] Color pointerIdleColor = new Color(0.7f, 0.85f, 1f, 0.9f);
    [Tooltip("Color del puntero cuando un aliado te esta reviviendo.")]
    [SerializeField] Color pointerRevivingColor = new Color(0.3f, 1f, 0.45f, 1f);

    CameraFollow       _camera;
    HybridInputManager _input;

    Character    _bodyCharacter; // cuerpo downed del jugador local
    GameObject   _ghost;

    /// <summary>True si el jugador local esta controlando el fantasma (downed). Lo usan el emisor de
    /// emotes y el presenter de burbujas para resolver la posicion de render (matriz de visibilidad).</summary>
    public bool IsGhostActive => _ghost != null;
    /// <summary>Transform del fantasma activo, o null si no hay fantasma.</summary>
    public Transform GhostTransform => _ghost != null ? _ghost.transform : null;

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
        if (_input == null) _input = Object.FindAnyObjectByType<HybridInputManager>();

        float baseSpeed = body.Motor != null ? body.Motor.MaxSpeed : 4f;
        float speed = baseSpeed * Mathf.Max(0.1f, ghostSpeedBonus);

        Vector3 pos = body.transform.position;
        // El fantasma es una herramienta de vista LOCAL del jugador downed (no lo ven los demas en
        // este hito). Se instancia SIEMPRE local: NO via ISpawnService, porque en el host esa ruta es
        // FusionSpawnService -> Runner.Spawn, que falla con un prefab sin NetworkObject (PlayerGhost no
        // lo tiene). "Ghost-ve-ghost" en red sera un hito posterior (ghost = NetworkObject + filtro).
        if (ghostPrefab != null)
        {
            _ghost = Instantiate(ghostPrefab, pos, Quaternion.identity);
            _ghost.transform.position = pos;
        }
        else
        {
            _ghost = new GameObject("PlayerGhost");
            _ghost.transform.position = pos;
        }

        // Get-or-add: funciona igual con prefab (trae los componentes) o construido en runtime.
        // Sprite del fantasma: arte dedicado si se asigno; si no, copia translucida del sprite del cuerpo.
        var sr = _ghost.GetComponent<SpriteRenderer>() ?? _ghost.AddComponent<SpriteRenderer>();
        var bodySr = body.GetComponentInChildren<SpriteRenderer>(true);
        sr.sprite = ghostSpriteOverride != null ? ghostSpriteOverride : (bodySr != null ? bodySr.sprite : sr.sprite);
        if (bodySr != null)
        {
            sr.flipX = bodySr.flipX;
            sr.sortingLayerID = bodySr.sortingLayerID;
            _ghost.transform.localScale = bodySr.transform.lossyScale; // igualar tamaño rendizado
        }
        sr.color = ghostTint;
        sr.sortingOrder = ghostSortingOrder; // > FogOverlay (20): el fantasma nunca se oscurece

        // Control local + puntero al cuerpo (config editable desde este componente).
        var gc = _ghost.GetComponent<GhostController>() ?? _ghost.AddComponent<GhostController>();
        gc.Configure(speed, body, _input);

        var ptr = _ghost.GetComponent<GhostBodyPointer>() ?? _ghost.AddComponent<GhostBodyPointer>();
        ptr.Configure(body, pointerSprite, pointerSize, pointerIdleColor, pointerRevivingColor);

        // Camara sigue al fantasma y la niebla se APAGA: el jugador downed ve TODO el mapa.
        if (_camera != null) _camera.SetTarget(_ghost.transform);
        if (FogOfWarManager.Instance != null) FogOfWarManager.Instance.SetRevealAll(true);
    }

    void ExitGhost()
    {
        // Restaurar niebla y devolver la camara al cuerpo (si sigue existiendo).
        if (FogOfWarManager.Instance != null) FogOfWarManager.Instance.SetRevealAll(false);
        if (_bodyCharacter != null && _camera != null) _camera.SetTarget(_bodyCharacter.transform);
        if (_ghost != null)
        {
            Destroy(_ghost); // fantasma local (ver SpawnGhost): destruir local, no via ISpawnService.
            _ghost = null;
        }
        _bodyCharacter = null;
    }
}
