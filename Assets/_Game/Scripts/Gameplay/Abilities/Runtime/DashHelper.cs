using UnityEngine;

/// <summary>
/// Helper compartido para "dash/teletransporte a un punto del mundo respetando muros".
/// Generaliza la parte comun de TeleportSmash: raycast hacia el destino, aterrizar justo
/// antes del muro si hay uno en el camino, mover al owner y detener su motor.
///
/// Lo usan Assault (Charmer, salta al enemigo mas cercano en vision) y, a futuro,
/// Ejecucion (Drowned, salta al punto apuntado). El VFX/SFX de llegada lo maneja cada
/// habilidad por separado, porque difieren.
///
/// Seam Fusion: en red el movimiento del owner sera autoritativo; este helper queda como
/// la primitiva local de "mover a destino respetando geometria".
/// </summary>
public static class DashHelper
{
    /// <summary>
    /// Mueve al owner hacia 'destination' en linea recta; si un muro (wallLayer) se cruza,
    /// aterriza a 'wallPadding' antes del impacto. Detiene el motor. Devuelve la posicion
    /// real de aterrizaje.
    /// </summary>
    public static Vector2 DashTo(Character owner, Vector2 destination, LayerMask wallLayer, float wallPadding)
    {
        if (owner == null) return destination;

        var t = owner.transform;
        Vector2 from  = t.position;
        Vector2 delta = destination - from;
        float   dist  = delta.magnitude;

        Vector2 landing = destination;
        if (dist > 0.001f)
        {
            Vector2 dir = delta / dist;
            var hit = Physics2D.Raycast(from, dir, dist, wallLayer);
            if (hit.collider != null)
                landing = hit.point - dir * wallPadding;
        }

        t.position = new Vector3(landing.x, landing.y, t.position.z);
        if (owner.Motor != null) owner.Motor.Stop();
        return landing;
    }
}
