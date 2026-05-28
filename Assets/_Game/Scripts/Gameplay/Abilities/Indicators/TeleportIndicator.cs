using UnityEngine;

/// <summary>
/// Indicador compuesto para TeleportSmash: flecha sprite desde el caster hasta
/// el punto de aterrizaje + circulo AoE en el aterrizaje.
///
/// Considera walls: Physics2D.Raycast con wallMask para no proyectar mas alla
/// de un muro intermedio.
///
/// Setup en prefab:
///   - GameObject root con este componente.
///   - Child SpriteRenderer asignado a 'arrowSprite' (Arrow Indicator.png, pivot Bottom Center).
///   - Child SpriteRenderer asignado a 'aoeCircle' (Heal Indicator.png, pivot Center).
///
/// Los valores se leen de AbilityData.IndicatorRange / IndicatorRadius cada Tick.
/// TeleportSmashAbilityData sobreescribe ambas propiedades con teleportDistance y aoeRadius.
/// </summary>
public class TeleportIndicator : AimIndicator
{
    [SerializeField] SpriteRenderer arrowSprite;
    [SerializeField] SpriteRenderer aoeCircle;
    [Tooltip("Unidades a recortar en la punta de la flecha (corrige padding transparente del sprite).")]
    [SerializeField] float tipTrim = 0f;
    [Tooltip("Layers que bloquean el TP. Se auto-resuelve al layer 'Wall' si se deja en Everything.")]
    [SerializeField] LayerMask wallMask = ~0;

    AbilityData _data;

    void Awake()
    {
        if (wallMask.value == -1)
        {
            int wall = LayerMask.NameToLayer("Wall");
            wallMask = wall >= 0 ? (1 << wall) : 0;
        }
    }

    public override void Begin(Character owner, AbilityData data)
    {
        _data = data;
    }

    public override void Tick(Vector2 origin, Vector2 direction)
    {
        float maxDist = _data != null ? _data.IndicatorRange  : 6f;
        float radius  = _data != null ? _data.IndicatorRadius : 2f;

        Vector2 dir = direction.normalized;
        var hit = Physics2D.Raycast(origin, dir, maxDist, wallMask);
        Vector2 landing = hit.collider != null ? hit.point - dir * 0.1f : origin + dir * maxDist;

        if (arrowSprite != null)
        {
            float dist = Mathf.Max(0f, Vector2.Distance(origin, landing) - tipTrim);
            float nativeH = arrowSprite.sprite != null ? arrowSprite.sprite.bounds.size.y : 1f;
            float nativeW = arrowSprite.sprite != null ? arrowSprite.sprite.bounds.size.x : 1f;
            float width = _data != null ? _data.IndicatorWidth : 1f;
            arrowSprite.transform.position = new Vector3(origin.x, origin.y, arrowSprite.transform.position.z);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            arrowSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            arrowSprite.transform.localScale = new Vector3(width / nativeW, dist / nativeH, 1f);
        }

        if (aoeCircle != null)
        {
            float diameter = radius * 2f;
            float nativeD = aoeCircle.sprite != null ? aoeCircle.sprite.bounds.size.x : 1f;
            float s = diameter / nativeD;
            aoeCircle.transform.position = new Vector3(landing.x, landing.y, aoeCircle.transform.position.z);
            aoeCircle.transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
