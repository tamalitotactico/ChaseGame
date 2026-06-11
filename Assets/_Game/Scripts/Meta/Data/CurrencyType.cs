/// <summary>
/// Monedas del meta-juego. Phase 2: solo display (no se gana ni se gasta).
/// Phase 7: economia real (recompensa por partida / IAP).
/// </summary>
public enum CurrencyType
{
    /// <summary>Comunes: se ganan jugando. Abundantes.</summary>
    Coins = 0,

    /// <summary>Premium: raras; cofres o compra real.</summary>
    Gems = 1
}
