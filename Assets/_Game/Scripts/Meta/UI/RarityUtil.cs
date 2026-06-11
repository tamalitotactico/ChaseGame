using UnityEngine;

/// <summary>
/// Helpers de presentacion para Rarity (color del marco/strip y nombre legible).
/// Display-only: la rareza no afecta gameplay en Phase 2 (ver [[phase2-ui-decisions]]).
/// </summary>
public static class RarityUtil
{
    public static Color Color(Rarity r)
    {
        switch (r)
        {
            case Rarity.PocoComun:  return new Color(0.45f, 0.80f, 0.40f); // verde
            case Rarity.Raro:       return new Color(0.30f, 0.55f, 0.95f); // azul
            case Rarity.Epico:      return new Color(0.65f, 0.35f, 0.90f); // morado
            case Rarity.Legendario: return new Color(0.95f, 0.70f, 0.20f); // dorado
            case Rarity.Divino:     return new Color(0.95f, 0.30f, 0.45f); // rojo/rosa
            default:                return new Color(0.55f, 0.58f, 0.62f); // gris (Comun)
        }
    }

    public static string DisplayName(Rarity r)
    {
        switch (r)
        {
            case Rarity.PocoComun:  return "Poco Comun";
            case Rarity.Raro:       return "Raro";
            case Rarity.Epico:      return "Epico";
            case Rarity.Legendario: return "Legendario";
            case Rarity.Divino:     return "Divino";
            default:                return "Comun";
        }
    }
}
