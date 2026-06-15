using UnityEngine;

[CreateAssetMenu(fileName = "GhostWolfAbility", menuName = "ChaseGame/Abilities/Ghost Wolf")]
public class GhostWolfAbilityData : AbilityData
{
    [Header("Lobo")]
    [Tooltip("Prefab del lobo (Rigidbody2D + Collider2D + GhostWolfController).")]
    public GameObject wolfPrefab;

    [Tooltip("Velocidad de desplazamiento del lobo (u/s).")]
    public float moveSpeed = 5f;

    [Tooltip("Vida maxima del lobo en segundos (si no muerde a nadie).")]
    public float maxLifetime = 15f;

    [Tooltip("Segundos iniciales en los que avanza recto en la direccion apuntada antes de perseguir.")]
    public float aimBiasSeconds = 0.5f;

    [Tooltip("Cada cuantos segundos recalcula la ruta A* al prey mas cercano.")]
    public float rePathInterval = 0.5f;

    [Tooltip("Margen para detenerse antes de un muro (el lobo no atraviesa muros).")]
    public float wallPadding = 0.15f;

    [Header("Mordisco (1 solo)")]
    [Tooltip("Distancia a la que considera alcanzado al prey y muerde.")]
    public float biteRadius = 0.6f;

    [Tooltip("Duracion del SlowedEffect del mordisco.")]
    public float biteSlowDuration = 2f;

    [Tooltip("Multiplicador de velocidad del slow del mordisco [0..1].")]
    [Range(0f, 1f)]
    public float biteSlowMultiplier = 0.5f;

    [Tooltip("Segundos de reveal del prey mordido a los hunters (World Target Pointer). 0 = sin reveal.")]
    public float revealDuration = 1f;

    [Tooltip("Sprite del puntero de reveal (null = triangulo generado).")]
    public Sprite pointerSprite;

    [Tooltip("Tamano del puntero en px.")]
    public float pointerSize = 44f;

    [Tooltip("Color del puntero de reveal.")]
    public Color pointerColor = new Color(0.6f, 0.3f, 1f, 1f);

    public override AimStyle Aim => AimStyle.Direction;
    public override float IndicatorRange => moveSpeed * aimBiasSeconds; // distancia recta inicial apuntable

    public override Ability CreateRuntime() => new GhostWolfAbility(this);
}
