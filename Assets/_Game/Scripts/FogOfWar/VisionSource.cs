using UnityEngine;

/// <summary>
/// Componente que indica que esta entidad genera vision propia (rompe la niebla).
/// Solo el jugador humano tiene este componente en Fase 1.
/// </summary>
public class VisionSource : MonoBehaviour
{
    [Header("Vision")]
    [Tooltip("Radio de vision en unidades de mundo. Hunter: 8, Prey: 5.")]
    [SerializeField] public float visionRadius = 5f;

    public float VisionRadius => visionRadius;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }
}
