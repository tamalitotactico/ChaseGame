using UnityEngine;
#if FUSION2
using Fusion;
#endif

/// <summary>
/// Despawn compatible con red: usa Runner.Despawn si el objeto tiene un NetworkObject activo con
/// StateAuthority; Object.Destroy en caso contrario (Solo o pre-red).
///
/// Usar en cualquier script que hoy llama Destroy(gameObject): Projectile, Placeable,
/// PlaceableRegistry, etc. No modifica la ruta local — si no hay runner, es un Destroy normal.
/// </summary>
public static class NetDespawn
{
    public static void Despawn(GameObject go)
    {
        if (go == null) return;
#if FUSION2
        var no = go.GetComponent<NetworkObject>();
        if (no != null && no.IsValid)
        {
            // Objeto networkeado: SOLO el host (StateAuthority) lo despawnea; replica a los clientes.
            // Un proxy cliente NO debe Object.Destroy (destruiria el objeto replicado localmente ->
            // desync). Si no es authority, no hace nada y espera el despawn del host.
            if (no.HasStateAuthority && no.Runner != null) no.Runner.Despawn(no);
            return;
        }
#endif
        Object.Destroy(go);
    }
}
