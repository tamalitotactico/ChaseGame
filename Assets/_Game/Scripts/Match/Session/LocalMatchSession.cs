/// <summary>
/// Sesion local (sin red). Cubre el modo Solo y el multiplayer-stub: este cliente es el unico host,
/// hay exactamente 1 humano local (slot 0 de su bando) y todos los demas slots son bots ("llenar con
/// bots"). Lee la composicion de MatchConfig (la fija MatchSetupScreen).
///
/// Cuando llegue Fusion, una FusionMatchSession implementara esta misma interfaz devolviendo
/// LocalPlayer / RemoteHuman / Bot segun los PlayerRef conectados; GameManager y la UI no cambian.
/// </summary>
public class LocalMatchSession : IMatchSession
{
    public int HuntersTotal => MatchConfig.HuntersTotal;
    public int PreysTotal   => MatchConfig.PreysTotal;

    public CharacterTeam LocalTeam => MatchConfig.PlayerTeam;

    public bool IsHost => true;

    public MatchSlotKind SlotKind(CharacterTeam team, int index)
    {
        // El humano local ocupa el slot 0 de su bando; el resto se llena con bots.
        if (team == LocalTeam && index == 0) return MatchSlotKind.LocalPlayer;
        return MatchSlotKind.Bot;
    }
}
