using UnityEngine;

[CreateAssetMenu(fileName = "BearTrapAbility", menuName = "ChaseGame/Abilities/Bear Trap")]
public class BearTrapAbilityData : AbilityData
{
    [Header("Trampa")]
    [Tooltip("Prefab de la trampa (SpriteRenderer + BearTrapPlaceable).")]
    public GameObject trapPrefab;

    [Tooltip("Radio en el que un hunter dispara la trampa.")]
    public float triggerRadius = 0.9f;

    [Tooltip("Duracion del aturdimiento al hunter.")]
    public float stunDuration = 1f;

    [Header("Reveal (World Target Pointer)")]
    [Tooltip("Segundos que se revela la posicion del hunter a los preys.")]
    public float revealDuration = 1f;

    [Tooltip("Sprite del puntero de reveal (null = triangulo generado).")]
    public Sprite pointerSprite;

    [Tooltip("Tamano del puntero en px.")]
    public float pointerSize = 44f;

    [Tooltip("Color del puntero de reveal.")]
    public Color pointerColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Header("Cupo / deteccion")]
    [Tooltip("Cupo de trampas de oso simultaneas por prey.")]
    public int maxTraps = 3;

    [Tooltip("Mascara de personajes para la deteccion (el filtro real es por team). Default Everything.")]
    public LayerMask characterMask = ~0;

    public override AimStyle Aim => AimStyle.SelfAoE;
    public override float IndicatorRadius => triggerRadius;

    public override Ability CreateRuntime() => new BearTrapAbility(this);
}
