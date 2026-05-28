using UnityEngine;

/// <summary>
/// Datos pasados a Aimer.Begin() y Ability.Execute(). El AbilityController
/// lo construye cada vez que arranca una activacion.
///
/// Diseno: solo refs estables/datos por valor; nada que cambie entre
/// frames. La aim direction y el target del frame se pasan separadamente
/// via BrainIntent (a Aimer) y AimResult (a Execute).
/// </summary>
public struct AbilityContext
{
    public Character        Owner;
    public Vector2          OwnerPosition;
    public Vector2          MoveDirection;   // direccion de movimiento al castear (fallback)
    public Vector2          FacingDirection; // ultima direccion no-cero (fallback)
    public IAuthorityContext Authority;
    public ISpawnService     SpawnService;
}
