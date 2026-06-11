using UnityEngine;

/// <summary>
/// Camara 2D con seguimiento suave. Encuentra automaticamente al jugador local
/// suscribiendose a CharacterSpawnedEvent y filtrando por PlayerBrain.
///
/// No tiene dependencia de gameplay: si no hay jugador local, queda estatica.
///
/// La posicion suavizada se rastrea en <see cref="FollowPosition"/> de forma
/// independiente de transform.position (no se retroalimenta de ella). Esto permite
/// que CameraEffectsRig sume shake/punch encima de transform.position SIN que ese
/// offset se filtre de vuelta al calculo del lerp del frame siguiente (lo cual
/// produciria un bucle de retroalimentacion / deriva acumulativa).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] float smoothSpeed     = 5f;
    [SerializeField] float orthographicSize = 5f;

    Transform        _target;
    Camera           _cam;
    Vector3          _smoothedPos;
    CameraEffectsRig _rig;

    /// <summary>Posicion suavizada "limpia" (sin efectos de camara). Fuente de verdad
    /// para CameraEffectsRig: leerla en vez de transform.position evita feedback.</summary>
    public Vector3 FollowPosition => _smoothedPos;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _rig = GetComponent<CameraEffectsRig>();
        _smoothedPos = transform.position;
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
        if (_target != null)
        {
            Vector3 desired = new Vector3(_target.position.x, _target.position.y, _smoothedPos.z);
            // Suavizado exponencial (frame-rate independiente): a diferencia de
            // Lerp(pos, desired, speed*dt), esto converge igual sea cual sea el FPS.
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            _smoothedPos = Vector3.Lerp(_smoothedPos, desired, t);
        }

        // Si CameraEffectsRig esta presente, el escribira transform.position final
        // (FollowPosition + shake/punch) en su propio LateUpdate, que corre despues
        // (DefaultExecutionOrder). Sin el rig, este sigue siendo el dueño exclusivo.
        if (_rig == null) transform.position = _smoothedPos;
    }
}
