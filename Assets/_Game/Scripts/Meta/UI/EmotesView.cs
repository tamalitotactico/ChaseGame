using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sub-vista de Emotes de Customize: 3 slots equipados (rueda) + grid de todos los emotes del
/// catalogo. El slot seleccionado se resalta; tocar un emote del grid lo asigna a ese slot via
/// IProfileService.SetEmote (persiste + publica EmotesChangedEvent). El boton central despliega/oculta
/// la rueda de slots. Los botones del grid se construyen por codigo (sin prefab).
/// </summary>
public class EmotesView : MonoBehaviour
{
    [Header("Rueda (3 slots equipados)")]
    [SerializeField] GameObject wheelRoot;          // contenedor de los 3 slots (toggle)
    [SerializeField] Button centerToggle;           // boton central despliega/oculta
    [SerializeField] Button[] slotButtons = new Button[3];
    [SerializeField] Image[] slotIcons = new Image[3];

    [Header("Grid de emotes")]
    [SerializeField] RectTransform gridContent;     // GridLayoutGroup

    [Header("Preview / Confirmar")]
    [Tooltip("Bocadillo del personaje: muestra el ultimo emote asignado.")]
    [SerializeField] Image previewBubble;
    [Tooltip("READY: confirma y vuelve a la pantalla anterior.")]
    [SerializeField] Button readyButton;

    static readonly Color SlotNormal = new Color(0.70f, 0.70f, 0.73f, 1f);
    static readonly Color SlotSelected = new Color(0.30f, 0.55f, 0.95f, 1f);

    int _selectedSlot = 0;
    bool _gridBuilt;
    readonly List<Button> _gridButtons = new();

    void Awake()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            if (slotButtons[i] != null) slotButtons[i].onClick.AddListener(() => SelectSlot(slot));
        }
        if (centerToggle != null) centerToggle.onClick.AddListener(ToggleWheel);
        if (readyButton != null) readyButton.onClick.AddListener(() => ServiceLocator.Resolve<IScreenService>()?.Back());
    }

    public void Refresh()
    {
        BuildGrid();
        RefreshSlots();
        HighlightSlot();
    }

    void BuildGrid()
    {
        if (_gridBuilt) return;
        var profile = ServiceLocator.Resolve<IProfileService>();
        if (profile == null || profile.Catalog == null || gridContent == null) return;

        var emotes = profile.Catalog.emotes;
        for (int i = 0; i < emotes.Count; i++)
        {
            var e = emotes[i];
            if (e == null) continue;
            string id = e.id;

            var go = new GameObject("Emote", typeof(RectTransform));
            go.transform.SetParent(gridContent, false);
            var bg = go.AddComponent<Image>();
            bg.sprite = MetaTheme.Rounded(); bg.type = Image.Type.Sliced;
            bg.color = MetaTheme.Button;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            // Icono si existe; si no, etiqueta de texto. NUNCA ambos en el mismo GO:
            // Image y TextMeshProUGUI son Graphic y no pueden coexistir (NRE).
            var childGo = new GameObject(e.icon != null ? "Icon" : "Label", typeof(RectTransform));
            childGo.transform.SetParent(go.transform, false);
            var cr = childGo.GetComponent<RectTransform>();
            cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
            cr.offsetMin = new Vector2(8, 8); cr.offsetMax = new Vector2(-8, -8);
            if (e.icon != null)
            {
                var icon = childGo.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.sprite = e.icon;
            }
            else
            {
                var lbl = childGo.AddComponent<TextMeshProUGUI>();
                lbl.text = e.displayName;
                lbl.fontSize = 14;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.raycastTarget = false;
            }

            btn.onClick.AddListener(() => AssignToSelected(id));
            _gridButtons.Add(btn);
        }
        _gridBuilt = true;
    }

    void AssignToSelected(string emoteId)
    {
        var profile = ServiceLocator.Resolve<IProfileService>();
        if (profile == null) return;
        profile.SetEmote(_selectedSlot, emoteId);
        RefreshSlots();

        if (previewBubble != null)
        {
            var e = profile.Catalog != null ? profile.Catalog.GetEmote(emoteId) : null;
            previewBubble.sprite = e != null ? e.icon : null;
            previewBubble.enabled = e != null && e.icon != null;
        }
    }

    void SelectSlot(int slot)
    {
        _selectedSlot = Mathf.Clamp(slot, 0, 2);
        HighlightSlot();
    }

    void ToggleWheel()
    {
        if (wheelRoot != null) wheelRoot.SetActive(!wheelRoot.activeSelf);
    }

    void RefreshSlots()
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

    void HighlightSlot()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null) continue;
            var img = slotButtons[i].targetGraphic as Image;
            if (img != null) img.color = i == _selectedSlot ? SlotSelected : SlotNormal;
        }
    }
}
