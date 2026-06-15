using UnityEngine;

[CreateAssetMenu(fileName = "BeaconAbility", menuName = "ChaseGame/Abilities/Beacon")]
public class BeaconAbilityData : AbilityData
{
    [Header("Baliza")]
    [Tooltip("Prefab de la baliza (Collider2D trigger + BeaconPlaceable).")]
    public GameObject beaconPrefab;

    // La vida (s) de la baliza si nadie la rompe usa el campo base 'duration' del AbilityData.
    // (NO redeclarar 'duration' aqui: Unity no soporta serializar el mismo nombre en clase + padre.)

    [Tooltip("Radio del aura de buff.")]
    public float areaRadius = 2.5f;

    [Header("Buff a aliados")]
    [Tooltip("Multiplicador de velocidad que reciben los aliados dentro (>= 1).")]
    public float hasteMultiplier = 1.5f;

    [Tooltip("Segundos que dura el boost tras salir del area (o al expirar la baliza).")]
    public float exitBoostDuration = 1f;

    [Header("Rompible")]
    [Tooltip("Golpes del hunter para romperla.")]
    public int beaconHealth = 1;

    [Tooltip("Cupo de balizas simultaneas por prey.")]
    public int maxBeacons = 1;

    public override float IndicatorRadius => areaRadius;

    public override AimStyle Aim => AimStyle.SelfAoE;

    public override Ability CreateRuntime() => new BeaconAbility(this);
}
