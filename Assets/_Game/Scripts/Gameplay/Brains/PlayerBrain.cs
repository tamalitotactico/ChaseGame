using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Brain del jugador local. Combina:
///   - HybridInputManager para movimiento (WASD + joystick virtual).
///   - Keyboard.current para teclas de abilities (Q/E/R) y attack (Space).
///   - Hooks publicos OnAbilityButton*/OnAttackButton para botones tactiles del HUD.
///
/// Phase 3: este brain corre solo en el cliente local; un RemoteBrain leera el
/// NetworkInput sincronizado para los demas jugadores.
/// </summary>
public class PlayerBrain : MonoBehaviour, IBrain
{
    [Header("Refs")]
    [SerializeField] HybridInputManager inputManager;

    [Header("Keyboard bindings")]
    [SerializeField] Key slot0Key = Key.Q;
    [SerializeField] Key slot1Key = Key.E;
    [SerializeField] Key slot2Key = Key.R;
    [SerializeField] Key attackKey = Key.Space;

    // Estado por slot: separamos teclado (poll cada frame) y UI (event-driven)
    bool[] _kbHeld = new bool[3];
    bool[] _uiHeld = new bool[3];
    bool[] _heldPrev = new bool[3];
    bool _attackQueued; // true si UI o teclado lo activo este frame
    Vector2 _uiAim;     // drag-aim del boton tactil; cero = sin drag activo

    /// <summary>
    /// Referencia retenida al PlayerBrain local activo. La UI (botones de habilidad/ataque) la usa
    /// para BINDEARSE al re-activarse aunque haya perdido el CharacterSpawnedEvent (ej. un slot que
    /// AbilityHUD prende/apaga por rol, o el boton de ataque). Sin esto, un binder que estaba inactivo
    /// cuando se publico el spawn nunca se enteraba del jugador -> habilidad/boton muertos desde la 2da
    /// partida. Es null entre partidas (el cuerpo se destruye y limpia la ref).
    /// </summary>
    public static PlayerBrain Local { get; private set; }

    void Awake()
    {
        if (inputManager == null) inputManager = FindAnyObjectByType<HybridInputManager>();
    }

    void OnEnable()  => Local = this;
    void OnDisable() { if (Local == this) Local = null; }

    public BrainIntent CaptureIntent()
    {
        // Teclado
        var kb = Keyboard.current;
        if (kb != null)
        {
            _kbHeld[0] = kb[slot0Key].isPressed;
            _kbHeld[1] = kb[slot1Key].isPressed;
            _kbHeld[2] = kb[slot2Key].isPressed;
            if (kb[attackKey].wasPressedThisFrame) _attackQueued = true;
        }
        else
        {
            for (int i = 0; i < 3; i++) _kbHeld[i] = false;
        }

        Vector2 move = inputManager != null ? inputManager.GetMovementInput() : Vector2.zero;

        // AimInput: si la UI escribio un drag-aim, gana sobre el movimiento.
        // Sin drag, cae a MoveInput (preserva auto-aim implicito en abilities
        // que leen AimInput).
        Vector2 aim = _uiAim.sqrMagnitude > 0.01f ? _uiAim : move;

        var intent = new BrainIntent
        {
            MoveInput     = move,
            AimInput      = aim,
            AttackPressed = _attackQueued,
            Slot0 = ComputeSlot(0),
            Slot1 = ComputeSlot(1),
            Slot2 = ComputeSlot(2)
        };

        // Avanzar al siguiente frame
        for (int i = 0; i < 3; i++) _heldPrev[i] = IsHeld(i);
        _attackQueued = false;

        // Si ningun slot esta presionado, limpiar _uiAim para que el siguiente
        // frame no arrastre aim residual. Importante: el clear ocurre DESPUES
        // de construir el intent, de modo que el frame del Released aun ve la
        // ultima direccion de aim (necesario para que el Aimer distinga "drag
        // y soltar mientras apuntaba" de "drag y volver al centro").
        bool anyHeld = _uiHeld[0] || _uiHeld[1] || _uiHeld[2];
        if (!anyHeld) _uiAim = Vector2.zero;

        return intent;
    }

    bool IsHeld(int slot) => _kbHeld[slot] || _uiHeld[slot];

    AbilityInputState ComputeSlot(int slot)
    {
        bool was = _heldPrev[slot];
        bool now = IsHeld(slot);
        if (!was && now) return AbilityInputState.Pressed;
        if (was && now)  return AbilityInputState.Held;
        if (was && !now) return AbilityInputState.Released;
        return AbilityInputState.None;
    }

    // ----- Hooks para botones tactiles -----
    public void OnAbilityButtonDown(int slot) { if (slot >= 0 && slot < 3) _uiHeld[slot] = true; }
    public void OnAbilityButtonUp(int slot)   { if (slot >= 0 && slot < 3) _uiHeld[slot] = false; }
    public void OnAttackButton()              => _attackQueued = true;

    /// <summary>Vector unitario de drag-aim desde el boton tactil. Vector2.zero para limpiar.</summary>
    public void SetAimInput(Vector2 aim)      => _uiAim = aim;
}
