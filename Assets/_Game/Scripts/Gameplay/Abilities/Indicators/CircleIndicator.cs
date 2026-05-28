using UnityEngine;

/// <summary>
/// Indicador circular (AoE alrededor del caster o en posicion fija).
/// Usa un SpriteRenderer con sprite circulo y lo escala a IndicatorRadius*2.
///
/// Setup en prefab:
///   - GameObject con SpriteRenderer asignado a 'circle'.
///   - Sprite: Range Indicator.png (anillo de rango) o Heal Indicator.png (AoE relleno).
///   - Pivot del sprite: Center.
///
/// El radio se lee de AbilityData.IndicatorRadius cada Tick (live, sin cachear).
/// Si la AbilityData concreta sobreescribe IndicatorRadius (ej. RemnantAbilityData
/// retorna effectRadius), el indicador refleja el valor real de gameplay.
/// </summary>
public class CircleIndicator : AimIndicator
{
    [SerializeField] SpriteRenderer circle;
    [Tooltip("True: el indicador sigue al caster cada frame. False: queda donde Begin() lo posiciono.")]
    [SerializeField] bool followCaster = true;

    AbilityData _data;

    void Reset() => circle = GetComponentInChildren<SpriteRenderer>();

    void Awake()
    {
        if (circle == null) circle = GetComponentInChildren<SpriteRenderer>();
    }

    public override void Begin(Character owner, AbilityData data)
    {
        _data = data;
    }

    public override void Tick(Vector2 origin, Vector2 direction)
    {
        float radius = _data != null ? _data.IndicatorRadius : 1.5f;
        float diameter = radius * 2f;
        if (circle != null)
        {
            float nativeD = circle.sprite != null ? circle.sprite.bounds.size.x : 1f;
            float s = diameter / nativeD;
            circle.transform.localScale = new Vector3(s, s, 1f);
        }

        if (followCaster)
            transform.position = new Vector3(origin.x, origin.y, transform.position.z);
    }
}
