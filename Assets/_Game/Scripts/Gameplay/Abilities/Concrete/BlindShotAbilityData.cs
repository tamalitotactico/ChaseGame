using UnityEngine;

[CreateAssetMenu(fileName = "BlindShotAbility", menuName = "ChaseGame/Abilities/Disparo Cegador")]
public class BlindShotAbilityData : AbilityData
{
    [Header("Projectile")]
    [Tooltip("Prefab del proyectil (Rigidbody2D Kinematic + Collider2D trigger + BlindProjectile).")]
    public GameObject projectilePrefab;

    public float speed = 9f;
    public float range = 9f;

    [Tooltip("Radio del proyectil (u): fuente unica de collider de impacto + wall-sensor (mitad) + sprite + ancho del indicador.")]
    public float projectileRadius = 0.275f;

    [Header("Cegado + slow al impacto")]
    [Tooltip("Fraccion de vision del hunter [0..1]. 0.4 = 40% de vision.")]
    [Range(0f, 1f)]
    public float fovMultiplier = 0.4f;

    [Tooltip("Duracion del cegado (FOV).")]
    public float fovDuration = 2f;

    [Tooltip("Duracion del slow.")]
    public float slowDuration = 1f;

    [Tooltip("Multiplicador de velocidad del slow [0..1].")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    [Header("Audio")]
    public AudioCue sfxOnHit;

    public override float IndicatorRange => range;
    public override float ProjectileRadius => projectileRadius;

    public override AimStyle Aim => AimStyle.Direction;

    public override Ability CreateRuntime() => new BlindShotAbility(this);
}
