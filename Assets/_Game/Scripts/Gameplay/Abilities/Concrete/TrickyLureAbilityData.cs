using UnityEngine;

[CreateAssetMenu(fileName = "TrickyLureAbility", menuName = "ChaseGame/Abilities/Tricky Lure")]
public class TrickyLureAbilityData : AbilityData
{
    [Header("Clon-senuelo")]
    [Tooltip("Prefab del clon (Rigidbody2D + Collider2D trigger + SpriteRenderer + TrickyLureClone).")]
    public GameObject clonePrefab;

    [Tooltip("Velocidad del clon (u/s).")]
    public float cloneSpeed = 4f;

    [Tooltip("Vida del clon en segundos.")]
    public float cloneLifetime = 3f;

    [Tooltip("Golpes que aguanta el clon.")]
    public int cloneHealth = 1;

    [Tooltip("SFX de risa al morir el clon.")]
    public AudioCue laughSfx;

    [Tooltip("Margen (u) que el clon deja antes de un muro al frenar (raycast-clamp).")]
    public float wallPadding = 0.15f;

    [Tooltip("Color del outline que ven los ALIADOS (indicador de 'esto es un clon'). El enemigo lo ve identico.")]
    public Color allyOutlineColor = new Color(0.3f, 0.9f, 1f, 1f);

    [Header("Invisibilidad del prey")]
    [Tooltip("Segundos que el prey queda invisible tras crear el clon.")]
    public float invisDuration = 1f;

    [Tooltip("Multiplicador de velocidad (haste) mientras dura la invisibilidad (>= 1).")]
    public float hasteMultiplier = 1.5f;

    public override AimStyle Aim => AimStyle.Direction;
    public override float IndicatorRange => cloneSpeed * cloneLifetime; // distancia maxima que recorre el clon

    public override Ability CreateRuntime() => new TrickyLureAbility(this);
}
