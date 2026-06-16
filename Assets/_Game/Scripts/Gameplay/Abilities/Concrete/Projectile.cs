using UnityEngine;

/// <summary>
/// Componente runtime del proyectil. Se mueve cada frame en linea recta hasta
/// agotar el rango o impactar a un IDamageable / pared.
///
/// El prefab debe traer: Rigidbody2D (Kinematic) + Collider2D (Is Trigger) + Projectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour, IWallDestructible
{
    Vector2   _direction;
    float     _speed;
    float     _maxRange;
    float     _traveled;
    int       _damage;
    Character _owner;
    AudioCue  _sfxOnHit;
    Rigidbody2D _rb;
    bool      _initialized;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float speed, float range, int damage, Character owner, AudioCue sfxOnHit = null)
    {
        _direction   = direction.normalized;
        _speed       = speed;
        _maxRange    = range;
        _damage      = damage;
        _owner       = owner;
        _traveled    = 0f;
        _sfxOnHit    = sfxOnHit;
        _initialized = true;
    }

    // Movimiento en FixedUpdate via MovePosition para mantener consistencia con el
    // pipeline de fisica 2D (evita el desfase transform/RB que causa tunneling a
    // alta velocidad y triggers perdidos).
    // _initialized guard: en clientes de red Init() nunca se llama; sin el guard,
    // _maxRange=0 haria que el proyectil se auto-destruyera el primer frame.
    void FixedUpdate()
    {
        if (!_initialized) return;
        Vector2 step = _direction * (_speed * Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + step);
        _traveled += step.magnitude;
        if (_traveled >= _maxRange)
            NetDespawn.Despawn(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
        if (_owner != null && other.gameObject == _owner.gameObject) return;

        if (other.TryGetComponent<IDamageable>(out var d) && d.IsAlive)
        {
            d.TakeDamage(new DamageInfo
            {
                Amount    = _damage,
                Source    = _owner,
                Origin    = transform.position,
                Direction = _direction
            });
            ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_sfxOnHit, transform.position);
            NetDespawn.Despawn(gameObject);
            return;
        }
        // Los muros los maneja ProjectileWallSensor (collider de muro mas chico que el de impacto).
    }

    public void OnWallHit(Vector2 point)
    {
        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_sfxOnHit, point);
        NetDespawn.Despawn(gameObject);
    }
}
