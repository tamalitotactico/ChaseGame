using UnityEngine;

/// <summary>
/// Utilidades de debug visual para AI y gameplay. Solo activas en editor/development builds.
/// </summary>
public static class DebugDrawer
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawCircle(Vector3 center, float radius, Color color, int segments = 24)
    {
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float rad1 = Mathf.Deg2Rad * (i * step);
            float rad2 = Mathf.Deg2Rad * ((i + 1) * step);
            Vector3 p1 = center + new Vector3(Mathf.Cos(rad1), Mathf.Sin(rad1), 0) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(rad2), Mathf.Sin(rad2), 0) * radius;
            Debug.DrawLine(p1, p2, color);
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawStateLabel(Vector3 worldPos, string label)
    {
        // Util para visualizar el estado actual de un bot en la scene view.
        // No hace nada en builds; los Gizmos de texto requieren OnDrawGizmos en MonoBehaviour.
        Debug.DrawRay(worldPos + Vector3.up * 0.5f, Vector3.up * 0.2f, Color.yellow);
    }
}
