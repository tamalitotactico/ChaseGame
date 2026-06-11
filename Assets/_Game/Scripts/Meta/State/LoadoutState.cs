using System;

/// <summary>
/// Loadout persistente del jugador (parte de ProfileState, serializado a JSON).
/// Guarda IDS string (estables) que MetaCatalog resuelve a SO. El jugador mantiene
/// SIEMPRE un Hunter y un Prey equipados (el rol en partida se asigna al azar) + 3 emotes
/// a nivel de cuenta.
/// </summary>
[Serializable]
public class LoadoutState
{
    public string hunterCharId;
    public string hunterSkinId;
    public string preyCharId;
    public string preySkinId;
    public string[] emoteIds = new string[3];

    public string GetCharId(CharacterTeam role) =>
        role == CharacterTeam.Hunter ? hunterCharId : preyCharId;

    public string GetSkinId(CharacterTeam role) =>
        role == CharacterTeam.Hunter ? hunterSkinId : preySkinId;

    public void SetEquipped(CharacterTeam role, string charId, string skinId)
    {
        if (role == CharacterTeam.Hunter) { hunterCharId = charId; hunterSkinId = skinId; }
        else                              { preyCharId = charId;   preySkinId = skinId; }
    }
}
