using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador de vida "corazon" sobre la cabeza de un Character (mundo). Reemplaza la barra de
/// relleno: un solo corazon cuyo LLENADO es proporcional a la vida (current/max), asi escala a
/// futuros personajes con mas HP sin tocar nada. Con el sistema actual de 2 golpes: 2/2 = lleno
/// (oculto), 1/2 = medio corazon, downed = corazon roto.
///
/// Visibilidad (anti-saturacion): el corazon es visible para TODOS los que ven al prey, pero solo
/// durante una ventana corta (visibleDuration, def. 2s) y luego se oculta para no saturar la pantalla
/// cuando hay varios preys juntos. La ventana se RE-DISPARA cuando:
///   - el prey recibe daño / cae / revive (cambio de estado), o
///   - el prey RE-ENTRA en la vision del jugador local (flanco de subida de SpriteRenderer.enabled,
///     que CharacterFogVisibility activa/desactiva). Asi "vuelve a aparecer en vision -> 2s mas".
/// Fuera de vision el corazon se oculta siempre (no flota en la niebla).
///
/// Self-construido en runtime (sin prefab de UI): robusto a ediciones de escena via MCP. El sprite
/// del corazon se genera proceduralmente como placeholder; asignar heartSprite/brokenSprite cuando
/// llegue el arte (Phase 6) lo reemplaza.
/// </summary>
[DisallowMultipleComponent]
public class HeartIndicator : MonoBehaviour
{
    [Header("Posicion / tamaño (mundo)")]
    [Tooltip("Offset local sobre la cabeza del personaje.")]
    [SerializeField] Vector3 localOffset = new Vector3(0f, 1.0f, 0f);
    [Tooltip("Tamaño del corazon en unidades de mundo.")]
    [SerializeField] float worldSize = 0.55f;
    [Tooltip("Sorting order del canvas (debe ir SOBRE el FogOverlay, sortingOrder 20).")]
    [SerializeField] int sortingOrder = 30;

    [Header("Visibilidad")]
    [Tooltip("Segundos que el corazon permanece visible tras cada disparo (daño o re-entrada a vision).")]
    [SerializeField] float visibleDuration = 2f;

    [Header("Arte (opcional; placeholder generado si se deja vacio)")]
    [SerializeField] Sprite heartSprite;     // forma del corazon (relleno + vacio)
    [SerializeField] Sprite brokenSprite;    // corazon roto (downed); si null, se usa el vacio en gris

    [Header("Colores")]
    [SerializeField] Color fillColor   = new Color(0.92f, 0.18f, 0.22f, 1f);  // rojo vida
    [SerializeField] Color emptyColor  = new Color(0.25f, 0.06f, 0.08f, 0.85f); // hueco oscuro
    [SerializeField] Color brokenColor = new Color(0.35f, 0.35f, 0.38f, 0.9f);  // roto/gris

    enum HeartState { Full, Damaged, Broken }

    Character       _char;
    CharacterHealth _health;
    SpriteRenderer  _visionSprite; // el SR del personaje (lo activa/desactiva CharacterFogVisibility)

    Canvas _canvas;
    Image  _emptyImg;
    Image  _fillImg;

    HeartState _state = HeartState.Full;
    float      _windowRemaining;
    bool       _prevVisible;
    float      _fillRatio = 1f;

    static Sprite _generatedHeart;

    void Awake()
    {
        _char   = GetComponent<Character>();
        _health = GetComponent<CharacterHealth>();
        // El SR del personaje (no el del corazon: el corazon usa Image, no SpriteRenderer).
        _visionSprite = GetComponentInChildren<SpriteRenderer>(true);

        Build();
        SetVisible(false);
    }

    void OnEnable()
    {
        EventBus.Subscribe<CharacterDamagedEvent>(OnDamaged);
        EventBus.Subscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Subscribe<CharacterRevivedEvent>(OnRevived);
        RecomputeFromHealth();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterDamagedEvent>(OnDamaged);
        EventBus.Unsubscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Unsubscribe<CharacterRevivedEvent>(OnRevived);
    }

    // --- Construccion de la UI ---

    void Build()
    {
        var canvasGo = new GameObject("HeartCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        canvasGo.transform.localPosition = localOffset;
        canvasGo.transform.localRotation = Quaternion.identity;
        // Canvas world-space pequeño: sizeDelta en "px" * scale = tamaño mundo.
        float px = 100f;
        canvasGo.transform.localScale = Vector3.one * (worldSize / px);
        var crt = (RectTransform)canvasGo.transform;
        crt.sizeDelta = new Vector2(px, px);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = sortingOrder;

        Sprite hs = heartSprite != null ? heartSprite : GeneratedHeart();

        _emptyImg = MakeImage("Empty", canvasGo.transform, hs, emptyColor);
        _emptyImg.type = Image.Type.Simple;

        _fillImg = MakeImage("Fill", canvasGo.transform, hs, fillColor);
        _fillImg.type = Image.Type.Filled;
        _fillImg.fillMethod = Image.FillMethod.Vertical;
        _fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
        _fillImg.fillAmount = 1f;
    }

    static Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }

