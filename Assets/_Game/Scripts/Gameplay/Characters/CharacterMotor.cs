using UnityEngine;

/// <summary>
/// Movimiento fisico del personaje. NO conoce input directo: el Character le
/// pasa una direccion via SetMoveInput() cada frame.
///
/// Soporta impulses (dash, knockback) con duracion fija que sobreescriben el
/// movimiento normal mientras dura.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMotor : MonoBehaviour
{
    [SerializeField]
    float maxSpeed = 4f;

    Rigidbody2D _rb;
    Vector2 _input;
    float _speedMultiplier = 1f;

    // Impulse override
    Vector2 _impulseVel;
    float _impulseRemaining;

    public float SpeedMultiplier
    {
        get => _speedMultiplier;
        set => _speedMultiplier = Mathf.Max(0f, value);
    }
    public float MaxSpeed
    {
        get => maxSpeed;
    }
    public Vector2 Velocity => _rb.linearVelocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Setup(float speed)
    {
        maxSpeed = speed;
    }

    public void SetMoveInput(Vector2 input)
    {
        _input = Vector2.ClampMagnitude(input, 1f);
    }

    /// <summary>Empuja al personaje en una direccion durante 'duration' segundos.</summary>
    public void ApplyImpulse(Vector2 velocity, float duration)
    {
        _impulseVel = velocity;
        _impulseRemaining = Mathf.Max(_impulseRemaining, duration);
    }

    public void Stop()
    {
        _input = Vector2.zero;
        _impulseRemaining = 0f;
        _rb.linearVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (_impulseRemaining > 0f)
        {
            _rb.linearVelocity = _impulseVel;
            _impulseRemaining -= Time.fixedDeltaTime;
            if (_impulseRemaining <= 0f)
                _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            _rb.linearVelocity = _input * (maxSpeed * _speedMultiplier);
        }
    }
}
