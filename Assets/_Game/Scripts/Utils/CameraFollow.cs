using UnityEngine;

/// <summary>
/// Camara 2D con seguimiento suave. Encuentra automaticamente al jugador local
/// suscribiendose a CharacterSpawnedEvent y filtrando por PlayerBrain.
///
/// No tiene dependencia de gameplay: si no hay jugador local, queda estatica.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] float smoothSpeed     = 5f;
    [SerializeField] float orthographicSize = 5f;

    Transform _target;
    Camera    _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (_cam != null && _cam.orthographic)
            _cam.orthographicSize = orthographicSize;
    }

    void OnEnable()
    {
        EventBus.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
    }

    void OnCharacterSpawned(CharacterSpawnedEvent e)
    {
        if (e.Character == null) return;
        // Solo seguir al jugador local (el que tiene PlayerBrain).
        if (e.Character.GetComponent<PlayerBrain>() == null) return;
        _target = e.Character.transform;
    }

    public void SetTarget(Transform t) => _target = t;
    public void ClearTarget()           => _target = null;

    void LateUpdate()
    {
        if (_target == null) return;
        Vector3 desired = new Vector3(_target.position.x, _target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
