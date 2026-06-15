using UnityEngine;

/// <summary>
/// Mantiene una camara OVERLAY (URP camera stack) alineada con la camara BASE para que la UI
/// world-space (ej. burbuja de emote) se renderice en el mismo encuadre PERO sin el post-proceso
/// de la base (bloom/grading). La overlay es hija de la base, asi que posicion/rotacion ya siguen
/// por jerarquia; lo unico dinamico que hay que copiar es el orthographicSize (CameraEffectsRig
/// hace zoom modificando ese valor: downed dramatico, fear, etc).
///
/// Setup: este componente en la camara overlay; 'baseCamera' = la camara base (o se resuelve a
/// Camera.main en Awake). La overlay debe tener renderType=Overlay, post-processing OFF, culling
/// mask = solo la capa de UI world-space, y estar en el stack de la base.
/// </summary>
[RequireComponent(typeof(Camera))]
public class OverlayCameraSync : MonoBehaviour
{
    [Tooltip("Camara base a seguir. Si es null, usa Camera.main en Awake.")]
    [SerializeField] Camera baseCamera;

    Camera _self;

    void Awake()
    {
        _self = GetComponent<Camera>();
        if (baseCamera == null) baseCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (baseCamera == null || _self == null) return;
        _self.orthographic     = baseCamera.orthographic;
        _self.orthographicSize = baseCamera.orthographicSize;
        if (!baseCamera.orthographic) _self.fieldOfView = baseCamera.fieldOfView;
    }
}
