using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Rueda radial de emotes in-game (Brawl Stars style): mantener el boton abre la rueda con los 3
/// emotes EQUIPADOS (IProfileService.GetEmoteId 0..2); arrastrar selecciona; soltar dispara. Soltar
/// al centro / dentro del deadzone cancela. Cooldown corto global anti-spam.
///
/// Autorizado en ESCENA (este componente va en el boton; wheelRoot/slotRects/slotIcons se asignan en
/// el inspector) -> tunable sin tocar codigo. Solo los iconos se rellenan en runtime desde el loadout.
///
/// El render del emote lo hace EmoteBubblePresenter al recibir EmoteUsedEvent.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EmoteWheelHUD : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Rueda")]
    [Tooltip("Contenedor de los 3 slots; se muestra mientras se mantiene el boton.")]
    [SerializeField] GameObject wheelRoot;
    [Tooltip("RectTransforms de los 3 slots (su direccion desde el boton define la seleccion).")]
    [SerializeField] RectTransform[] slotRects = new RectTransform[3];
    [Tooltip("Iconos de los 3 slots; se rellenan con los emotes equipados.")]
    [SerializeField] Image[] slotIcons = new Image[3];

    [Header("Tuning")]
    [Tooltip("Segundos entre emotes (anti-spam global).")]
    [SerializeField] float cooldownSeconds = 2.5f;
    [Tooltip("Distancia minima de arrastre (px) para considerar que se eligio un slot.")]
    [SerializeField] float selectDeadzonePx = 40f;
    [Tooltip("Escala del slot resaltado durante el arrastre.")]
    [SerializeField] float selectedScale = 1.25f;

    RectTransform _self;
    Camera        _eventCam;
    GhostModeController _ghostCtrl;
    int   _selected = -1;
    float _nextAllowed;

    void Awake()
    {
        _self = (RectTransform)transform;
    }

    void OnEnable()
    {
        if (wheelRoot != null) wheelRoot.SetActive(false);
        ResetHighlight();
    }

    public void OnPointerDown(PointerEventData e)
    {
        _eventCam = e.pressEventCamera;
        _selected = -1;
        RefreshIcons();
        if (wheelRoot != null) wheelRoot.SetActive(true);
        ResetHighlight();
    }

    public void OnDrag(PointerEventData e)
    {
        if (wheelRoot == null || !wheelRoot.activeSelf) return;

        Vector2 center = RectTransformUtility.WorldToScreenPoint(_eventCam, _self.position);
        Vector2 drag = e.position - center;

        if (drag.magnitude < selectDeadzonePx) { _selected = -1; ResetHighlight(); return; }

        Vector2 dragDir = drag.normalized;
        int best = -1; float bestDot = -1f;
        for (int i = 0; i < slotRects.Length; i++)
        {
            if (slotRects[i] == null) continue;
            Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(_eventCam, slotRects[i].position);
            Vector2 slotDir = (slotScreen - center);
            if (slotDir.sqrMagnitude < 1f) continue;
            float dot = Vector2.Dot(dragDir, slotDir.normalized);
            if (dot > bestDot) { bestDot = dot; best = i; }
        }
        _selected = best;
        ApplyHighlight();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (_selected >= 0) PlaySlot(_selected);
        if (wheelRoot != null) wheelRoot.SetActive(false);
        _selected = -1;
        ResetHighlight();
        _eventCam = null;
    }

    /// <summary>Dispara el emote del slot (0..2). Publico para testeo. Respeta cooldown.</summary>
    public bool PlaySlot(int slot)
    {
        if (Time.time < _nextAllowed) return false;

        var profile = ServiceLocator.Resolve<IProfileService>();
        if (profile == null) return false;
        string id = profile.GetEmoteId(slot);
        if (string.IsNullOrEmpty(id)) return false;

        var localPb = PlayerBrain.Local;
        if (localPb == null) return false;
        var source = localPb.GetComponent<Character>();
        if (source == null) return false;

        if (_ghostCtrl == null) _ghostCtrl = Object.FindAnyObjectByType<GhostModeController>();
        bool fromGhost = _ghostCtrl != null && _ghostCtrl.IsGhostActive;
        Vector3 ghostPos = (fromGhost && _ghostCtrl.GhostTransform != null)
            ? _ghostCtrl.GhostTransform.position
            : source.transform.position;

        EventBus.Publish(new EmoteUsedEvent
        {
            Source    = source,
            EmoteId   = id,
            FromGhost = fromGhost,
            GhostPos  = ghostPos,
            BodyPos   = source.transform.position
        });

        _nextAllowed = Time.time + cooldownSeconds;
        return true;
    }

    void RefreshIcons()
    {
        var profile = ServiceLocator.Resolve<IProfileService>();
        if (profile == null || profile.Catalog == null) return;
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] == null) continue;
            var e = profile.Catalog.GetEmote(profile.GetEmoteId(i));
            slotIcons[i].sprite = e != null ? e.icon : null;
            slotIcons[i].enabled = e != null && e.icon != null;
        }
    }

    void ApplyHighlight()
    {
        for (int i = 0; i < slotRects.Length; i++)
            if (slotRects[i] != null)
                slotRects[i].localScale = Vector3.one * (i == _selected ? selectedScale : 1f);
    }

    void ResetHighlight()
    {
        for (int i = 0; i < slotRects.Length; i++)
            if (slotRects[i] != null) slotRects[i].localScale = Vector3.one;
    }
}
