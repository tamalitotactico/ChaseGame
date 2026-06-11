using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalogo maestro del meta-juego: todos los personajes, emotes y modos. Fuente unica
/// para poblar los grids y para RESOLVER ids -> assets (loadout/ownership guardan ids string,
/// estables a traves de serializacion; este catalogo los mapea a los SO).
///
/// Indices construidos lazy en el primer acceso (se reconstruyen al recargar dominio).
/// </summary>
[CreateAssetMenu(fileName = "MetaCatalog", menuName = "ChaseGame/Meta/Catalog")]
public class MetaCatalog : ScriptableObject
{
    public List<MetaCharacter> characters = new();
    public List<EmoteData> emotes = new();
    public List<GameModeData> gameModes = new();

    Dictionary<string, MetaCharacter> _charById;
    Dictionary<string, Skin> _skinById;
    Dictionary<string, EmoteData> _emoteById;
    Dictionary<string, GameModeData> _modeById;

    public MetaCharacter GetCharacter(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureIndex();
        return _charById.TryGetValue(id, out var c) ? c : null;
    }

    public Skin GetSkin(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureIndex();
        return _skinById.TryGetValue(id, out var s) ? s : null;
    }

    public EmoteData GetEmote(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureIndex();
        return _emoteById.TryGetValue(id, out var e) ? e : null;
    }

    public GameModeData GetGameMode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureIndex();
        return _modeById.TryGetValue(id, out var m) ? m : null;
    }

    /// <summary>Personajes de un rol (Hunters o Preys), en el orden del catalogo.</summary>
    public IEnumerable<MetaCharacter> CharactersForRole(CharacterTeam role)
    {
        for (int i = 0; i < characters.Count; i++)
            if (characters[i] != null && characters[i].role == role)
                yield return characters[i];
    }

    /// <summary>Llamar si las listas cambian en runtime (no necesario en uso normal).</summary>
    public void InvalidateIndex() => _charById = null;

    void EnsureIndex()
    {
        if (_charById != null) return;

        _charById = new Dictionary<string, MetaCharacter>();
        _skinById = new Dictionary<string, Skin>();
        _emoteById = new Dictionary<string, EmoteData>();
        _modeById = new Dictionary<string, GameModeData>();

        for (int i = 0; i < characters.Count; i++)
        {
            var c = characters[i];
            if (c == null || string.IsNullOrEmpty(c.id)) continue;
            _charById[c.id] = c;
            if (c.skins == null) continue;
            for (int s = 0; s < c.skins.Count; s++)
            {
                var sk = c.skins[s];
                if (sk != null && !string.IsNullOrEmpty(sk.id)) _skinById[sk.id] = sk;
            }
        }
        for (int i = 0; i < emotes.Count; i++)
        {
            var e = emotes[i];
            if (e != null && !string.IsNullOrEmpty(e.id)) _emoteById[e.id] = e;
        }
        for (int i = 0; i < gameModes.Count; i++)
        {
            var m = gameModes[i];
            if (m != null && !string.IsNullOrEmpty(m.id)) _modeById[m.id] = m;
        }
    }
}
