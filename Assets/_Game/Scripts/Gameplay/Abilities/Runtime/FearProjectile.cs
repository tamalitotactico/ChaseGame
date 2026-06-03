using UnityEngine;

/// <summary>
/// Runtime del proyectil de miedo (Habilidad 2 del Hunter).
/// Si _homing y _target estan seteados, se curva hacia el target con un cap de giro.
/// Al colisionar con un Character enemigo aplica FearedEffect + SlowedEffect.
/// Se destruye al impactar, alcanzar el rango maximo, o tocar un muro.
///
/// Prefab requirements: Rigidbody2D (Kinematic) + Collider2D (IsTrigger=true) + FearProjectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FearProjectile : MonoBehaviour
{
    Vector2   _direction;
    float     _speed;
    float     _maxRange;
    float     _traveled;
    Character _target;
    bool      _homing;
    float     _turnRateRad;
    float     _fearDuration;
    float     _slowDuration;
    float     _slowMultiplier;
    Character _owner;
    AudioCue  _sfxOnHit;
    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float speed, float range, Character target,
                     bool homing, float turnRateDeg,
                     float fearDuration, float slowDuration, float slowMultiplier,
                     Character owner, AudioCue sfxOnHit = null)
    {
        _direction      = direction.normalized;
        _speed          = speed;
        _maxRange       = range;
        _traveled       = 0f;
        _target         = target;
        _homing         = homing;
        _turnRateRad    = turnRateDeg * Mathf.Deg2Rad;
        _fearDuration   = fearDuration;
        _slowDuration   = slowDuration;
        _slowMultiplier = slowMultiplier;
        _owner          = owner;
        _sfxOnHit       = sfxOnHit;
    }

    // Movimiento + homing en FixedUpdate via MovePosition (consistencia con fisica 2D).
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Homing: curvar hacia target si esta vivo y existe
        if (_homing && _target != null && _target.IsAlive)
        {
            Vector2 toTarget = ((Vector2)_target.transform.position - _rb.position);
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector2 desiredDir = toTarget.normalized;
                float deltaAngleRad = Mathf.Deg2Rad * Vector2.SignedAngle(_direction, desiredDir);
                float maxStep       = _turnRateRad * dt;
                float step          = Mathf.Clamp(deltaAngleRad, -maxStep, maxStep);
                _direction = Rotate(_direction, step);
                transform.rotation = Quaternion.FromToRotation(Vector3.right, _direction);
            }
        }

        Vector2 delta = _direction * (_speed * dt);
        _rb.MovePosition(_rb.position + delta);
        _traveled += delta.magnitude;
        if (_traveled >= _maxRange)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner != null && other.gameObject == _owner.gameObject) return;

        // Wall layer: destruir
        if (other.gameObject.layer == GameLayers.Wall)
        {
            Destroy(gameObject);
            return;
        }

        var c = other.GetComponentInParent<Character>();
        if (c == null || c == _owner) return;
        if (_owner != null && c.Team == _owner.Team) return;
        if (!c.IsAlive) return;

        if (c.StatusEffects != null)
        {
            Vector2 flee = ((Vector2)c.transform.position - (Vector2)transform.position).normalized;
            c.StatusEffects.Apply(new FearedEffect(_fearDuration, flee));
            c.StatusEffects.Apply(new SlowedEffect(_slowDuration, _slowMultiplier));
        }

        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_sfxOnHit, transform.position);
        Destroy(gameObject);
    }

    static Vector2 Rotate(Vector2 v, float rad)
    {
        float s = Mathf.Sin(rad), co = Mathf.Cos(rad);
        return new Vector2(v.x * co - v.y * s, v.x * s + v.y * co);
    }
}
