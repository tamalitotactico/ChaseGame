/// <summary>
/// Configuracion de partida que SOBREVIVE a SceneManager.LoadScene. Los managers son
/// scene-scoped (se recrean al recargar), pero esta clase estatica persiste porque
/// LoadScene no recarga el dominio. El lobby la setea; GameManager la lee en Start.
///
/// Flujo:
///   - Fresh / volver al lobby: Configured = false  -> GameManager muestra LobbyState.
///   - Lobby elige rol:         StartMatch()         -> Configured = true, arranca match.
///   - Rematch:                 Configured se mantiene true -> arranca con el mismo rol.
///
/// Escalable: agregar aqui seleccion de personaje/modo cuando exista mas de uno.
/// </summary>
public static class MatchConfig
{
    public static CharacterTeam PlayerTeam = CharacterTeam.Prey;

    /// <summary>false = mostrar lobby; true = arrancar match directo con PlayerTeam.</summary>
    public static bool Configured = false;

    /// <summary>Modo elegido en el Hub/Select Gamemode. Sobrevive el LoadScene (estatico).</summary>
    public static string SelectedModeId = "survival";
}
