using UnityEngine;

[CreateAssetMenu(fileName = "GanchoAbility", menuName = "ChaseGame/Abilities/Gancho")]
public class GanchoAbilityData : AbilityData
{
    [Header("Projectile")]
    [Tooltip("Prefab del gancho. Debe tener Rigidbody2D (Kinematic), Collider2D trigger y HookProjectile.")]
    public GameObject projectilePrefab;

    [Tooltip("Velocidad del gancho (u/s).")]
    public float speed = 12f;

    [Tooltip("Alcance maximo antes de auto-destruirse.")]
    public float range = 8f;

    [Tooltip("Radio del proyectil (u): fuente unica de collider de impacto + wall-sensor (mitad) + sprite + ancho del indicador.")]
    public float projectileRadius = 0.275f;

    [Header("Pull (al impacto)")]
    [Tooltip("Segundos que tarda en arrastrar al prey hasta el punto medio.")]
    public float pullDuration = 0.25f;

    [Tooltip("Duracion del SlowedEffect al impactar.")]
    public float slowDuration = 1f;

    [Tooltip("Multiplicador de velocidad del slow [0..1].")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    [Header("Audio Impacto")]
    public AudioCue sfxOnHit;

    public override float IndicatorRange => range;
    public override float ProjectileRadius => projectileRadius;

    public override AimStyle Aim => AimStyle.Direction;

    public override Ability CreateRuntime() => new GanchoAbility(this);
}
