/// <summary>
/// Contexto de seleccion compartido para abrir el Detalle de Personaje sin acoplar pantallas
/// (Customize NO referencia a CharacterDetail directamente; deja aqui que personaje/rol inspeccionar
/// y luego llama IScreenService.Show("CharacterDetail")). Es un dato compartido, no una referencia
/// screen-a-screen. Scene-scoped del meta; se sobreescribe en cada apertura.
/// </summary>
public static class MetaSelection
{
    public static CharacterTeam Role = CharacterTeam.Hunter;
    public static string CharacterId;

    public static void Set(CharacterTeam role, string characterId)
    {
        Role = role;
        CharacterId = characterId;
    }
}
