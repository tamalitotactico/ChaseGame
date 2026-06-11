using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Grid reusable de personajes de un rol (Hunters o Preys). Pobla tarjetas (CharacterCard) desde
/// el MetaCatalog filtrado por rol, ordenadas favoritos -> owned -> rareza desc. Locked = silueta.
/// Las tarjetas se construyen por codigo (sin prefab). Decoupled: la navegacion la inyecta
/// CustomizeScreen via Init(onSelect); favoritos van directo a IProfileService.
/// </summary>
public class CharacterGridView : MonoBehaviour
{
    [SerializeField] CharacterTeam role = CharacterTeam.Hunter;
    [Tooltip("Contenedor con GridLayoutGroup donde se instancian las tarjetas.")]
    [SerializeField] RectTransform content;
    [Tooltip("Etiqueta tipo 'HUNTERS (x/total)'. Opcional.")]
    [SerializeField] TMP_Text counterLabel;

    Action<MetaCharacter> _onSelect;
    readonly List<CharacterCard> _cards = new();

    public CharacterTeam Role { get => role; set => role = value; }

    public void Init(Action<MetaCharacter> onSelect) => _onSelect = onSelect;

    public void Refresh()
    {
        var profile = ServiceLocator.Resolve<IProfileService>();
        if (profile == null || profile.Catalog == null || content == null) return;

        var list = new List<MetaCharacter>(profile.Catalog.CharactersForRole(role));
        list.Sort((a, b) =>
        {
            bool fa = profile.IsFavorite(a.id), fb = profile.IsFavorite(b.id);
            if (fa != fb) return fb.CompareTo(fa);             // favoritos primero
            bool oa = profile.IsOwned(a), ob = profile.IsOwned(b);
            if (oa != ob) return ob.CompareTo(oa);             // owned antes que locked
            int byRarity = ((int)b.rarity).CompareTo((int)a.rarity); // rareza desc
            if (byRarity != 0) return byRarity;
            return string.Compare(a.displayName, b.displayName, StringComparison.Ordinal);
        });

        EnsureCards(list.Count);

        int owned = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            bool isOwned = profile.IsOwned(c);
            if (isOwned) owned++;
            _cards[i].gameObject.SetActive(true);
            _cards[i].Bind(c, isOwned, profile.IsFavorite(c.id), OnCardClick, OnCardFavorite);
        }
        for (int i = list.Count; i < _cards.Count; i++)
            _cards[i].gameObject.SetActive(false);

        if (counterLabel != null)
            counterLabel.text = $"{(role == CharacterTeam.Hunter ? "HUNTERS" : "PREYS")} ({owned}/{list.Count})";
    }

    void EnsureCards(int needed)
    {
        for (int i = _cards.Count; i < needed; i++)
            _cards.Add(CharacterCard.Create(content));
    }

    void OnCardClick(MetaCharacter c) => _onSelect?.Invoke(c);

    void OnCardFavorite(MetaCharacter c)
    {
        var profile = ServiceLocator.Resolve<IProfileService>();
        if (profile == null || c == null) return;
        profile.SetFavorite(c.id, !profile.IsFavorite(c.id));
        Refresh(); // re-ordena (favoritos arriba) y actualiza la estrella
    }
}
