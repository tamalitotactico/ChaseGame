using UnityEngine;
#if ASTAR_EXISTS
using Pathfinding;
#endif

/// <summary>
/// Dev-only (escena de preview): garantiza un GridGraph 2D que marca los muros (layer Wall) como
/// no-caminables, para que Smell y el Ghost Wolf naveguen con A* rodeando muros tambien aca (la
/// escena de preview no trae grafo horneado).
///
/// La creacion/configuracion del grafo se hace en Start (play), porque AstarPath.data solo esta
/// inicializado en runtime (en edit mode lanza NRE). Re-escanea con Rescan() si agregas muros en play.
/// </summary>
#if ASTAR_EXISTS
[RequireComponent(typeof(AstarPath))]
#endif
public class PreviewAstarGraph : MonoBehaviour
{
    [Tooltip("Tamano de nodo (u). Menor = mas preciso, mas lento.")]
    public float nodeSize = 0.4f;
    [Tooltip("Nodos a lo ancho / alto (cubre width*nodeSize x depth*nodeSize unidades).")]
    public int width = 64, depth = 48;
    public Vector2 center = Vector2.zero;

#if ASTAR_EXISTS
    void Start() => Rescan();

    /// <summary>Configura (si falta) y escanea el grafo. Llamar tras agregar muros en play.</summary>
    public void Rescan()
    {
        var astar = AstarPath.active != null ? AstarPath.active : GetComponent<AstarPath>();
        if (astar == null) return;

        GridGraph grid = (astar.data.graphs != null && astar.data.graphs.Length > 0)
            ? astar.data.graphs[0] as GridGraph
            : null;
        if (grid == null)
            grid = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;

        grid.center   = new Vector3(center.x, center.y, 0f);
        grid.rotation = new Vector3(-90f, 0f, 0f); // plano XY (2D)
        grid.SetDimensions(width, depth, nodeSize);
        grid.collision.use2D          = true;
        grid.collision.collisionCheck = true;
        grid.collision.heightCheck    = false;
        grid.collision.type           = ColliderType.Sphere; // "Circle" en 2D
        grid.collision.diameter       = 1.2f;
        grid.collision.mask           = GameLayers.WallMask;

        // SIN erosion: inflar muros uniformemente vuelve incaminables los pasillos angostos
        // (1 nodo de ancho). El clearance de esquinas se maneja en runtime con el steering
        // de evasion de muros en BotLocomotion.GetSteeringDirection().
        grid.erodeIterations = 0;

        astar.Scan();
    }
#endif
}
