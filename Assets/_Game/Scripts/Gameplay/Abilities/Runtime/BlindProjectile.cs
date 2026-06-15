using UnityEngine;

/// <summary>
/// Proyectil del "Disparo cegador" (TrickyWizard). Viaja recto; al primer enemigo aplica BlindedEffect
/// (reduce FOV) + SlowedEffect. Se destruye al impactar, tocar muro, o agotar el rango.
///
/// Prefab requirements: Rigidbody2D (Kinematic) + Collider2D (IsTrigger=true) + BlindProjectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BlindProjectile : MonoBehaviour, IWallDestructible
{
    Vector2   _dir;
    float     _speed, _maxRange, _traveled;
    float     _fovMultiplier, _fovDuration, _slowDuration, _slowMultiplier;
    Character _owner;
    AudioCue  _sfxOnHit;
    Rigidbody2D _rb;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Init(Vector2 dir, float speed, float range, float fovMultiplier, float fovDuration,
                     float slowDuration, float slowMultiplier, Character owner, AudioCue sfxOnHit = null)
    {
        _dir            = dir.normalized;
        _speed          = speed;
        _maxRange       = range;
        _traveled       = 0f;
        _fovMultiplier  = fovMultiplier;
        _fovDuration    = fovDuration;
        _slowDuration   = slowDuration;
        _slowMultiplier = slowMultiplier;
        _owner          = owner;
        _sfxOnHit       = sfxOnHit;
    }

    void FixedUpdate()
    {
        Vector2 delta = _dir * (_speed * Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + delta);
        _traveled += delta.magnitude;
        if (_traveled >= _maxRange) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner != null && other.gameObject == _owner.gameObject) return;
        // Los muros los maneja ProjectileWallSensor (collider de muro mas chico que el de impacto).

        var c = other.GetComponentInParent<Character>();
        if (c == null || c == _owner) return;
        if (_owner != null && c.Team == _owner.Team) return;
        if (!c.IsAlive) return;

        if (c.StatusEffects != null)
        {
            c.StatusEffects.Apply(new BlindedEffect(_fovDuration, _fovMultiplier));
            c.StatusEffects.Apply(new SlowedEffect(_slowDuration, _slowMultiplier));
        }
        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_sfxOnHit, transform.position);
        Destroy(gameObject);
    }

    public void OnWallHit(Vector2 point) => Destroy(gameObject);
}
