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

/// <summary>Un StatusEffect se aplico a un Character. Generico (cualquier efecto): consumidores
/// filtran por tipo (e.Effect is FearedEffect) y/o por personaje (jugador local). Lo usa
/// CameraEffectsRig para sacudir la camara SOLO cuando el prey controlado por el jugador recibe
/// miedo. Escala a nuevos efectos (stun, charm) y a HUD de estados sin tocar el emisor.</summary>
public struct StatusEffectAppliedEvent
{
    public Character    Character;
    public StatusEffect Effect;
}

/// <summary>Un StatusEffect dejo de estar activo (expiro o se removio). Pareja de
/// [StatusEffectAppliedEvent] para limpiar estado en consumidores (ej. soltar el efecto de camara).</summary>
public struct StatusEffectRemovedEvent
{
    public Character    Character;
    public StatusEffect Effect;
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

// --- Meta-layer (Phase 2): navegacion y loadout. La UI se comunica solo por estos eventos. ---

/// <summary>El loadout equipado cambio (equipar personaje/skin). El Hub redibuja el avatar.</summary>
public struct LoadoutChangedEvent
{
    public CharacterTeam Role;
    public string CharacterId;
    public string SkinId;
}

/// <summary>Los 3 emotes equipados (a nivel cuenta) cambiaron.</summary>
public struct EmotesChangedEvent
{
    public string[] EmoteIds;
}

/// <summary>Una moneda cambio de valor (display en Phase 2).</summary>
public struct CurrencyChangedEvent
{
    public CurrencyType Type;
    public int Amount;
}

/// <summary>Se eligio un modo de juego en el selector.</summary>
public struct GameModeSelectedEvent
{
    public string ModeId;
}

/// <summary>Cambio la pantalla activa del meta (navegacion entre pantallas, no pestanas).</summary>
public struct ScreenChangedEvent
{
    public string ScreenId;
}
