using UnityEngine;

/// <summary>
/// Output del Aimer al confirmar la habilidad. Contiene todos los datos
/// que la habilidad necesita para Execute() (direccion, posicion, target).
///
/// Un AimResult limpio (default) significa "sin apuntar" (instant cast).
/// </summary>
public struct AimResult
{
    public Vector2  Direction;       // direccion confirmada (-1..1, normalizada)
    public Vector3  TargetPosition;  // punto del mundo apuntado (para area)
    public Transform TargetEntity;    // entidad apuntada (para target lock)
    public bool     HasDirection;
    public bool     HasPosition;
    public bool     HasTarget;
    public bool     Explicit;        // true si el jugador apunto con drag (no fallback)
}
