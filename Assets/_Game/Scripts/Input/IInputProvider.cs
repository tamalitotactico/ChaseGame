using UnityEngine;

/// <summary>
/// Contrato comun para cualquier fuente de input de movimiento.
/// </summary>
public interface IInputProvider
{
    /// <summary>Vector de movimiento normalizado en rango [-1, 1].</summary>
    Vector2 GetMovementInput();

    /// <summary>True si esta fuente tiene input no-cero en este frame.</summary>
    bool IsActive { get; }
}
