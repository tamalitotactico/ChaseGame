using UnityEngine;

/// <summary>
/// Marca un punto de spawn en el mapa. El tag del GameObject indica el tipo.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Header("Gizmo")]
    [Tooltip("Color del gizmo en editor para identificar rapidamente el tipo de spawn.")]
    [SerializeField] private Color gizmoColor = Color.green;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.6f);
    }
}
