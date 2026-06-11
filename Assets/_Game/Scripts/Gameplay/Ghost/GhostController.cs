using UnityEngine;

/// <summary>
/// Controlador local del fantasma del jugador downed. Lee el MISMO HybridInputManager que PlayerBrain
/// y mueve el transform directamente (sin Rigidbody/Collider): atraviesa paredes por construccion.
/// Sin ataque ni habilidades.
///
/// El fantasma es una ENTIDAD de primera clase (no un truco de camara): expone OwnerCharacter para
/// que en multiplayer sea spawnable como NetworkObject con input authority al dueño. Hoy es local-only:
/// lo crea/destruye GhostModeController.
/// </summary>
[DisallowMultipleComponent]
public class GhostController : MonoBehaviour
{
    /// <summary>El cuerpo downed al que pertenece este fantasma (ancla de revive).</summary>
    public Character OwnerCharacter { get; private set; }

    float _speed = 5f;
    HybridInputManager _input;

    public void Configure(float speed, Character owner, HybridInputManager input)
    {
        _speed = Mathf.Max(0.1f, speed);
        OwnerCharacter = owner;
        _input = input != null ? input : Object.FindAnyObjectByType<HybridInputManager>();
    }

    void Update()
    {
        if (_input == null)
        {
            _input = Object.FindAnyObjectByType<HybridInputManager>();
            if (_input == null) return;
        }

        Vector2 move = _input.GetMovementInput();
        if (move.sqrMagnitude > 1f) move = move.normalized;
        transform.position += (Vector3)(move * (_speed * Time.deltaTime));
    }
}
