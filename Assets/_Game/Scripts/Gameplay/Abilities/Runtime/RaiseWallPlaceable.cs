using UnityEngine;

/// <summary>
/// Muro del Engineer: una barra (NO bloquea, sin collider solido) que RELENTIZA a los hunters que la
/// cruzan; los preys pasan libres. Se coloca en la posición del caster y se extiende entre dos muros
/// cercanos detectados por raycast en 8 direcciones cardinales. Elige la pareja de muros cuya distancia
/// total es más corta.
///
/// El slow se aplica mientras el hunter esta dentro (se refresca cada frame con slowDuration corto).
/// Deteccion por OverlapBox (no requiere collider). Visual via SpriteRenderer.size (drawMode Sliced).
///
/// Prefab requirements: SpriteRenderer + RaiseWallPlaceable.
/// </summary>
public class RaiseWallPlaceable : Placeable
{
    Vector2 _along;     // eje a lo largo del muro
    float   _length;
    float   _width;
    float   _slowMult, _slowDur, _hitLifetime;
    LayerMask _hunterLayer;
    bool    _touched;

    SpriteRenderer _sr;
    static readonly Collider2D[] _buf = new Collider2D[16];

    public void Setup(Vector2 origin, float maxLength, float width, float slowMult, float slowDur,
                      float hitLifetime, LayerMask wallLayer, LayerMask hunterLayer)
    {
        _width       = Mathf.Max(0.05f, width);
        _slowMult    = slowMult;
        _slowDur     = Mathf.Max(0.05f, slowDur);
        _hitLifetime = hitLifetime;
        _hunterLayer = hunterLayer;

        // Detectar muros en 8 direcciones cardinales y seleccionar la mejor pareja.
        var (dir1, dist1, dir2, dist2) = FindBestWallPair(origin, maxLength * 0.5f, wallLayer);

        // Calcular puntos de conexion
        Vector2 endA = origin + dir1 * dist1;
        Vector2 endB = origin + dir2 * dist2;
        Vector2 mid  = (endA + endB) * 0.5f;
        _along = (endB - endA).normalized;
        _length = (endB - endA).magnitude;

        // Posicionar y rotar el muro
        transform.position = new Vector3(mid.x, mid.y, transform.position.z);
        float angle = Mathf.Atan2(_along.y, _along.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        _sr = GetComponent<SpriteRenderer>();
        var size = new Vector2(Mathf.Max(0.1f, _length), _width);
        if (_sr != null)
        {
            _sr.drawMode = SpriteDrawMode.Sliced;
            _sr.size     = size;
        }

#if FUSION2
        // En red, replicar la geometria calculada (pos/rot/size) a los clientes; sin esto el cliente
        // ve el prefab base ("un punto"). Inerte en Solo (Object null) -> el size de arriba ya basta.
        var netVis = GetComponent<NetworkedPlaceableVisual>();
        if (netVis != null) netVis.HostSet(mid, angle, size);
#endif
    }

    // Raycast en 8 direcciones y retorna la pareja con distancia total minima.
    (Vector2 dir1, float dist1, Vector2 dir2, float dist2) FindBestWallPair(Vector2 origin, float maxDist, LayerMask wallLayer)
    {
        // 8 direcciones cardinales y diagonales
        Vector2[] directions = new Vector2[]
        {
            Vector2.right,
            Vector2.up,
            Vector2.left,
            Vector2.down,
            (Vector2.right + Vector2.up).normalized,
            (Vector2.left + Vector2.up).normalized,
            (Vector2.left + Vector2.down).normalized,
            (Vector2.right + Vector2.down).normalized
        };

        float[] distances = new float[8];

        // Raycast en cada direccion
        for (int i = 0; i < 8; i++)
        {
            var hit = Physics2D.Raycast(origin, directions[i], maxDist, wallLayer);
            distances[i] = hit.collider != null ? hit.distance : maxDist;
        }

        // Seleccionar la pareja opuesta con menor distancia total
        // Pares opuestos: (0,2), (1,3), (4,6), (5,7) = (der,izq), (arriba,abajo), (diagonal-UR,diagonal-DL), (diagonal-UL,diagonal-DR)
        int[] pairs = { 0, 4, 8, 12 }; // indices de pares
        float minTotal = float.MaxValue;
        int bestPair = 0;

        for (int p = 0; p < 4; p++)
        {
            int i = p * 2;
            int j = (i + 2) % 8;
            float total = distances[i] + distances[j];
            if (total < minTotal)
            {
                minTotal = total;
                bestPair = i;
            }
        }

        int dir1Idx = bestPair;
        int dir2Idx = (bestPair + 2) % 8;

        return (directions[dir1Idx], distances[dir1Idx], directions[dir2Idx], distances[dir2Idx]);
    }

    protected override void Update()
    {
        base.Update();
        ScanAndSlow();
    }

    /// <summary>Relentiza a los hunters dentro de la barra (refresca el slow). Publico para testeo.</summary>
    public void ScanAndSlow()
    {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_hunterLayer);
        int n = Physics2D.OverlapBox(transform.position, new Vector2(_length, _width),
                                     transform.eulerAngles.z, filter, _buf);
        for (int i = 0; i < n; i++)
        {
            var c = _buf[i] != null ? _buf[i].GetComponentInParent<Character>() : null;
            if (c == null || !c.IsAlive) continue;
            if (Owner != null && c.Team == Owner.Team) continue; // solo enemigos (hunters)

            if (c.StatusEffects != null)
                c.StatusEffects.Apply(new SlowedEffect(_slowDur, _slowMult));

            if (!_touched) { _touched = true; SetLifetime(_hitLifetime); }
        }
    }
}
