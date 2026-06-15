using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puntero al cuerpo del jugador mientras controla el fantasma. Vive en el GO del fantasma y
/// auto-construye un Canvas Screen-Space Overlay (sin prefab, patron de HeartIndicator).
///
/// Dos modos:
///   - Cuerpo EN pantalla: baliza (flecha apuntando hacia abajo) sobre el cuerpo.
///   - Cuerpo FUERA de pantalla: flecha clampeada al borde, rotada hacia el cuerpo.
///
/// Cuando un aliado esta reviviendo el cuerpo (ReviveProgressChangedEvent.HasReviver del cuerpo),
/// cambia de color y pulsa para avisar al jugador que vuelva.
/// </summary>
// RUNTIME-UI-REVIEW: construido en runtime (sin prefab). Sprite/tamaño/colores ya expuestos via
// GhostModeController; pendiente convertir el fantasma entero a prefab (lo exigira Fusion Runner.Spawn).
[DisallowMultipleComponent]
public class GhostBodyPointer : MonoBehaviour
{
    [Header("Aspecto")]
    [Tooltip("Tamaño de la flecha/baliza en px (lo setea GhostModeController.pointerSize).")]
    [SerializeField] float markerSize = 44f;
    [Tooltip("Margen desde el borde de pantalla al clampear, en px.")]
    [SerializeField] float edgeMargin = 48f;
    [Tooltip("Offset vertical de la baliza sobre el cuerpo cuando esta en pantalla, en px.")]
    [SerializeField] float onScreenYOffset = 56f;
    [Tooltip("Sorting order del canvas (overlay, sobre el HUD/niebla).")]
    [SerializeField] int sortingOrder = 200;

    [Header("Colores")]
    [SerializeField] Color idleColor    = new Color(0.7f, 0.85f, 1f, 0.9f);   // celeste tenue
    [SerializeField] Color revivingColor = new Color(0.3f, 1f, 0.45f, 1f);    // verde "te estan reviviendo"

    Transform _body;
    Character _bodyCharacter;

    Canvas _canvas;
    RectTransform _markerRt;
    Image _markerImg;

    bool  _hasReviver;
    float _progress;

    static Sprite _generatedArrow;
    Sprite _overrideSprite;

    /// <summary>Configura y construye el puntero. Lo llama GhostModeController, que expone estos
    /// valores en el inspector (sprite/tamaño/colores) porque el fantasma se crea en runtime.
    /// sprite null => usa el triangulo generado por defecto. size &lt;= 0 => conserva el default.</summary>
    public void Configure(Character body, Sprite sprite, float size, Color idle, Color reviving)
    {
        _bodyCharacter = body;
        _body = body != null ? body.transform : null;
        _overrideSprite = sprite;
        if (size > 0f) markerSize = size;
        idleColor = idle;
        revivingColor = reviving;
        Build();
    }

    void OnEnable()
    {
        EventBus.Subscribe<ReviveProgressChangedEvent>(OnReviveProgress);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<ReviveProgressChangedEvent>(OnReviveProgress);
    }

    void OnReviveProgress(ReviveProgressChangedEvent e)
    {
        if (_bodyCharacter == null || e.Character != _bodyCharacter) return;
        _hasReviver = e.HasReviver;
        _progress   = e.Progress;
    }

    void Build()
    {
        var canvasGo = new GameObject("GhostBodyPointerCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;

        var markerGo = new GameObject("Marker", typeof(RectTransform));
        markerGo.transform.SetParent(canvasGo.transform, false);
        _markerRt = (RectTransform)markerGo.transform;
        _markerRt.anchorMin = Vector2.zero;
        _markerRt.anchorMax = Vector2.zero;
        _markerRt.pivot = new Vector2(0.5f, 0.5f);
        _markerRt.sizeDelta = new Vector2(markerSize, markerSize);

        _markerImg = markerGo.AddComponent<Image>();
        _markerImg.sprite = _overrideSprite != null ? _overrideSprite : GeneratedArrow();
        _markerImg.color = idleColor;
        _markerImg.raycastTarget = false;
        _markerImg.preserveAspect = true;
    }

    void LateUpdate()
    {
        if (_body == null || _canvas == null) { SetVisible(false); return; }

        var cam = Camera.main;
        if (cam == null) { SetVisible(false); return; }

        SetVisible(true);

        Vector3 sp = cam.WorldToScreenPoint(_body.position);
        bool behind = sp.z < 0f;
        if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }

        bool onScreen = !behind
            && sp.x >= edgeMargin && sp.x <= Screen.width - edgeMargin
            && sp.y >= edgeMargin && sp.y <= Screen.height - edgeMargin;

        Vector2 pos;
        float angle; // la flecha base apunta hacia +Y

        if (onScreen)
        {
            // Baliza sobre el cuerpo, apuntando hacia abajo hacia el.
            pos = new Vector2(sp.x, sp.y + onScreenYOffset);
            angle = 180f;
        }
        else
        {
            // Clampear a un rectangulo (pantalla - margen) en la direccion desde el centro.
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = (Vector2)sp - center;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();

            float halfW = Screen.width * 0.5f - edgeMargin;
            float halfH = Screen.height * 0.5f - edgeMargin;
            float sx = Mathf.Abs(dir.x) < 1e-4f ? float.MaxValue : halfW / Mathf.Abs(dir.x);
            float sy = Mathf.Abs(dir.y) < 1e-4f ? float.MaxValue : halfH / Mathf.Abs(dir.y);
            float scale = Mathf.Min(sx, sy);
            pos = center + dir * scale;
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // rotar +Y hacia dir
        }

        _markerRt.anchoredPosition = pos;
        _markerRt.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Estado de revive: color + pulso.
        if (_hasReviver)
        {
            float pulse = 1f + 0.18f * Mathf.Sin(Time.time * 9f);
            _markerRt.localScale = Vector3.one * pulse;
            _markerImg.color = revivingColor;
        }
        else
        {
            _markerRt.localScale = Vector3.one;
            _markerImg.color = idleColor;
        }
    }

    void SetVisible(bool v)
    {
        if (_canvas != null && _canvas.enabled != v) _canvas.enabled = v;
    }

    // --- Placeholder procedural: triangulo solido apuntando hacia +Y (cacheado) ---
    static Sprite GeneratedArrow()
    {
        if (_generatedArrow != null) return _generatedArrow;
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var px = new Color32[N * N];
        const int SS = 2;
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            int inside = 0;
            for (int sy = 0; sy < SS; sy++)
            for (int sx = 0; sx < SS; sx++)
            {
                float u = (x + (sx + 0.5f) / SS) / N; // 0..1
                float v = (y + (sy + 0.5f) / SS) / N; // 0..1, apex arriba
                // Triangulo: en altura v el medio-ancho permitido es (1-v)/2 (apex en v=1).
                float halfW = (1f - v) * 0.5f;
                if (Mathf.Abs(u - 0.5f) <= halfW) inside++;
            }
            byte a = (byte)(255 * inside / (SS * SS));
            px[y * N + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply();
        _generatedArrow = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        _generatedArrow.name = "GeneratedArrow";
        return _generatedArrow;
    }
}
