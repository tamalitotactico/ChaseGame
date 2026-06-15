using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Boton de habilidad estilo Brawl Stars. Soporta dos modos. El modo NO es estatico:
/// se DERIVA en runtime de la habilidad bindeada en este slot (Ability.RequiresAim) cada vez
/// que se bindea al jugador local, porque el roster es data-driven y el mismo indice de slot
/// puede ser tap-only o apuntable segun el personaje equipado. El campo serializado 'allowAim'
/// solo es fallback antes de bindear. Los dos modos:
///
/// modo tap-only (RequiresAim=false; abilities como Remnant/señuelo):
///   - Press: arma la habilidad (notifica al PlayerBrain).
///   - Drag: ignorado, el handle no se mueve, no se apunta.
///   - Release: dispara. El TapAimer del controller ejecuta al instante.
///
/// modo apuntable (RequiresAim=true; abilities como FearProjectile, TeleportSmash):
///   - Press: arma. NO ejecuta nada todavia.
///   - Drag: mueve el handle dentro del background (mismo gesto que el
///     VirtualJoystick de movimiento). La direccion normalizada se manda al
///     PlayerBrain via SetAimInput.
///   - Release con drag fuera del deadzone: dispara en esa direccion.
///   - Release con handle dentro del deadzone (jugador devolvio al centro):
///     cancela — la habilidad NO se ejecuta y NO entra en cooldown. Gesto
///     estandar de cancelacion en Brawl Stars.
///   - Release sin haber dragueado (tap puro): dispara con direccion de
///     fallback (facing/movimiento).
///
/// Se autoconecta al PlayerBrain del jugador local via CharacterSpawnedEvent.
/// Funciona igual con touch y mouse: uGUI EventSystem unifica ambos.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AimableAbilityButton : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Binding")]
    [Tooltip("Indice del slot de habilidad (0, 1, 2).")]
    [SerializeField] int slot;

    [Tooltip("FALLBACK solamente. En runtime el modo de aim se DERIVA de la habilidad bindeada en este " +
             "slot (Ability.RequiresAim): el roster es data-driven y el mismo slot puede ser tap-only o " +
             "apuntable segun el personaje equipado. Este valor solo se usa antes de bindear / si el slot " +
             "no tiene habilidad resuelta.")]
    [SerializeField] bool allowAim = true;

    [Header("Joystick layout (igual que VirtualJoystick)")]
    [Tooltip("RectTransform de fondo del joystick (area base). Si esta vacio se usa el propio RectTransform de este boton.")]
    [SerializeField] RectTransform background;

    [Tooltip("RectTransform del handle (punto movil). Si esta vacio el drag no muestra handle pero igual apunta.")]
    [SerializeField] RectTransform handle;

    [Tooltip("Radio maximo en pixels que puede moverse el handle. Tambien define el clamp de aim.")]
    [SerializeField] float maxRadius = 60f;

    [Tooltip("Distancia minima en pixels para considerar drag-aim. Bajo este umbral se interpreta como 'sin aim' (cancela en abilities apuntables o sirve para tap puro).")]
    [SerializeField] float deadzonePx = 8f;

    PlayerBrain       _pb;
    AbilityController _ac;
    bool              _effectiveAllowAim;   // derivado de la habilidad del slot; cae a 'allowAim' si no hay
    bool          _down;
    RectTransform _rt;
    Camera        _eventCam;

    void Awake()
    {
        _rt = (RectTransform)transform;
        if (background == null) background = _rt;
        if (handle != null) handle.localPosition = Vector3.zero;
        _effectiveAllowAim = allowAim;       // fallback hasta bindear la habilidad real del slot
    }

    void OnEnable()
    {
        EventBus.Subscribe<CharacterSpawnedEvent>(OnSpawned);
        if (handle != null) handle.localPosition = Vector3.zero;
        // Si este boton se re-activo DESPUES del spawn (ej. AbilityHUD prende el Slot_R/ultimate al
        // pasar a Hunter, ya reaccionando al CharacterSpawnedEvent), nunca recibiria ese evento y la
        // habilidad quedaria sin bindear. Hacemos pull de la ref retenida del jugador local.
        if (PlayerBrain.Local != null) BindCharacter(PlayerBrain.Local.GetComponent<Character>());
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSpawnedEvent>(OnSpawned);
        if (_pb != null && _down)
        {
            _pb.OnAbilityButtonUp(slot);
            _pb.SetAimInput(Vector2.zero);
        }
        _pb   = null;
        _ac   = null;
        _down = false;
        if (handle != null) handle.localPosition = Vector3.zero;
    }

    void OnSpawned(CharacterSpawnedEvent e)
    {
        if (e.Character == null) return;
        BindCharacter(e.Character);
    }

    /// <summary>Bindea al jugador local y deriva el modo de aim del slot. Ignora no-jugadores (bots).</summary>
    void BindCharacter(Character c)
    {
        if (c == null) return;
        var pb = c.GetComponent<PlayerBrain>();
        if (pb == null) return;             // solo el jugador local tiene PlayerBrain
        _pb = pb;
        _ac = c.Abilities;
        RefreshAimMode();
    }

    /// <summary>
    /// Deriva si este slot acepta aim-drag de la habilidad realmente bindeada (Ability.RequiresAim).
    /// El roster es data-driven: el mismo indice de slot puede ser tap-only o apuntable segun el
    /// personaje equipado, asi que un 'allowAim' estatico por boton no puede ser correcto para todos.
    /// </summary>
    void RefreshAimMode()
    {
        _effectiveAllowAim = allowAim;      // fallback
        var abilities = _ac != null ? _ac.Abilities : null;
        if (abilities != null && slot >= 0 && slot < abilities.Length && abilities[slot] != null)
            _effectiveAllowAim = abilities[slot].RequiresAim;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (_pb == null) return;
        _down     = true;
        _eventCam = e.pressEventCamera;
        _pb.OnAbilityButtonDown(slot);
        // El handle queda en centro: esperamos al primer drag para distinguir
        // tap puro de drag-aim. Si allowAim=false jamas se movera.
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_down || _pb == null) return;

        // Si la habilidad es tap-only, ignorar el drag por completo.
        if (!_effectiveAllowAim)
        {
            if (handle != null) handle.localPosition = Vector3.zero;
            return;
        }
        if (background == null) return;

        // Convertir posicion de pantalla a espacio local del background (mismo
        // patron que VirtualJoystick.OnDrag).
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, e.position, _eventCam, out Vector2 localPoint))
            return;

        // Dentro del deadzone: tratar como "sin apuntar" en este frame. Si el
        // jugador ya habia apuntado antes y devolvio al centro, el aimer del
        // controller detectara la cancelacion al Released.
        if (localPoint.sqrMagnitude < deadzonePx * deadzonePx)
        {
            _pb.SetAimInput(Vector2.zero);
            if (handle != null) handle.localPosition = Vector3.zero;
            return;
        }

        // Clamp al radio maximo y mover el handle.
        Vector2 clamped = Vector2.ClampMagnitude(localPoint, maxRadius);
        if (handle != null) handle.localPosition = clamped;
        _pb.SetAimInput(clamped.normalized);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!_down) return;
        _down = false;
        if (_pb != null)
        {
            // NO limpiamos _uiAim aqui: queremos que el frame del Released
            // todavia vea la ultima direccion de aim del drag (para distinguir
            // fire-on-release de cancel-by-deadzone). PlayerBrain lo limpia
            // automaticamente cuando ningun slot esta presionado.
            _pb.OnAbilityButtonUp(slot);
        }
        if (handle != null) handle.localPosition = Vector3.zero;
        _eventCam = null;
    }
}
