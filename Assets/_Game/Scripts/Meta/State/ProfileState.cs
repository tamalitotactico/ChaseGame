using System;

/// <summary>
/// Estado completo del perfil del jugador, serializado a JSON por ProfileService.
/// Meta-estado persistente (lo que el jugador posee y tiene equipado), separado del
/// estado-de-partida efimero (el rol resuelto en runtime dentro del lobby).
///
/// Monedas/nivel/trofeos son DISPLAY en Phase 2 (sin earn/spend hasta Phase 7).
/// </summary>
[Serializable]
public class ProfileState
{
    public string playerName = "Player";
    public int level = 1;
    public int trophies = 0;

    // Valores de ejemplo (display-only en Phase 2; economia real en Phase 7).
    public int coins = 2050;
    public int gems = 150;

    public LoadoutState loadout = new();
    public OwnershipState ownership = new();

    public int GetCurrency(CurrencyType type) => type == CurrencyType.Gems ? gems : coins;
}
