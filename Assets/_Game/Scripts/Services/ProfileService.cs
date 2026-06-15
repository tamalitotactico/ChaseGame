using System.IO;
using UnityEngine;

/// <summary>
/// Implementacion local de IProfileService. Persiste ProfileState como JSON en
/// Application.persistentDataPath/profile.json (via JsonUtility).
///
/// Seed: si el perfil es nuevo o quedo invalido (catalogo cambio), equipa el primer
/// Hunter y Prey del catalogo + sus skins default + los primeros 3 emotes, y los marca owned.
/// Monedas/nivel/trofeos son display en Phase 2.
/// </summary>
public sealed class ProfileService : IProfileService
{
    const string FileName = "profile.json";

    readonly MetaCatalog _catalog;
    readonly string _path;
    ProfileState _state;

    public ProfileState State => _state;
    public MetaCatalog Catalog => _catalog;

    public ProfileService(MetaCatalog catalog)
    {
        _catalog = catalog;
        _path = Path.Combine(Application.persistentDataPath, FileName);
        Load();
    }

    void Load()
    {
        try
        {
            if (File.Exists(_path))
                _state = JsonUtility.FromJson<ProfileState>(File.ReadAllText(_path));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ProfileService] No se pudo leer el perfil ({e.Message}). Se regenera.");
        }

        if (_state == null) _state = new ProfileState();
        if (_state.loadout == null) _state.loadout = new LoadoutState();
        if (_state.ownership == null) _state.ownership = new OwnershipState();

        EnsureSeed();
    }

    /// <summary>Garantiza un loadout valido (starter desbloqueado) si el perfil es nuevo o invalido.</summary>
    void EnsureSeed()
    {
        if (_catalog == null) return;
        bool dirty = false;

        dirty |= SeedRole(CharacterTeam.Hunter);
        dirty |= SeedRole(CharacterTeam.Prey);

        // Phase 0/dev: desbloquear TODOS los personajes del catalogo + su skin default, para poder
        // elegir cualquiera de los 8 sin economia/shop. Cuando exista monetizacion, quitar esto.
        if (_catalog.characters != null)
            foreach (var c in _catalog.characters)
            {
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                if (!_state.ownership.OwnsCharacter(c.id)) { _state.ownership.GrantCharacter(c.id); dirty = true; }
                var sk = c.DefaultSkin;
                if (sk != null && !_state.ownership.OwnsSkin(sk.id)) { _state.ownership.GrantSkin(sk.id); dirty = true; }
            }

        if (_state.loadout.emoteIds == null || _state.loadout.emoteIds.Length != 3)
        {
            _state.loadout.emoteIds = new string[3];
            dirty = true;
        }
        for (int i = 0; i < 3 && i < _catalog.emotes.Count; i++)
        {
            var e = _catalog.emotes[i];
            if (e != null && string.IsNullOrEmpty(_state.loadout.emoteIds[i]))
            {
                _state.loadout.emoteIds[i] = e.id;
                _state.ownership.GrantEmote(e.id);
                dirty = true;
            }
        }

        if (dirty) Save();
    }

    bool SeedRole(CharacterTeam role)
    {
        var equipped = _catalog.GetCharacter(_state.loadout.GetCharId(role));
        if (equipped == null)
        {
            MetaCharacter first = null;
            foreach (var c in _catalog.CharactersForRole(role)) { first = c; break; }
            if (first == null) return false;

            var skin = first.DefaultSkin;
            _state.loadout.SetEquipped(role, first.id, skin != null ? skin.id : null);
            _state.ownership.GrantCharacter(first.id);
            if (skin != null) _state.ownership.GrantSkin(skin.id);
            return true;
        }

        // La skin equipada debe existir; si no, caer a la default del personaje.
        if (_catalog.GetSkin(_state.loadout.GetSkinId(role)) == null && equipped.DefaultSkin != null)
        {
            _state.loadout.SetEquipped(role, equipped.id, equipped.DefaultSkin.id);
            _state.ownership.GrantSkin(equipped.DefaultSkin.id);
            return true;
        }
        return false;
    }

    public MetaCharacter GetEquippedCharacter(CharacterTeam role) =>
        _catalog != null ? _catalog.GetCharacter(_state.loadout.GetCharId(role)) : null;

    public Skin GetEquippedSkin(CharacterTeam role)
    {
        if (_catalog == null) return null;
        var sk = _catalog.GetSkin(_state.loadout.GetSkinId(role));
        if (sk != null) return sk;
        var c = GetEquippedCharacter(role);
        return c != null ? c.DefaultSkin : null;
    }

    public void Equip(CharacterTeam role, string charId, string skinId)
    {
        _state.loadout.SetEquipped(role, charId, skinId);
        Save();
        EventBus.Publish(new LoadoutChangedEvent { Role = role, CharacterId = charId, SkinId = skinId });
    }

    public string GetEmoteId(int slot) =>
        (slot >= 0 && _state.loadout.emoteIds != null && slot < _state.loadout.emoteIds.Length)
            ? _state.loadout.emoteIds[slot] : null;

    public void SetEmote(int slot, string emoteId)
    {
        if (slot < 0 || slot > 2) return;
        if (_state.loadout.emoteIds == null || _state.loadout.emoteIds.Length != 3)
            _state.loadout.emoteIds = new string[3];
        _state.loadout.emoteIds[slot] = emoteId;
        Save();
        EventBus.Publish(new EmotesChangedEvent { EmoteIds = _state.loadout.emoteIds });
    }

    public bool IsOwned(MetaCharacter character) =>
        character != null && _state.ownership.OwnsCharacter(character.id);

    public bool IsOwnedSkin(Skin skin) =>
        skin != null && _state.ownership.OwnsSkin(skin.id);

    public bool IsFavorite(string characterId) => _state.ownership.IsFavorite(characterId);

    public void SetFavorite(string characterId, bool favorite)
    {
        _state.ownership.SetFavorite(characterId, favorite);
        Save();
    }

    public int GetCurrency(CurrencyType type) => _state.GetCurrency(type);

    public void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonUtility.ToJson(_state, true));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ProfileService] No se pudo guardar el perfil: {e.Message}");
        }
    }
}
