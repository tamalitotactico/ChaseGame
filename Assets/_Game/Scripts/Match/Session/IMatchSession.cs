/// <summary>
/// Sesion de partida: decide la COMPOSICION (cuantos por bando + que es cada slot: humano local,
/// humano remoto o bot). GameManager la consume para spawnear sin saber de red.
///
/// - Solo / multiplayer-stub: LocalMatchSession (host unico, 1 humano local, resto bots).
/// - Phase 3 (Bloque 8): FusionMatchSession resolvera SlotKind por PlayerRef conectados + relleno
///   con bots del host. La UI y GameManager NO cambian: solo se registra otra implementacion.
/// </summary>
public interface IMatchSession
{
    int HuntersTotal { get; }
    int PreysTotal   { get; }

    /// <summary>Bando del jugador de ESTE cliente.</summary>
    CharacterTeam LocalTeam { get; }

    /// <summary>true si este cliente es el host/autoridad (quien rellena con bots y arranca).</summary>
    bool IsHost { get; }

    /// <summary>Naturaleza del slot (team, index) dentro de su pool. Determina el brain a usar.</summary>
    MatchSlotKind SlotKind(CharacterTeam team, int index);
}
