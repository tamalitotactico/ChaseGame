using UnityEngine;

public class RemnantDecoy : MonoBehaviour
{
    [Header("Activation")]
    [SerializeField]
    float _activationRadius = 1.5f;

    [Header("Effect")]
    [SerializeField]
    float _effectRadius = 4f;

    [SerializeField]
    float _fearDuration = 1.5f;

    [SerializeField]
    float _slowDuration = 2f;

    [SerializeField]
    float _slowMultiplier = 0.5f;

    [SerializeField]
    float _duration = 5f;

    [SerializeField]
    float _activeDuration = 1f;

    Character _owner;

    float _timer;

    bool _initialized;
    bool _triggered;

    [SerializeField]
    LayerMask _preyLayer;

    static readonly Collider2D[] _hitBuffer = new Collider2D[16];

    public void Setup(
        float duration,
        float activationRadius,
        float effectRadius,
        float fearDuration,
        float slowDuration,
        float slowMultiplier,
        float activeDuration,
        Character owner,
        LayerMask preyLayer
    )
    {
        _duration = duration;
        _activationRadius = activationRadius;
        _effectRadius = effectRadius;
        _fearDuration = fearDuration;
        _slowDuration = slowDuration;
        _slowMultiplier = slowMultiplier;
        _activeDuration = activeDuration;
        _owner = owner;
        _preyLayer = preyLayer;
        _timer = _duration;
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized)
            return;

        _timer -= Time.deltaTime;

        if (_timer <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (_triggered)
            return;

        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            _activationRadius,
            _hitBuffer,
            _preyLayer
        );

        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];

            if (col == null)
                continue;

            var c = col.GetComponentInParent<Character>();

            if (c == null)
                continue;

            if (c == _owner)
                continue;

            if (!c.IsAlive)
                continue;

            TriggerTrap();

            return;
        }
    }

    void TriggerTrap()
    {
        _triggered = true;

        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            _effectRadius,
            _hitBuffer,
            _preyLayer
        );

        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];

            if (col == null)
                continue;

            var c = col.GetComponentInParent<Character>();

            if (c == null)
                continue;

            if (c == _owner)
                continue;

            if (!c.IsAlive)
                continue;

            if (c.StatusEffects == null)
                continue;

            Vector2 flee = ((Vector2)c.transform.position - (Vector2)transform.position).normalized;

            c.StatusEffects.Apply(new FearedEffect(_fearDuration, flee));
            c.StatusEffects.Apply(new SlowedEffect(_slowDuration, _slowMultiplier));
        }

        _timer = _activeDuration;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _activationRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _effectRadius);
    }
}
