using UnityEngine;

/// <summary>
/// Servicio estatico de spawn de efectos VFX.
///
/// Dos modalidades:
///   PlayOnce  — instancia el prefab en una posicion mundial y lo destruye automaticamente.
///   Attach    — instancia el prefab como hijo de un Transform; devuelve un VFXHandle
///               para detenerlo manualmente (cuando el efecto de estado termina).
///
/// No usa pooling en esta version; para Phase 2 se puede agregar ObjectPool<T>.
/// </summary>
public static class VFXSpawner
{
    /// <summary>
    /// Instancia el prefab en la posicion dada y lo destruye tras autoDestroyDelay segundos.
    /// Util para efectos de impacto (golpe, dash, revive completado).
    /// </summary>
    public static void PlayOnce(GameObject prefab, Vector3 position, float autoDestroyDelay = 3f)
    {
        if (prefab == null) return;
        var go = Object.Instantiate(prefab, position, Quaternion.identity);
        Object.Destroy(go, autoDestroyDelay);
    }

    /// <summary>
    /// Instancia el prefab como hijo del Transform dado (sigue al personaje).
    /// Devuelve un VFXHandle; llamar Handle.Stop() cuando el efecto deba terminar.
    /// Util para efectos persistentes: stun stars, slow trail, marked aura.
    /// </summary>
    public static VFXHandle Attach(GameObject prefab, Transform parent)
    {
        if (prefab == null) return new VFXHandle(null);
        var go = Object.Instantiate(prefab, parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        return new VFXHandle(go);
    }
}
