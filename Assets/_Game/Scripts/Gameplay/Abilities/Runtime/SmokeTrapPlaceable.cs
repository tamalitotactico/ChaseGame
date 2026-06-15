using UnityEngine;

/// <summary>
/// Trampa de humo del Trapper. Armada y visible; solo la dispara un HUNTER (enemigo del owner) que
/// entra en triggerRadius. Al dispararse, durante areaDuration aplica BlindedEffect (reduce vision) a
/// los hunters dentro de areaRadius, y luego desaparece.
///
/// Deteccion por OverlapCircle con filtro por team (mascara amplia). Tick() es publico para testeo.
///
/// Prefab requirements: SpriteRenderer (visual) + SmokeTrapPlaceable.
/// </summary>
public class SmokeTrapPlaceable : Placeable
{
    float _triggerRadius, _areaRadius, _areaDuration, _fovMultiplier, _blindRefresh;
    LayerMask _mask;
    bool _triggered;

    static readonly Collider2D[] _buf = new Collider2D[16];

    public void Setup(float triggerRadius, float areaRadius, float areaDuration,
                      float fovMultiplier, float blindRefresh, LayerMask mask)
    {
        _triggerRadius = triggerRadius;
        _areaRadius    = areaRadius;
        _areaDuration  = areaDuration;
        _fovMultiplier = fovMultiplier;
        _blindRefresh  = Mathf.Max(0.05f, blindRefresh);
        _mask          = mask;
    }

    protected override void Update()
    {
        base.Update();
        Tick();
    }

    /// <summary>Arma/dispara la trampa y aplica el cegado. Publico para testeo.</summary>
    public void Tick()
    {
        if (!_triggered)
        {
            if (EnemyInRadius(_triggerRadius) != null)
            {
                _triggered = true;
                SetLifetime(_areaDuration); // a partir de aca vive areaDuration
            }
            return;
        }

        // Humo activo: cegar a los hunters dentro del area (refresca el efecto).
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_mask);
        int n = Physics2D.OverlapCircle(transform.position, _areaRadius, filter, _buf);
        for (int i = 0; i < n; i++)
        {
            var c = _buf[i] != null ? _buf[i].GetComponentInParent<Character>() : null;
            if (c == null || !c.IsAlive) continue;
            if (Owner != null && c.Team == Owner.Team) continue; // solo enemigos (hunters)
            if (c.StatusEffects != null)
                c.StatusEffects.Apply(new BlindedEffect(_blindRefresh, _fovMultiplier));
        }
    }

    Character EnemyInRadius(float r)
    {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_mask);
        int n = Physics2D.OverlapCircle(transform.position, r, filter, _buf);
        for (int i = 0; i < n; i++)
        {
            var c = _buf[i] != null ? _buf[i].GetComponentInParent<Character>() : null;
            if (c == null || !c.IsAlive) continue;
            if (Owner != null && c.Team == Owner.Team) continue;
            return c;
        }
        return null;
    }
}