    // --- Eventos ---

    void OnDamaged(CharacterDamagedEvent e)
    {
        if (e.Character != _char) return;
        // El golpe mortal publica CharacterDamagedEvent (HP=0) DESPUES de CharacterDownedEvent
        // (OnDied corre dentro de TryDamage). Sin esta guarda, ese evento sobreescribiria el
        // estado Broken con Damaged(fill 0). Downed / HP<=0 => Broken.
        if (e.CurrentHealth <= 0 || (_char != null && _char.IsDowned))
        {
            _state = HeartState.Broken;
            _fillRatio = 0f;
        }
        else
        {
            _fillRatio = e.MaxHealth > 0 ? Mathf.Clamp01((float)e.CurrentHealth / e.MaxHealth) : 0f;
            _state = _fillRatio >= 0.999f ? HeartState.Full : HeartState.Damaged;
        }
        Trigger();
    }

    void OnDowned(CharacterDownedEvent e)
    {
        if (e.Character != _char) return;
        _state = HeartState.Broken;
        _fillRatio = 0f;
        Trigger();
    }

    void OnRevived(CharacterRevivedEvent e)
    {
        if (e.Character != _char) return;
        RecomputeFromHealth();
        Trigger();
    }

    void RecomputeFromHealth()
    {
        if (_char != null && _char.IsDowned) { _state = HeartState.Broken; _fillRatio = 0f; return; }
        if (_health == null) { _state = HeartState.Full; _fillRatio = 1f; return; }
        _fillRatio = _health.MaxHealth > 0 ? Mathf.Clamp01((float)_health.CurrentHealth / _health.MaxHealth) : 0f;
        _state = _fillRatio >= 0.999f ? HeartState.Full : HeartState.Damaged;
    }

    /// <summary>Re-arma la ventana de visibilidad y refresca el aspecto.</summary>
    void Trigger()
    {
        _windowRemaining = visibleDuration;
        Apply();
    }

    // --- Loop ---

    void Update()
    {
        // Vision: el personaje esta visible para el jugador local si su SR esta enabled
        // (CharacterFogVisibility lo gestiona; para el jugador local siempre true).
        bool visibleInFog = _visionSprite == null || _visionSprite.enabled;

        // Flanco de subida (re-entro a vision) -> re-disparar la ventana (si hay algo que mostrar).
        if (visibleInFog && !_prevVisible && _state != HeartState.Full)
            _windowRemaining = visibleDuration;
        _prevVisible = visibleInFog;

        if (_windowRemaining > 0f)
            _windowRemaining -= Time.deltaTime;

        bool show = _state != HeartState.Full && visibleInFog && _windowRemaining > 0f;
        SetVisible(show);
    }

    void Apply()
    {
        if (_fillImg == null || _emptyImg == null) return;
        switch (_state)
        {
            case HeartState.Broken:
                _fillImg.enabled = false;
                _emptyImg.enabled = true;
                _emptyImg.sprite = brokenSprite != null ? brokenSprite : (heartSprite != null ? heartSprite : GeneratedHeart());
                _emptyImg.color = brokenColor;
                break;
            case HeartState.Damaged:
                _emptyImg.enabled = true;
                _emptyImg.sprite = heartSprite != null ? heartSprite : GeneratedHeart();
                _emptyImg.color = emptyColor;
                _fillImg.enabled = true;
                _fillImg.fillAmount = _fillRatio;
                break;
            default: // Full (oculto de todas formas)
                _fillImg.enabled = true;
                _fillImg.fillAmount = 1f;
                _emptyImg.enabled = true;
                _emptyImg.color = emptyColor;
                break;
        }
    }

    void SetVisible(bool v)
    {
        if (_canvas == null) return;
        if (_canvas.enabled != v) _canvas.enabled = v;
        if (v) Apply();
    }

    // --- Placeholder procedural del corazon (cacheado) ---

    static Sprite GeneratedHeart()
    {
        if (_generatedHeart != null) return _generatedHeart;
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[N * N];
        const int SS = 2;            // supersample para anti-alias
        const float range = 1.35f;
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            int inside = 0;
            for (int sy = 0; sy < SS; sy++)
            for (int sx = 0; sx < SS; sx++)
            {
                float u = (x + (sx + 0.5f) / SS) / N;
                float v = (y + (sy + 0.5f) / SS) / N;
                // mundo: x en [-range,range], y arriba positivo
                float fx = (u * 2f - 1f) * range;
                float fy = (v * 2f - 1f) * range;
                if (HeartImplicit(fx, fy) <= 0f) inside++;
            }
            byte a = (byte)(255 * inside / (SS * SS));
            px[y * N + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply();
        _generatedHeart = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        _generatedHeart.name = "GeneratedHeart";
        return _generatedHeart;
    }

    // Curva implicita clasica del corazon: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
    static float HeartImplicit(float x, float y)
    {
        float a = x * x + y * y - 1f;
        return a * a * a - x * x * y * y * y;
    }
}
