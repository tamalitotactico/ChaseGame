using UnityEngine;

/// <summary>
/// Indicador de cono sprite (estilo MOBA) para habilidades de area frontal.
/// El angulo del cono esta bakeado en Cone Indicator.png — escala uniforme
/// mantiene las proporciones, agrandar el rango agranda el cono proporcionalmente.
///
/// Setup en prefab:
///   - GameObject root con este componente.
///   - Child SpriteRenderer asignado a 'coneSprite' (Cone Indicator.png, pivot Bottom Center).
///
/// En Tick():
///   - scale = IndicatorRange / sprite.bounds.size.y
///   - Escala uniforme (X e Y) para mantener el angulo del cono.
///   - El root rota hacia direction.
/// </summary>
public class ConeIndicator : AimIndicator
{
    [SerializeField] SpriteRenderer coneSprite;

    AbilityData _data;

    void Reset() => coneSprite = GetComponentInChildren<SpriteRenderer>();

    void Awake()
    {
        if (coneSprite == null) coneSprite = GetComponentInChildren<SpriteRenderer>();
    }

    public override void Begin(Character owner, AbilityData data)
    {
        _data = data;
    }

    public override void Tick(Vector2 origin, Vector2 direction)
    {
        if (coneSprite == null || _data == null) return;

        float range = _data.IndicatorRange;
        float nativeH = coneSprite.sprite != null ? coneSprite.sprite.bounds.size.y : 1f;
        float scale = range / nativeH;

        transform.position = new Vector3(origin.x, origin.y, transform.position.z);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        coneSprite.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
