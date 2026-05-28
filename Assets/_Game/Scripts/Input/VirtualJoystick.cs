using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Joystick virtual tactil para mobile. Implementa IInputProvider.
/// El background es el area base; el handle sigue el toque hasta maxRadius.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IInputProvider, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Layout")]
    [Tooltip("Imagen de fondo del joystick (area base).")]
    [SerializeField] private RectTransform background;

    [Tooltip("Imagen del handle (punto movil).")]
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [Tooltip("Radio maximo en pixels que puede moverse el handle.")]
    [SerializeField] private float maxRadius = 60f;

    private Vector2 _direction = Vector2.zero;
    private bool _isActive;

    public Vector2 Direction => _direction;

    // --- IInputProvider ---

    public Vector2 GetMovementInput() => _direction;
    public bool IsActive => _isActive;

    // --- Eventos de puntero ---

    public void OnPointerDown(PointerEventData eventData)
    {
        _isActive = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Convertir posicion de pantalla a espacio local del background.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        // Limitar al radio maximo.
        Vector2 clamped = Vector2.ClampMagnitude(localPoint, maxRadius);
        handle.localPosition = clamped;

        _direction = clamped.magnitude > 0.01f ? clamped.normalized : Vector2.zero;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isActive = false;
        _direction = Vector2.zero;
        handle.localPosition = Vector2.zero;
    }
}
