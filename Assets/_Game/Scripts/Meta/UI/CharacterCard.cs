using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tarjeta de personaje del grid de Customize. Construida 100% por codigo (Create) para no depender
/// de un prefab. Afinada al mockup: card landscape con el splash a sangre (clip redondeado via Mask),
/// nombre abajo-izquierda en mayusculas con contorno, y corazon de favorito arriba-derecha.
/// Locked = arte oscurecido + etiqueta. El grid inyecta los callbacks (onClick / onFavorite).
/// </summary>
public class CharacterCard : MonoBehaviour
{
    Image _splash;
    GameObject _lock;
    TMP_Text _favIcon;
    TMP_Text _nameLabel;

    MetaCharacter _character;
    Action<MetaCharacter> _onClick;
    Action<MetaCharacter> _onFavorite;

    static readonly Color FavOn  = new Color(0.10f, 0.10f, 0.12f, 1f);   // corazon relleno (oscuro)
    static readonly Color FavOff = new Color(0.10f, 0.10f, 0.12f, 0.45f);

    public MetaCharacter Character => _character;

    public void Bind(MetaCharacter c, bool owned, bool favorite,
                     Action<MetaCharacter> onClick, Action<MetaCharacter> onFavorite)
    {
        _character = c;
        _onClick = onClick;
        _onFavorite = onFavorite;

        Sprite art = c != null ? (c.splash != null ? c.splash : c.icon) : null;
        if (_splash != null)
        {
            _splash.sprite = art;
            _splash.enabled = art != null;
            _splash.color = owned ? Color.white : MetaTheme.Locked;
        }
        if (_nameLabel != null) _nameLabel.text = c != null ? c.displayName.ToUpperInvariant() : "";
        if (_lock != null) _lock.SetActive(!owned);
        if (_favIcon != null) _favIcon.color = favorite ? FavOn : FavOff;
    }

    void RaiseClick()    { if (_onClick != null && _character != null) _onClick(_character); }
    void RaiseFavorite() { if (_onFavorite != null && _character != null) _onFavorite(_character); }

    /// <summary>Construye la jerarquia de la tarjeta bajo parent y devuelve el componente listo.</summary>
    public static CharacterCard Create(Transform parent)
    {
        var go = new GameObject("Card", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var bg = go.AddComponent<Image>();
        bg.sprite = MetaTheme.Rounded();
        bg.type = Image.Type.Sliced;
        bg.color = MetaTheme.Card;
        var mask = go.AddComponent<Mask>();          // recorta el splash a las esquinas redondeadas
        mask.showMaskGraphic = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        var card = go.AddComponent<CharacterCard>();

        // Splash a sangre (rellena la card; clip por Mask)
        var splashGo = new GameObject("Splash", typeof(RectTransform));
        splashGo.transform.SetParent(go.transform, false);
        var splash = splashGo.AddComponent<Image>();
        splash.raycastTarget = false;
        splash.preserveAspect = false;
        Stretch(splashGo.GetComponent<RectTransform>(), 0, 0, 0, 0);
        card._splash = splash;

        // Banda oscura inferior para legibilidad del nombre (sharp_edges: no-text-outline-or-shadow)
        var nameBgGo = new GameObject("NameBg", typeof(RectTransform));
        nameBgGo.transform.SetParent(go.transform, false);
        var nameBg = nameBgGo.AddComponent<Image>();
        nameBg.color = new Color(0f, 0f, 0f, 0.38f);
        nameBg.raycastTarget = false;
        var nbr = nameBgGo.GetComponent<RectTransform>();
        nbr.anchorMin = new Vector2(0, 0); nbr.anchorMax = new Vector2(1, 0); nbr.pivot = new Vector2(0.5f, 0);
        nbr.offsetMin = new Vector2(0, 0); nbr.offsetMax = new Vector2(0, 52);

        // Nombre abajo-izquierda, blanco + contorno
        var nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(go.transform, false);
        var label = nameGo.AddComponent<TextMeshProUGUI>();
        label.text = "";
        label.fontSize = 24;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.BottomLeft;
        label.raycastTarget = false;
        label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 24;
        label.outlineWidth = 0.25f; label.outlineColor = Color.black;
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(1, 0); nr.pivot = new Vector2(0.5f, 0);
        nr.offsetMin = new Vector2(14, 8); nr.offsetMax = new Vector2(-14, 48);
        card._nameLabel = label;

        // Overlay locked
        var lockGo = new GameObject("Lock", typeof(RectTransform));
        lockGo.transform.SetParent(go.transform, false);
        var lockLabel = lockGo.AddComponent<TextMeshProUGUI>();
        lockLabel.text = "LOCKED";
        lockLabel.fontSize = 26;
        lockLabel.fontStyle = FontStyles.Bold;
        lockLabel.color = new Color(1f, 1f, 1f, 0.9f);
        lockLabel.alignment = TextAlignmentOptions.Center;
        lockLabel.raycastTarget = false;
        lockLabel.outlineWidth = 0.25f; lockLabel.outlineColor = Color.black;
        Stretch(lockGo.GetComponent<RectTransform>(), 0, 0, 0, 0);
        card._lock = lockGo;

        // Corazon de favorito arriba-derecha (touch target 48)
        var favGo = new GameObject("Fav", typeof(RectTransform));
        favGo.transform.SetParent(go.transform, false);
        var fav = favGo.AddComponent<TextMeshProUGUI>();
        fav.text = "♥"; // corazon
        fav.fontSize = 30;
        fav.alignment = TextAlignmentOptions.Center;
        fav.outlineWidth = 0.2f; fav.outlineColor = Color.white;
        var favBtn = favGo.AddComponent<Button>();
        favBtn.targetGraphic = fav;
        var fr = favGo.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(1, 1); fr.anchorMax = new Vector2(1, 1); fr.pivot = new Vector2(1, 1);
        fr.anchoredPosition = new Vector2(-6, -6); fr.sizeDelta = new Vector2(48, 48);
        card._favIcon = fav;

        btn.onClick.AddListener(card.RaiseClick);
        favBtn.onClick.AddListener(card.RaiseFavorite);
        return card;
    }

    static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}
