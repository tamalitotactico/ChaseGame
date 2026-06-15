using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sistema de ANUNCIOS globales de partida: un banner screen-space que aparece, se mantiene y
/// se desvanece, con audio global opcional. Hoy cubre <see cref="CharacterDownedEvent"/> ("X fue
/// derribado"); es extensible: cualquier sistema puede llamar <see cref="Announce"/>.
///
/// Vive en un GameObject de escena (ej. "AnnouncementSystem" en 00_InGame). Es screen-space
/// OVERLAY, asi que NO lo afecta el post-proceso del Volume (a diferencia de la burbuja de emote,
/// que es world-space). Los anuncios se encolan: si llegan varios, se muestran uno tras otro.
///
/// RUNTIME-UI-REVIEW: el banner (Canvas overlay + CanvasGroup + TMP) se construye en runtime.
/// Convertir a prefab si se quiere arte custom (fondo, slide-in, iconos). Todos los tunables
/// (cue, tiempos, color, tamaño, posicion, sortingOrder) ya estan serializados en este componente.
/// </summary>
public class MatchAnnouncementUI : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Cue global (2D, todos lo oyen) que suena al anunciar un derribo.")]
    [SerializeField] AudioCue downedCue;

    [Header("Texto del evento downed")]
    [Tooltip("{0} = nombre del personaje derribado.")]
    [SerializeField] string downedFormat = "{0} fue derribado";
    [SerializeField] Color  downedColor  = new Color(1f, 0.5f, 0.4f, 1f);

    [Header("Estilo / timing del banner")]
    [SerializeField] float   holdSeconds  = 2f;
    [SerializeField] float   fadeSeconds  = 0.35f;
    [SerializeField] int     fontSize     = 46;
    [Tooltip("Posicion del banner anclado al borde SUPERIOR-centro (x, y hacia abajo negativo).")]
    [SerializeField] Vector2 anchoredPos  = new Vector2(0f, -120f);
    [Tooltip("Orden del Canvas overlay (alto = encima del HUD).")]
    [SerializeField] int     sortingOrder = 100;

    CanvasGroup     _group;
    TextMeshProUGUI _label;

    readonly Queue<Entry> _queue = new();
    struct Entry { public string Msg; public Color Col; }

    enum Phase { Idle, In, Hold, Out }
    Phase _phase = Phase.Idle;
    float _timer;

    void Awake()
    {
        BuildUI();
        SetAlpha(0f);
    }

    void OnEnable()  => EventBus.Subscribe<CharacterDownedEvent>(OnDowned);
    void OnDisable() => EventBus.Unsubscribe<CharacterDownedEvent>(OnDowned);

    void OnDowned(CharacterDownedEvent e)
    {
        if (e.Character == null) return;
        Announce(string.Format(downedFormat, NameOf(e.Character)), downedColor);
        if (downedCue != null)
            ServiceLocator.Resolve<IAudioService>()?.PlayGlobal(downedCue);
    }

    /// <summary>Encola un anuncio. Si no hay ninguno mostrandose, arranca de inmediato.</summary>
    public void Announce(string message, Color color)
    {
        if (string.IsNullOrEmpty(message)) return;
        _queue.Enqueue(new Entry { Msg = message, Col = color });
        if (_phase == Phase.Idle) Next();
    }

    static string NameOf(Character c)
    {
        if (c.Data != null && !string.IsNullOrEmpty(c.Data.displayName)) return c.Data.displayName;
        return c.name;
    }

    void Next()
    {
        if (_queue.Count == 0) { _phase = Phase.Idle; SetAlpha(0f); return; }
        var e = _queue.Dequeue();
        if (_label != null) { _label.text = e.Msg; _label.color = e.Col; }
        _phase = Phase.In;
        _timer = fadeSeconds;
    }

    void Update()
    {
        if (_phase == Phase.Idle) return;
        _timer -= Time.unscaledDeltaTime; // unscaled: visible aunque el juego pause/slow-mo

        switch (_phase)
        {
            case Phase.In:
                SetAlpha(fadeSeconds > 0f ? Mathf.Clamp01(1f - _timer / fadeSeconds) : 1f);
                if (_timer <= 0f) { _phase = Phase.Hold; _timer = holdSeconds; SetAlpha(1f); }
                break;
            case Phase.Hold:
                if (_timer <= 0f) { _phase = Phase.Out; _timer = fadeSeconds; }
                break;
            case Phase.Out:
                SetAlpha(fadeSeconds > 0f ? Mathf.Clamp01(_timer / fadeSeconds) : 0f);
                if (_timer <= 0f) Next();
                break;
        }
    }

    void SetAlpha(float a) { if (_group != null) _group.alpha = a; }

    void BuildUI()
    {
        var canvasGO = new GameObject("AnnouncementCanvas",
            typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        canvasGO.layer = LayerMask.NameToLayer("UI");

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        var bannerGO = new GameObject("Banner", typeof(CanvasGroup), typeof(TextMeshProUGUI));
        bannerGO.transform.SetParent(canvasGO.transform, false);
        bannerGO.layer = canvasGO.layer;

        _group = bannerGO.GetComponent<CanvasGroup>();
        _group.interactable   = false;
        _group.blocksRaycasts = false;

        _label = bannerGO.GetComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Center;
        _label.enableAutoSizing = false;
        _label.fontSize  = fontSize;
        _label.fontStyle = FontStyles.Bold;
        _label.raycastTarget = false;
        _label.text = string.Empty;

        var rt = _label.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(1200f, 100f);
        rt.anchoredPosition = anchoredPos;
    }
}
