#if FUSION2
using Fusion;
using UnityEngine;

/// <summary>
/// Implementacion de <see cref="ISpawnService"/> sobre Fusion (Host Mode). Solo el host/StateAuthority
/// debe spawnear entidades replicadas; los clientes las reciben por replicacion. Los prefabs spawneados
/// DEBEN tener un NetworkObject (characters, proyectiles, placeables).
///
/// El parent en red no se usa (los NetworkObject no se anidan como UI); el parametro se ignora en esta
/// implementacion y se conserva solo por compatibilidad con la interfaz local.
/// </summary>
public sealed class FusionSpawnService : ISpawnService
{
    readonly NetworkRunner _runner;

    public FusionSpawnService(NetworkRunner runner) { _runner = runner; }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null || _runner == null) return null;
        var no = _runner.Spawn(prefab, position, rotation);
        return no != null ? no.gameObject : null;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        // Parent ignorado en red (ver resumen). Spawn normal.
        return Spawn(prefab, position, rotation);
    }

    /// <summary>Spawn asignando InputAuthority a un jugador (para el personaje del humano de un slot).
    /// No esta en ISpawnService porque solo aplica a entidades controladas por un cliente.</summary>
    public GameObject SpawnForPlayer(GameObject prefab, Vector3 position, Quaternion rotation, PlayerRef inputAuthority)
    {
        if (prefab == null || _runner == null) return null;
        var no = _runner.Spawn(prefab, position, rotation, inputAuthority);
        return no != null ? no.gameObject : null;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null || _runner == null) return;
        var no = instance.GetComponent<NetworkObject>();
        if (no != null) _runner.Despawn(no);
        else Object.Destroy(instance);
    }
}
#endif
