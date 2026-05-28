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

    [Tooltip("Dano al impactar.")]
    public int damage = 1;

    [Header("Audio Impacto")]
    [Tooltip("Sonido al impactar un IDamageable o pared. Activar spatial en el AudioCue para panning posicional.")]
    public AudioCue sfxOnHit;

    public override float IndicatorRange => range;

    public override Ability CreateRuntime() => new ProjectileAbility(this);
}
