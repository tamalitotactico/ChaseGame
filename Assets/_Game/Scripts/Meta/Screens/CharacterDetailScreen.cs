using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Detalle de Personaje. Lee MetaSelection (rol + id) al mostrarse. Afinado al mockup: splash
/// dominante con nombre/titulo/rareza superpuestos arriba-izquierda; panel HABILIDADES a la derecha
/// = una columna de slots circulares (uno por habilidad) que al tocarse muestran su descripcion;
/// carrusel (< >) abajo-centro con thumbnail jugable; preview in-game abajo-izquierda; SELECT equipa.
/// La seleccion de skin queda opcional (skinsContent puede ser null: equipa la skin equipada/default).
/// </summary>
public class CharacterDetailScreen : ScreenController
{
    public override string ScreenId => "CharacterDetail";

    [Header("Cabecera (overlay sobre splash)")]
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] TMP_Text titleLabel;
    [SerializeField] TMP_Text rarityLabel;
    [SerializeField] Image rarityBadge;

    [Header("Splash")]
    [SerializeField] Image splashImage;
    [SerializeField] SplashVideoView splashVideo;

    [Header("Habilidades")]
    [Tooltip("Columna vertical donde se instancian los slots circulares de habilidad.")]
    [SerializeField] RectTransform abilitySlotsContainer;
    [SerializeField] TMP_Text abilityNameLabel;
    [SerializeField] TMP_Text abilityDescLabel;

    [Header("Skins (opcional)")]
    [SerializeField] RectTransform skinsContent;

    [Header("Preview / Carrusel / Accion")]
    [SerializeField] Image previewImage;
    [SerializeField] Image carouselThumb;
    [SerializeField] Button prevButton;
    [SerializeField] Button nextButton;
    [SerializeField] Button backButton;
    [SerializeField] Button readyButton;
    [SerializeField] TMP_Text readyLabel;

    CharacterTeam _role;
    readonly List<MetaCharacter> _roster = new();
    int _index;
    string _selectedSkinId;
    AbilityData[] _abilities;

    static readonly Color SlotNormal = new Color(0.70f, 0.70f, 0.73f, 1f);
    static readonly Color SlotSelected = new Color(0.30f, 0.55f, 0.95f, 1f);

    void Awake()
    {
        if (prevButton != null)  prevButton.onClick.AddListener(() => Step(-1));
        if (nextButton != null)  nextButton.onClick.AddListener(() => Step(1));
        if (backButton != null)  backButton.onClick.AddListener(() => Screens?.Back());
        if (readyButton != null) readyButton.onClick.AddListener(OnReady);
    }

    public override void OnShow()
    {
        _role = MetaSelection.Role;
        BuildRoster();
        _index = Mathf.Max(0, _roster.FindIndex(c => c != null && c.id == MetaSelection.CharacterId));
        BindCurrent();
    }

    void BuildRoster()
    {
        _roster.Clear();
        if (Profile == null || Profile.Catalog == null) return;
        foreach (var c in Profile.Catalog.CharactersForRole(_role)) _roster.Add(c);
    }

    MetaCharacter Current => (_index >= 0 && _index < _roster.Count) ? _roster[_index] : null;

    void Step(int dir)
    {
        if (_roster.Count == 0) return;
        _index = (_index + dir + _roster.Count) % _roster.Count;
        BindCurrent();
    }

    void BindCurrent()
    {
        var c = Current;
        if (c == null) return;

        var equippedSkin = Profile != null ? Profile.GetEquippedSkin(_role) : null;
        var equippedChar = Profile != null ? Profile.GetEquippedCharacter(_role) : null;
        _selectedSkinId = (equippedChar != null && equippedChar.id == c.id && equippedSkin != null)
            ? equippedSkin.id
            : (c.DefaultSkin != null ? c.DefaultSkin.id : null);

        if (nameLabel != null)   nameLabel.text = c.displayName;
        if (titleLabel != null)  titleLabel.text = string.IsNullOrEmpty(c.title) ? "" : c.title.ToUpperInvariant();
        if (rarityLabel != null) rarityLabel.text = RarityUtil.DisplayName(c.rarity).ToUpperInvariant();
        if (rarityBadge != null) rarityBadge.color = RarityUtil.Color(c.rarity);
        if (splashImage != null)
        {
            splashImage.sprite = c.splash;
            splashImage.enabled = c.splash != null;
        }

        BuildAbilities(c);
        BuildSkins(c);
        RefreshPreview();
        RefreshReady(c);
    }

    void BuildAbilities(MetaCharacter c)
    {
        if (abilitySlotsContainer == null) return;
        for (int i = abilitySlotsContainer.childCount - 1; i >= 0; i--)
            Destroy(abilitySlotsContainer.GetChild(i).gameObject);

        _abilities = c.gameplayData != null ? c.gameplayData.abilities : null;
        int count = _abilities != null ? _abilities.Length : 0;

        for (int i = 0; i < count; i++)
        {
            var ab = _abilities[i];
            int index = i;

            var slot = new GameObject("AbilitySlot", typeof(RectTransform));
            slot.transform.SetParent(abilitySlotsContainer, false);
            var bg = slot.AddComponent<Image>();
            bg.sprite = MetaTheme.Circle();
            bg.color = SlotNormal;
            var btn = slot.AddComponent<Button>();
            btn.targetGraphic = bg;
            var le = slot.AddComponent<LayoutElement>();
            le.minWidth = 64; le.minHeight = 64; le.preferredWidth = 64; le.preferredHeight = 64;

            // icono o numero dentro del slot
            var inner = new GameObject(ab != null && ab.icon != null ? "Icon" : "Num", typeof(RectTransform));
            inner.transform.SetParent(slot.transform, false);
            var ir = inner.GetComponent<RectTransform>();
            ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(10, 10); ir.offsetMax = new Vector2(-10, -10);
            if (ab != null && ab.icon != null)
            {
                var icon = inner.AddComponent<Image>();
                icon.sprite = ab.icon; icon.preserveAspect = true; icon.raycastTarget = false;
            }
            else
            {
                var num = inner.AddComponent<TextMeshProUGUI>();
                num.text = (index + 1).ToString();
                num.fontSize = 26; num.alignment = TextAlignmentOptions.Center;
                num.color = MetaTheme.TextDark; num.raycastTarget = false;
            }

            btn.onClick.AddListener(() => ShowAbility(index));
        }

        ShowAbility(0);
    }

    void ShowAbility(int index)
    {
        if (_abilities == null || index < 0 || index >= _abilities.Length)
        {
            if (abilityNameLabel != null) abilityNameLabel.text = "";
            if (abilityDescLabel != null) abilityDescLabel.text = "";
            return;
        }
        var ab = _abilities[index];
        if (abilityNameLabel != null) abilityNameLabel.text = ab != null ? ab.displayName : "";
        if (abilityDescLabel != null) abilityDescLabel.text = ab != null ? ab.description : "";

        // resalta el slot seleccionado
        if (abilitySlotsContainer != null)
            for (int i = 0; i < abilitySlotsContainer.childCount; i++)
            {
                var img = abilitySlotsContainer.GetChild(i).GetComponent<Image>();
                if (img != null) img.color = i == index ? SlotSelected : SlotNormal;
            }
    }

    void BuildSkins(MetaCharacter c)
    {
        if (skinsContent == null) return; // skins ocultas en el layout actual (mockup); equipa default
        for (int i = skinsContent.childCount - 1; i >= 0; i--)
            Destroy(skinsContent.GetChild(i).gameObject);

        if (c.skins == null) return;
        for (int i = 0; i < c.skins.Count; i++)
        {
            var skin = c.skins[i];
            if (skin == null) continue;
            string skinId = skin.id;
            bool owned = Profile != null && Profile.IsOwnedSkin(skin);

            var go = new GameObject("Skin", typeof(RectTransform));
            go.transform.SetParent(skinsContent, false);
            var bg = go.AddComponent<Image>();
            bg.sprite = MetaTheme.Rounded(); bg.type = Image.Type.Sliced;
            bg.color = skinId == _selectedSkinId ? SlotSelected : MetaTheme.Button;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 72; le.minHeight = 72;

            btn.onClick.AddListener(() => SelectSkin(skinId));
        }
    }

    void SelectSkin(string skinId)
    {
        _selectedSkinId = skinId;
        var c = Current;
        if (c != null) { BuildSkins(c); RefreshPreview(); RefreshReady(c); }
    }

    void RefreshPreview()
    {
        var c = Current;
        Skin skin = c != null ? c.GetSkin(_selectedSkinId) : null;
        Sprite s = skin != null && skin.playablePreview != null ? skin.playablePreview
                 : (c != null ? c.icon : null);
        if (previewImage != null)  { previewImage.sprite = s; previewImage.enabled = s != null; }
        if (carouselThumb != null) { carouselThumb.sprite = s; carouselThumb.enabled = s != null; }
    }

    void RefreshReady(MetaCharacter c)
    {
        bool ownedChar = Profile != null && Profile.IsOwned(c);
        if (readyButton != null) readyButton.interactable = ownedChar;
        if (readyLabel != null)  readyLabel.text = ownedChar ? "SELECT" : "BLOQUEADO";
    }

    void OnReady()
    {
        var c = Current;
        if (c == null || Profile == null || !Profile.IsOwned(c)) return;
        Profile.Equip(_role, c.id, _selectedSkinId);
        Screens?.Back();
    }
}
