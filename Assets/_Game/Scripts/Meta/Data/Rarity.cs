/// <summary>
/// Tier de rareza del meta-juego. 6 niveles; cada uno ~x10 mas raro que el anterior,
/// el ultimo (Divino) x50 sobre Legendario. Esas probabilidades son de Phase 7 (cofres);
/// en Phase 2 la rareza es solo display + clave de ordenamiento del grid.
/// Personajes y skins tienen rareza de forma independiente.
/// El valor entero define el orden de sort (mayor = mas raro).
/// </summary>
public enum Rarity
{
    Comun = 0,
    PocoComun = 1,
    Raro = 2,
    Epico = 3,
    Legendario = 4,
    Divino = 5
}
