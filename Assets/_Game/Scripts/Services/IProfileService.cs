/// <summary>
/// Servicio del perfil/meta del jugador: loadout equipado, ownership, favoritos y monedas.
/// Persistente cross-scene (registrado por AppRoot con DontDestroyOnLoad) para que el
/// loadout este disponible cuando GameManager spawnea en la escena de partida.
///
/// La UI lo resuelve por ServiceLocator y reacciona a LoadoutChangedEvent / EmotesChangedEvent /
/// CurrencyChangedEvent. Phase 5: una implementacion de red puede sincronizar el perfil con backend.
/// </summary>
public interface IProfileService
{
    ProfileState State { get; }
    MetaCatalog Catalog { get; }

    MetaCharacter GetEquippedCharacter(CharacterTeam role);
    Skin GetEquippedSkin(CharacterTeam role);
    void Equip(CharacterTeam role, string charId, string skinId);

    string GetEmoteId(int slot);
    void SetEmote(int slot, string emoteId);

    bool IsOwned(MetaCharacter character);
    bool IsOwnedSkin(Skin skin);
    bool IsFavorite(string characterId);
    void SetFavorite(string characterId, bool favorite);

    int GetCurrency(CurrencyType type);
    void Save();
}
