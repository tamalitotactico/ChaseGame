using UnityEngine;

/// <summary>
/// Indicador de flecha sprite (estilo MOBA) para habilidades direccionales:
/// proyectiles, dash, cualquier habilidad con rango lineal.
///
/// Setup en prefab:
///   - GameObject root con este componente.
///   - Child SpriteRenderer asignado a 'arrowSprite' (Arrow Indicator.png, pivot Bottom Center).
///
/// En Tick():
///   - El root se posiciona en el caster (origin) y rota hacia direction.
///   - La escala Y de arrowSprite = IndicatorRange / sprite.bounds.size.y
///     (normaliza el sprite a 1 unidad = 1 unidad de mundo, luego escala al rango).
///   - La escala X = arrowWidth / sprite.bounds.size.x (ancho configurable en world units).
///
/// El rango se lee de AbilityData.IndicatorRange cada frame (live, sin cachear).
/// detectWalls: si true, el raycast recorta la flecha al primer muro en la trayectoria.
/// </summary>
public class ArrowIndicator : AimIndicator
{
    [SerializeField] SpriteRenderer arrowSprite;
    [Tooltip("Unidades a recortar en la punta de la flecha. Positivo = punta mas corta (corrige padding transparente en el sprite). Negativo = punta mas larga.")]
    [SerializeField] float tipTrim = 0f;
    [Tooltip("Si true, la flecha se detiene en el primer muro en la trayectoria.")]
    [SerializeField] bool detectWalls;
    [Tooltip("Layers de muro. Se auto-resuelve al layer 'Wall' si se deja en Everything.")]
    [SerializeField] LayerMask wallMask = ~0;

    AbilityData _data;

    void Reset() => arrowSprite = GetComponentInChildren<SpriteRenderer>();

    void Awake()
    {
        if (arrowSprite == null) arrowSprite = GetComponentInChildren<SpriteRenderer>();
        if (detectWalls && wallMask.value == -1)
            wallMask = GameLayers.WallMask;
    }

    public override void Begin(Character owner, AbilityData data)
    {
        _data = data;
    }

    public override void Tick(Vector2 origin, Vector2 direction)
    {
        if (arrowSprite == null || _data == null) return;

        float range = _data.IndicatorRange;
        Vector2 dir = direction.normalized;

        if (detectWalls)
        {
            var hit = Physics2D.Raycast(origin, dir, range, wallMask);
            if (hit.collider != null) range = Mathf.Max(0f, hit.distance - 0.1f);
        }

        float nativeH = arrowSprite.sprite != null ? arrowSprite.sprite.bounds.size.y : 1f;
        float nativeW = arrowSprite.sprite != null ? arrowSprite.sprite.bounds.size.x : 1f;

        // tipTrim recorta el padding transparente del sprite para que la punta
        // quede exactamente en el limite del rango.
        float visualLength = Mathf.Max(0f, range - tipTrim);
        float width = _data.IndicatorWidth;

        transform.position = new Vector3(origin.x, origin.y, transform.position.z);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        arrowSprite.transform.localScale = new Vector3(width / nativeW, visualLength / nativeH, 1f);
    }
}
