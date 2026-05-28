using UnityEngine;

[CreateAssetMenu(fileName = "FearProjectileAbility", menuName = "ChaseGame/Abilities/Fear Projectile")]
public class FearProjectileAbilityData : AbilityData
{
    [Header("Projectile")]
    [Tooltip("Prefab del proyectil. Debe tener Rigidbody2D (Kinematic), Collider2D trigger, y FearProjectile.")]
    public GameObject projectilePrefab;

    [Tooltip("Velocidad del proyectil (u/s).")]
    public float speed = 7f;

    [Tooltip("Distancia maxima antes de auto-destruirse.")]
    public float range = 12f;

    [Tooltip("Si true, el proyectil persigue al target inicial. Si false, va en linea recta.")]
    public bool homing = true;

    [Tooltip("Velocidad de giro en grados/seg. Mayor = curva mas cerrada.")]
    public float homingTurnRateDeg = 360f;

    [Header("Impact effects")]
    [Tooltip("Duracion del FearedEffect aplicado al impactar.")]
    public float fearDuration = 2f;

    [Tooltip("Duracion del SlowedEffect aplicado al impactar.")]
    public float slowDuration = 2f;

    [Tooltip("Multiplicador de velocidad del slow [0..1]. 0.5 = 50% de velocidad.")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    [Header("Audio Impacto")]
    [Tooltip("Sonido al impactar un Character enemigo. Activar spatial en el AudioCue para panning posicional.")]
    public AudioCue sfxOnHit;

    public override float IndicatorRange => range;

    public override Ability CreateRuntime() => new FearProjectileAbility(this);
}
