using UnityEngine;

/// <summary>
/// Trampa de oso del Trapper. Armada; al primer HUNTER (enemigo) que entra en triggerRadius lo ATURDE
/// (stun) y revela su posicion a los preys via World Target Pointer por revealDuration, luego desaparece
/// (1 solo uso). Cupo maxTraps.
///
/// El reveal solo se muestra si el jugador LOCAL es del bando del owner (un prey). Tick() publico para test.
///
/// Prefab requirements: SpriteRenderer (visual) + BearTrapPlaceable.
/// </summary>
public class BearTrapPlaceable : Placeable
{
    float _triggerRadius, _stunDuration, _revealDuration, _pointerSize;
    Color _pointerColor = Color.red;
    Sprite _pointerSprite;
    LayerMask _mask;
    bool _sprung;

    static readonly Collider2D[] _buf = new Collider2D[16];

    public void Setup(float triggerRadius, float stunDuration, float revealDuration,
                      Sprite pointerSprite, float pointerSize, Color pointerColor, LayerMask mask)
    {
        _triggerRadius  = triggerRadius;
        _stunDuration   = stunDuration;
        _revealDuration = revealDuration;
        _pointerSprite  = pointerSprite;
        _pointerSize    = pointerSize;
        _pointerColor   = pointerColor;
        _mask           = mask;
    }

    protected override void Update()
    {
        base.Update();
        Tick();
    }

    /// <summary>Dispara la trampa si un hunter esta dentro. Publico para testeo.</summary>
    public void Tick()
    {
        if (_sprung) return;
        var hunter = EnemyInRadius(_triggerRadius);
        if (hunter != null) Spring(hunter);
    }

    void Spring(Character hunter)
    {
        _sprung = true;
        if (hunter.StatusEffects != null)
            hunter.StatusEffects.Apply(new StunnedEffect(_stunDuration));

        // Revelar al hunter a los preys: solo si el jugador local es del bando del owner.
        if (_revealDuration > 0f && ShouldRevealToLocal())
            WorldTargetPointer.Show(hunter.transform, _revealDuration, _pointerSprite, _pointerSize, _pointerColor);

        Destroy(gameObject);
    }

    bool ShouldRevealToLocal()
    {
        var local = PlayerBrain.Local;
        var viewer = local != null ? local.GetComponent<Character>() : null;
        return viewer != null && Owner != null && viewer.Team == Owner.Team;
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
