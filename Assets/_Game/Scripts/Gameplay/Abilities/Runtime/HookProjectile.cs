using UnityEngine;

/// <summary>
/// Runtime del gancho del Drowned. Viaja recto; al primer enemigo que toca lo ATRAE al punto medio
/// entre el cazador (al momento del impacto) y el prey, moviendo SOLO al prey (impulso del Motor
/// hacia el punto medio durante pullDuration), y le aplica un SlowedEffect. Se destruye al impactar,
/// tocar un muro, o agotar el rango.
///
/// Prefab requirements: Rigidbody2D (Kinematic) + Collider2D (IsTrigger=true) + HookProjectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HookProjectile : MonoBehaviour, IWallDestructible
{
    Vector2   _direction;
    float     _speed;
    float     _maxRange;
    float     _traveled;
    float     _pullDuration;
    float     _slowDuration;
    float     _slowMultiplier;
    Character _owner;
    AudioCue  _sfxOnHit;
    Rigidbody2D _rb;
    bool      _initialized;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Init(Vector2 direction, float speed, float range, float pullDuration,
                     float slowDuration, float slowMultiplier, Character owner, AudioCue sfxOnHit = null)
    {
        _direction      = direction.normalized;
        _speed          = speed;
        _maxRange       = range;
        _traveled       = 0f;
        _pullDuration   = Mathf.Max(0.05f, pullDuration);
        _slowDuration   = slowDuration;
        _slowMultiplier = slowMultiplier;
        _owner          = owner;
        _sfxOnHit       = sfxOnHit;
        _initialized    = true;
    }

    void FixedUpdate()
    {
        if (!_initialized) return;
        // Movimiento por velocidad (ver Projectile.cs): MovePosition era lento con la fisica de Fusion.
        _rb.linearVelocity = _direction * _speed;
        _traveled += _speed * Time.fixedDeltaTime;
        if (_traveled >= _maxRange) NetDespawn.Despawn(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;
        if (_owner != null && other.gameObject == _owner.gameObject) return;

        var c = other.GetComponentInParent<Character>();
        if (c == null || c == _owner) return;
        if (_owner != null && c.Team == _owner.Team) return;
        if (!c.IsAlive) return;

        ApplyHit(c);
        NetDespawn.Despawn(gameObject);
    }

    public void OnWallHit(Vector2 point) => NetDespawn.Despawn(gameObject);

    /// <summary>Aplica el efecto del gancho a un prey: lo atrae al punto medio (solo el prey) + slow.
    /// Publico para poder testearlo sin depender de un step de fisica.</summary>
    public void ApplyHit(Character prey)
    {
        if (prey == null) return;

        // Punto medio entre cazador y prey al momento del impacto; solo se mueve el prey.
        if (_owner != null && prey.Motor != null)
        {
            Vector2 preyPos = prey.transform.position;
            Vector2 mid     = ((Vector2)_owner.transform.position + preyPos) * 0.5f;
            Vector2 vel     = (mid - preyPos) / _pullDuration;
            prey.Motor.ApplyImpulse(vel, _pullDuration);
        }
        if (prey.StatusEffects != null)
            prey.StatusEffects.Apply(new SlowedEffect(_slowDuration, _slowMultiplier));

        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_sfxOnHit, transform.position);
    }
}
