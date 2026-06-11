using System;
using System.Collections.Generic;

/// <summary>
/// Que posee el jugador (para el estado owned/locked del grid) + favoritos. Parte de
/// ProfileState serializado a JSON, por eso usa List&lt;string&gt; (JsonUtility no serializa HashSet).
/// Las listas son chicas; Contains lineal es suficiente en Phase 2.
/// </summary>
[Serializable]
public class OwnershipState
{
    public List<string> ownedCharacters = new();
    public List<string> ownedSkins = new();
    public List<string> ownedEmotes = new();
    public List<string> favoriteCharacters = new();

    public bool OwnsCharacter(string id) => ownedCharacters.Contains(id);
    public bool OwnsSkin(string id) => ownedSkins.Contains(id);
    public bool OwnsEmote(string id) => ownedEmotes.Contains(id);
    public bool IsFavorite(string id) => favoriteCharacters.Contains(id);

    public void GrantCharacter(string id) { if (!ownedCharacters.Contains(id)) ownedCharacters.Add(id); }
    public void GrantSkin(string id)      { if (!ownedSkins.Contains(id)) ownedSkins.Add(id); }
    public void GrantEmote(string id)     { if (!ownedEmotes.Contains(id)) ownedEmotes.Add(id); }

    public void SetFavorite(string id, bool favorite)
    {
        bool has = favoriteCharacters.Contains(id);
        if (favorite && !has) favoriteCharacters.Add(id);
        else if (!favorite && has) favoriteCharacters.Remove(id);
    }
}
