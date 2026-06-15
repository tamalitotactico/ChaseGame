using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime del proyectil de Enchant (Charmer). Viaja recto (sin homing) y PERFORA: aplica
/// CharmedEffect a cada enemigo que toca, sin destruirse, hasta alcanzar maxTargets
/// (0 = ilimitado), tocar un muro, o agotar el rango.
///
/// El punto de jale del charm es la posicion del Charmer en el INSTANTE del impacto (se lee
/// owner.transform.position por golpe), no la del cast: por eso se guarda la referencia al owner.
///
/// Prefab requirements: Rigidbody2D (Kinematic) + Collider2D (IsTrigger=true) + CharmProjectile.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CharmProjectile : MonoBehaviour, IWallDestructible
{
    Vector2   _direction;
    float     _speed;
    float     _maxRange;
    float     _traveled;
    int       _maxTargets;
    float     _charmDuration;
    float     _charmPull;
    float     _slowMultiplier;
    Character _owner;
    AudioCue  _sfxOnHit;
    Rigidbody2D _rb;

    readonly HashSet<Character> _hit = new();

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float speed, float range, int maxTargets,
                     float charmDuration, float charmPull, float slowMultiplier, Character owner, AudioCue sfxOnHit = null)
    {
        _direction      = direction.normalized;
        _speed          = speed;
        _maxRange       = range;
        _traveled       = 0f;
        _maxTargets     = maxTargets;
        _charmDuration  = charmDuration;
        _charmPull      = charmPull;
        _slowMultiplier = slowMultiplier;
        _owner          = owner;
        _sfxOnHit       = sfxOnHit;
    }

    void FixedUpdate()
    {
        Vector2 delta = _direction * (_speed * Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + delta);
        _traveled += delta.magnitude;
        if (_traveled >= _maxRange)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner != null && other.gameObject == _owner.gameObject) return;
        // Los muros los maneja ProjectileWallSensor (collider de muro mas chico que el de impacto).

        var c = other.GetComponentInParent<Character>();
        if (c == null || c == _owner) return;
        if (_owner != null && c.Team == _owner.Team) return;
        if (!c.IsAlive) return;
        if (_hit.Contains(c)) return;

        if (c.StatusEffects != null && _owner != null)
        {
            Vector2 charmPoint = _owner.transform.position;
            c.StatusEffects.Apply(new CharmedEffect(_charmDuration, charmPoint, _charmPull));
            if (_slowMultiplier < 1f)
                c.StatusEffects.Apply(new SlowedEffect(_charmDuration, _slowMultiplier));
        }

        _hit.Add(c);
        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_sfxOnHit, transform.position);

        // Perfora: solo se destruye al alcanzar el cupo (si lo hay).
        if (_maxTargets > 0 && _hit.Count >= _maxTargets)
            Destroy(gameObject);
    }

    public void OnWallHit(Vector2 point) => Destroy(gameObject);
}
