using UnityEngine;

/// <summary>
/// Aplica el tamano del proyectil desde AbilityData.ProjectileRadius en spawn, como FUENTE UNICA:
///   - sprite visible escalado a diametro 2*radius (lo que se VE),
///   - collider de impacto a radio mundo = radius (con lo que COLISIONA),
///   - ProjectileWallSensor a radius*0.5 (donde MUERE en muro),
/// de modo que visible == collider == indicador. Robusto si el SpriteRenderer y el collider comparten
/// el transform (root) o estan en objetos distintos.
/// </summary>
public static class ProjectileSetup
{
    public static void Apply(GameObject go, float radius)
    {
        if (go == null || radius <= 0f) return;

        // 1) Escalar el sprite para que su diametro VISIBLE sea 2*radius (puede mover el lossyScale
        //    del root si el sprite esta en el root).
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float currentWorld = sr.sprite.bounds.size.x * Mathf.Abs(sr.transform.lossyScale.x);
            if (currentWorld > 0.0001f)
            {
                float k = (radius * 2f) / currentWorld;
                sr.transform.localScale *= k;
            }
        }

        // 2) Collider de impacto: fijar el radio MUNDO = radius compensando el lossyScale actual
        //    (que pudo cambiar en el paso 1 si el sprite es el root).
        if (go.TryGetComponent<CircleCollider2D>(out var col))
        {
            float ls = Mathf.Abs(col.transform.lossyScale.x);
            col.radius = ls > 0.0001f ? radius / ls : radius;
        }

        // 3) Wall-sensor: radio MUNDO directo (OverlapCircle), mitad del impacto.
        if (go.TryGetComponent<ProjectileWallSensor>(out var sensor))
            sensor.SetRadius(radius * 0.5f);
    }
}
