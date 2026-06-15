using UnityEngine;

/// <summary>
/// Base de las habilidades que COLOCAN un objeto en el mundo con cupo limitado (Raise Wall, Beacon,
/// trampas). Centraliza el spawn via ISpawnService + el registro/cupo en PlaceableRegistry. Las
/// subclases solo eligen prefab, posicion y configuran el placeable concreto.
/// </summary>
public abstract class PlaceableAbility : Ability
{
    protected PlaceableAbility(AbilityData data) : base(data) { }

    /// <summary>Spawnea el placeable, lo inicializa (owner + lifetime) y lo registra con cupo. Si al
    /// registrar se excede maxInstances, PlaceableRegistry despawnea el mas antiguo. Devuelve el GO.</summary>
    protected GameObject SpawnPlaceable(in AbilityContext ctx, GameObject prefab, Vector3 pos,
                                        Quaternion rot, int maxInstances, float lifetime)
    {
        if (prefab == null || ctx.SpawnService == null) return null;

        var go = ctx.SpawnService.Spawn(prefab, pos, rot);
        if (go == null) return null;

        if (go.TryGetComponent<Placeable>(out var p)) p.Init(ctx.Owner, lifetime);
        PlaceableRegistry.Register(ctx.Owner, GetType(), go, maxInstances);
        return go;
    }
}
