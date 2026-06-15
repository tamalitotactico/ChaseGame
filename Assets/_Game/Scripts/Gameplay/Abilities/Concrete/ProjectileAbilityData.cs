using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileAbility", menuName = "ChaseGame/Abilities/Projectile")]
public class ProjectileAbilityData : AbilityData
{
    [Header("Projectile")]
    [Tooltip("Prefab del proyectil. Debe tener Rigidbody2D, Collider2D trigger, y Projectile.")]
    public GameObject projectilePrefab;

    [Tooltip("Velocidad del proyectil (u/s).")]
    public float speed = 8f;

    [Tooltip("Distancia maxima antes de auto-destruirse.")]
    public float range = 6f;

    [Tooltip("Radio del proyectil (u): fuente unica de collider de impacto + wall-sensor (mitad) + sprite + ancho del indicador.")]
    public float projectileRadius = 0.08f;

    [Tooltip("Dano al impactar.")]
    public int damage = 1;

    [Header("Audio Impacto")]
    [Tooltip("Sonido al impactar un IDamageable o pared. Activar spatial en el AudioCue para panning posicional.")]
    public AudioCue sfxOnHit;

    public override float IndicatorRange => range;
    public override float ProjectileRadius => projectileRadius;

    public override AimStyle Aim => AimStyle.Direction;

    public override Ability CreateRuntime() => new ProjectileAbility(this);
}
