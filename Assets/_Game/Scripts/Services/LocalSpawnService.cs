using UnityEngine;

/// <summary>
/// Implementacion Phase 0 de ISpawnService. Usa Object.Instantiate/Destroy.
/// Phase 3 sera reemplazada por FusionSpawnService que llama Runner.Spawn /
/// Runner.Despawn para entidades replicadas.
///
/// Registro: hecho por GameManager en Awake via ServiceLocator.Register.
/// </summary>
public sealed class LocalSpawnService : ISpawnService
{
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return prefab != null ? Object.Instantiate(prefab, position, rotation) : null;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        return prefab != null ? Object.Instantiate(prefab, position, rotation, parent) : null;
    }

    public void Despawn(GameObject instance)
    {
        if (instance != null) Object.Destroy(instance);
    }
}
