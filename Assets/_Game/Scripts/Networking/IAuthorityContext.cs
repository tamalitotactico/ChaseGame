/// <summary>
/// Stub de networking. Indica si el codigo actual debe ejecutar logica autoritativa
/// (validar inputs, mover personaje, aplicar dano, instanciar entidades).
/// Phase 0 implementa LocalAuthority (siempre true).
/// Phase 3 implementa FusionAuthority leyendo Object.HasStateAuthority.
///
/// Quien lee esta interface (Character, AbilityController, CombatController) NO
/// debe llamar APIs de Photon directamente; toda referencia a red pasa por aqui.
/// </summary>
public interface IAuthorityContext
{
    /// <summary>True si esta instancia es controlada localmente por este cliente.</summary>
    bool IsLocal { get; }

    /// <summary>True si este cliente tiene autoridad para mutar estado replicado.</summary>
    bool IsAuthority { get; }

    /// <summary>True si los sistemas (motor, abilities, AI) deben simular este frame.</summary>
    bool CanSimulate { get; }
}
