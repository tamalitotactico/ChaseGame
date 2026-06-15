using UnityEngine;

[CreateAssetMenu(fileName = "EnchantAbility", menuName = "ChaseGame/Abilities/Enchant")]
public class EnchantAbilityData : AbilityData
{
    [Header("Projectile")]
    [Tooltip("Prefab del proyectil. Debe tener Rigidbody2D (Kinematic), Collider2D trigger y CharmProjectile.")]
    public GameObject projectilePrefab;

    [Tooltip("Velocidad del proyectil (u/s).")]
    public float speed = 8f;

    [Tooltip("Distancia maxima antes de auto-destruirse.")]
    public float range = 10f;

    [Tooltip("Maximo de objetivos que perfora. 0 = ilimitado (atraviesa a todos los enemigos en su linea).")]
    public int maxTargets = 0;

    [Tooltip("Radio del proyectil (u): fuente unica de collider de impacto + wall-sensor (mitad) + sprite + ancho del indicador.")]
    public float projectileRadius = 0.30f;

    [Header("Charm (al impacto)")]
    [Tooltip("Duracion del CharmedEffect aplicado al impactar.")]
    public float charmDuration = 1f;

    [Tooltip("Fuerza del jale hacia el Charmer [0..1]. 1 = arrastre a velocidad plena.")]
    [Range(0f, 1f)]
    public float charmPullStrength = 1f;

    [Tooltip("Multiplicador de velocidad del slow aplicado junto al charm [0..1]. Dura charmDuration.")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    [Header("Audio Impacto")]
    [Tooltip("Sonido al impactar un enemigo. Activar spatial en el AudioCue para panning posicional.")]
    public AudioCue sfxOnHit;

    public override float IndicatorRange => range;
    public override float ProjectileRadius => projectileRadius;

    public override AimStyle Aim => AimStyle.Direction;

    public override Ability CreateRuntime() => new EnchantAbility(this);
}
