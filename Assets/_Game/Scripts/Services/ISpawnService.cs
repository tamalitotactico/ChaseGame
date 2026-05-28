using UnityEngine;

/// <summary>
/// Abstraccion para instanciar entidades de gameplay (proyectiles, trampas,
/// pickups, characters). Phase 0 usa LocalSpawnService (Object.Instantiate).
/// Phase 3 swappea a FusionSpawnService que llama Runner.Spawn() para
/// entidades replicadas.
///
/// Las abilities y modos NUNCA llaman Object.Instantiate directamente para
/// gameplay objects.
/// </summary>
public interface ISpawnService
{
    GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
    GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent);
    void       Despawn(GameObject instance);
}
