/// <summary>
/// Catalogo de eventos publicados via EventBus. Cada uno es struct para evitar GC.
/// Phase 3: estos seran serializables como NetworkInput / NetworkBehaviour ticks.
/// </summary>
public struct CharacterSpawnedEvent
{
    public Character Character;
}

public struct CharacterDamagedEvent
{
    public Character Character;
    public int CurrentHealth;
    public int MaxHealth;
}

public struct CharacterDiedEvent
{
    public Character Character;
}

public struct CharacterRevivedEvent
{
    public Character Character;
}

public struct CharacterDownedEvent
{
    public Character Character;
}

/// <summary>Tick continuo del progreso de revive de un Prey downed. Para HUD bar.</summary>
public struct ReviveProgressChangedEvent
{
    public Character Character;       // el downed
    public float     Progress;        // 0..1
    public float     BleedOutRemaining;
    public bool      HasReviver;      // true si hay al menos un Prey en proximidad
}

public struct AbilityActivatedEvent
{
    public Character Owner;
    public int Slot;
    public string AbilityId;
}

public struct InteractionStartedEvent
{
    public Character Interactor;
}

public struct InteractionCompletedEvent
{
    public Character Interactor;
}

public struct MatchStateChangedEvent
{
    public string From;
    public string To;
}

/// <summary>El match entro a LobbyState. La UI de lobby se muestra.</summary>
public struct LobbyEnteredEvent { }

/// <summary>El match salio de LobbyState (empezo la partida). La UI de lobby se oculta.</summary>
public struct LobbyExitedEvent { }

public struct MatchStartedEvent { }

public struct MatchEndedEvent
{
    public CharacterTeam WinningTeam;
    public string Reason;
}

public struct CountdownTickEvent
{
    public int SecondsLeft;
}

public struct MatchTimerTickEvent
{
    public float SecondsRemaining;
}
