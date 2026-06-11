using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Selector de modo. Destaca un modo principal ("Modo del dia", rotacion diaria determinista por
/// fecha) + lista los secundarios desde el MetaCatalog en la distribucion del mockup: 1 card ancho
/// arriba (secondaryTop) + una fila de cards abajo (secondaryBottom). Solo los no-comingSoon son
/// seleccionables: elegir publica GameModeSelectedEvent, fija MatchConfig.SelectedModeId y vuelve.
/// </summary>
public class GameModeScreen : ScreenController
{
    public override string ScreenId => "GameMode";

    [Header("Destacado (Modo del dia)")]
    [SerializeField] TMP_Text featuredLabel;
    [SerializeField] Image featuredSplash;
    [SerializeField] TMP_Text dayBadge;
    [SerializeField] Button featuredSelect;
    [SerializeField] TMP_Text featuredSelectLabel;

    [Header("Secundarios / Navegacion")]
    [Tooltip("Slot ancho (arriba) para el primer modo secundario.")]
    [SerializeField] RectTransform secondaryTop;
    [Tooltip("Fila (abajo) con HorizontalLayoutGroup para el resto de modos.")]
    [SerializeField] RectTransform secondaryBottom;
    [SerializeField] Button backButton;

    GameModeData _featured;

    void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(() => Screens?.Back());
        if (featuredSelect != null) featuredSelect.onClick.AddListener(() => Choose(_featured));
    }

    public override void OnShow()
    {
        if (Profile == null || Profile.Catalog == null) return;
        var modes = Profile.Catalog.gameModes;
        if (modes == null || modes.Count == 0) return;

        _featured = PickDaily(modes);
        BindFeatured(_featured);
        BuildSecondary(modes, _featured);
    }

    static GameModeData PickDaily(List<GameModeData> modes)
    {
        int day = (int)(DateTime.UtcNow.Date - new DateTime(2024, 1, 1)).TotalDays;
        int idx = ((day % modes.Count) + modes.Count) % modes.Count;
        return modes[idx];
    }

    void BindFeatured(GameModeData m)
    {
        if (m == null) return;
        if (featuredLabel != null) featuredLabel.text = m.displayName.ToUpperInvariant();
        if (dayBadge != null) dayBadge.text = "¡MODO DEL DÍA!";
        if (featuredSplash != null)
        {
            featuredSplash.sprite = m.splash;
            featuredSplash.enabled = m.splash != null;
        }
        bool selectable = !m.isComingSoon;
        if (featuredSelect != null) featuredSelect.interactable = selectable;
        if (featuredSelectLabel != null) featuredSelectLabel.text = selectable ? "SELECCIONAR" : "PROXIMAMENTE";
    }

    void BuildSecondary(List<GameModeData> modes, GameModeData skip)
    {
        Clear(secondaryTop);
        Clear(secondaryBottom);

        var secs = new List<GameModeData>();
        for (int i = 0; i < modes.Count; i++)
            if (modes[i] != null && modes[i] != skip) secs.Add(modes[i]);

        for (int i = 0; i < secs.Count; i++)
        {
            if (i == 0 && secondaryTop != null) CreateCard(secondaryTop, secs[i], true);
            else if (secondaryBottom != null) CreateCard(secondaryBottom, secs[i], false);
        }
    }

    static void Clear(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }

    /// <summary>fill=true: el card rellena su contenedor (slot ancho). fill=false: usa LayoutElement
    /// flexible para repartirse la fila por igual.</summary>
    void CreateCard(RectTransform parent, GameModeData m, bool fill)
    {
        var go = new GameObject("Mode", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        var bg = go.AddComponent<Image>();
        bg.sprite = MetaTheme.Rounded(); bg.type = Image.Type.Sliced; bg.color = MetaTheme.Card;
        var mask = go.AddComponent<Mask>(); mask.showMaskGraphic = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.interactable = !m.isComingSoon;

        if (fill)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        else
        {
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1; le.flexibleHeight = 1;
        }

        if (m.splash != null)
        {
            var spGo = new GameObject("Splash", typeof(RectTransform));
            spGo.transform.SetParent(go.transform, false);
            var sp = spGo.AddComponent<Image>();
            sp.sprite = m.splash; sp.preserveAspect = false; sp.raycastTarget = false;
            sp.color = m.isComingSoon ? MetaTheme.Locked : Color.white;
            var sr = spGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one; sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;
        }

        var bannerGo = new GameObject("Banner", typeof(RectTransform));
        bannerGo.transform.SetParent(go.transform, false);
        var banner = bannerGo.AddComponent<Image>();
        banner.color = new Color(0.12f, 0.12f, 0.14f, 0.72f);
        banner.raycastTarget = false;
        var br = bannerGo.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0); br.pivot = new Vector2(0.5f, 0);
        br.offsetMin = new Vector2(0, 0); br.offsetMax = new Vector2(0, 48);

        var txtGo = new GameObject("Label", typeof(RectTransform));
        txtGo.transform.SetParent(bannerGo.transform, false);
        var t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = m.isComingSoon ? m.displayName.ToUpperInvariant() + "  ·  PROXIMAMENTE" : m.displayName.ToUpperInvariant();
        t.fontSize = 22; t.fontStyle = FontStyles.Bold;
        t.color = Color.white; t.alignment = TextAlignmentOptions.Left; t.raycastTarget = false;
        var tr = txtGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(16, 0); tr.offsetMax = new Vector2(-16, 0);

        btn.onClick.AddListener(() => Choose(m));
    }

    void Choose(GameModeData m)
    {
        if (m == null || m.isComingSoon) return;
        MatchConfig.SelectedModeId = m.id;
        EventBus.Publish(new GameModeSelectedEvent { ModeId = m.id });
        Screens?.Back();
    }
}
