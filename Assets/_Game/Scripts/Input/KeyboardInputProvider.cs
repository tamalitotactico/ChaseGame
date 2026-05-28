using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Proveedor de input desde teclado fisico (WASD / flechas).
/// Usa el nuevo Input System para compatibilidad con Unity 6.
/// </summary>
public class KeyboardInputProvider : MonoBehaviour, IInputProvider
{
    private Vector2 _input;

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            _input = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y += 1f;

        _input = new Vector2(x, y);
        if (_input.sqrMagnitude > 1f)
            _input.Normalize();
    }

    public Vector2 GetMovementInput() => _input;
    public bool IsActive => _input.sqrMagnitude > 0.01f;
}
