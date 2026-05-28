using UnityEngine;

/// <summary>
/// Componente runtime del proyectil. Se mueve cada frame en linea recta hasta
/// agotar el rango o impactar a un IDamageable / pared.
///
/// El prefab debe traer: Rigidbody2D (Kinematic) + Collider2D (Is Trigger) + Projectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    Vector2   _direction;
    float     _speed;
    float     _maxRange;
    float     _traveled;
    int       _damage;
    Character _owner;
    AudioCue  _sfxOnHit;

    public void Init(Vector2 direction, float speed, float range, int damage, Character owner, AudioCue sfxOnHit = null)
    {
        _direction = direction.normalized;
        _speed     = speed;
        _maxRange  = range;
        _damage    = damage;
        _owner     = owner;
        _traveled  = 0f;
        _sfxOnHit  = sfxOnHit;
    }

    void Update()
    {
        Vector2 step = _direction * (_speed * Time.deltaTime);
        transform.position += (Vector3)step;
        _traveled += step.magnitude;
        if (_traveled >= _maxRange)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
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
            Destroy(gameObject);
            return;
        }

        // Wall layer: destruir
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_sfxOnHit, transform.position);
            Destroy(gameObject);
        }
    }
}
