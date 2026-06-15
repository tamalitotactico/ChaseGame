using UnityEngine;

/// <summary>Proyectil que muere al tocar un muro. Lo invoca su ProjectileWallSensor.</summary>
public interface IWallDestructible
{
    /// <summary>Llamado cuando el sensor de muro (mas chico que el collider de impacto) toca un muro.</summary>
    void OnWallHit(Vector2 point);
}

/// <summary>
/// Sensor de muro del proyectil (game feel / fairness): el collider de IMPACTO (trigger del proyectil)
/// pega a enemigos con su tamano completo — generoso — mientras que la deteccion de MURO usa este
/// sensor con un radio MAS CHICO (≈ la mitad), asi el proyectil solo muere contra un muro cuando su
/// CENTRO realmente lo toca, no al rozarlo.
///
/// Se evalua como un OverlapCircle de 'radius' contra GameLayers.WallMask cada paso de fisica (en vez
/// de un Collider2D hijo, que requeriria un Rigidbody2D anidado y se desincroniza en proyectiles
/// kinematicos rapidos). Va en el ROOT del proyectil, junto al script del proyectil (IWallDestructible).
///
/// El collider de impacto del proyectil ya NO maneja muros (se removio esa rama); solo IDamageable.
/// </summary>
public class ProjectileWallSensor : MonoBehaviour
{
    [Tooltip("Radio del sensor de muro en unidades de mundo. Idealmente la MITAD del radio efectivo " +
             "del collider de impacto. Lo puede setear el spawner via SetRadius o quedar serializado.")]
    [SerializeField] float radius = 0.1f;

    IWallDestructible _projectile;

    public void SetRadius(float r) => radius = Mathf.Max(0.001f, r);

    void Awake() => _projectile = GetComponent<IWallDestructible>();

    void FixedUpdate()
    {
        if (radius <= 0f) return;
        if (Physics2D.OverlapCircle(transform.position, radius, GameLayers.WallMask) != null)
            _projectile?.OnWallHit(transform.position);
    }
}
