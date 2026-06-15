using UnityEngine;

/// <summary>
/// Baliza del Engineer: aura que da haste a los aliados (preys, incluido el dueno) dentro de areaRadius.
/// El haste se aplica con duracion = exitBoostDuration y se refresca cada frame mientras esten dentro,
/// asi que al salir (o al expirar la baliza) el boost dura exitBoostDuration mas. El hunter la rompe de
/// 1 golpe (IDamageable; ignora dano aliado).
///
/// Prefab requirements: Collider2D (trigger, para que el ataque del hunter la alcance) + BeaconPlaceable.
/// </summary>
public class BeaconPlaceable : Placeable, IDamageable
{
    float _radius, _haste, _exitBoost;
    int   _health = 1;

    public bool IsAlive     => true;
    public bool IsTargetable => true;

    /// <summary>Vida restante (para testeo/HUD).</summary>
    public int Health => _health;

    public void SetupBeacon(float radius, float hasteMultiplier, float exitBoostDuration, int health)
    {
        _radius    = radius;
        _haste     = hasteMultiplier;
        _exitBoost = Mathf.Max(0.05f, exitBoostDuration);
        _health    = Mathf.Max(1, health);
    }

    public void TakeDamage(in DamageInfo info)
    {
        // Solo el enemigo (hunter) la rompe; ignorar dano del propio bando.
        if (info.Source != null && Owner != null && info.Source.Team == Owner.Team) return;
        _health--;
        if (_health <= 0) Destroy(gameObject);
    }

    protected override void Update()
    {
        base.Update();
        ApplyAura();
    }

    /// <summary>Da haste a los aliados dentro del radio (refresca). Publico para testeo.</summary>
    public void ApplyAura()
    {
        var world = ServiceLocator.Resolve<IWorldQueryService>();
        if (world == null || Owner == null) return;

        var allies = world.GetAlliesOf(Owner.Team);
        Vector2 pos = transform.position;
        float r2 = _radius * _radius;
        for (int i = 0; i < allies.Count; i++)
        {
            var a = allies[i];
            if (a == null || !a.IsAlive || a.StatusEffects == null) continue;
            if (((Vector2)a.transform.position - pos).sqrMagnitude > r2) continue;
            a.StatusEffects.Apply(new HastedEffect(_exitBoost, _haste));
        }
    }
}
