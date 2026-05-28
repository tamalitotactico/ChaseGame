using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Combina teclado (WASD) y joystick virtual sin conflictos.
/// Prioridad: ultima fuente que se activo gana. En editor: teclado.
/// En mobile: joystick. Al cambiar de una a otra el cambio es inmediato.
/// </summary>
public class HybridInputManager : MonoBehaviour
{
    [Header("Providers")]
    [Tooltip("Proveedor de teclado (WASD). Se crea en Awake si no se asigna.")]
    [SerializeField] private KeyboardInputProvider keyboardProvider;

    [Tooltip("Joystick virtual (UI). Asignar desde el Canvas.")]
    [SerializeField] private VirtualJoystick joystickProvider;

    // Ultima fuente activa: evita que una fuente residual sobreescriba a la recien activada.
    private IInputProvider _activeProvider;
    private bool _keyboardWasActive;
    private bool _joystickWasActive;

    private void Awake()
    {
        if (keyboardProvider == null)
            keyboardProvider = gameObject.AddComponent<KeyboardInputProvider>();
    }

    private void Update()
    {
        // Lazy auto-find del joystick: el VirtualJoystick vive dentro del HUD
        // (instanciado al cargar la escena, no es referenciable desde el
        // InputManager por orden de carga). Lo buscamos en runtime si el
        // wiring del inspector no esta seteado, y lo cacheamos.
        if (joystickProvider == null)
            joystickProvider = Object.FindAnyObjectByType<VirtualJoystick>();

        bool keyboardNowActive = keyboardProvider != null && keyboardProvider.IsActive;
        bool joystickNowActive = joystickProvider != null && joystickProvider.IsActive;

        // Si una fuente acaba de activarse, toma la prioridad.
        if (keyboardNowActive && !_keyboardWasActive)
            _activeProvider = keyboardProvider;
        else if (joystickNowActive && !_joystickWasActive)
            _activeProvider = joystickProvider;

        // Si la fuente activa deja de tener input, cede paso a la otra si esta activa.
        if (_activeProvider != null && !_activeProvider.IsActive)
        {
            if (keyboardNowActive) _activeProvider = keyboardProvider;
            else if (joystickNowActive) _activeProvider = joystickProvider;
            else _activeProvider = null;
        }

        _keyboardWasActive = keyboardNowActive;
        _joystickWasActive = joystickNowActive;
    }

    /// <summary>Retorna el vector de movimiento de la fuente activa.</summary>
    public Vector2 GetMovementInput()
    {
        Vector2 v = _activeProvider?.GetMovementInput() ?? Vector2.zero;
        MovementTrace.Log("Input", "HIM active={0} move=({1:F2},{2:F2})",
            _activeProvider != null ? _activeProvider.GetType().Name : "null", v.x, v.y);
        return v;
    }

    /// <summary>True si alguna fuente esta produciendo input este frame.</summary>
    public bool IsActive => _activeProvider != null && _activeProvider.IsActive;
}
