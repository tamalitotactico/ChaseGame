using UnityEngine;

[CreateAssetMenu(fileName = "TeleportSmashAbility", menuName = "ChaseGame/Abilities/Teleport Smash")]
public class TeleportSmashAbilityData : AbilityData
{
    [Header("Cast (Canalizacion)")]
    [Tooltip("Segundos de canalizacion antes de ejecutar el teletransporte. El Hunter se inmoviliza durante este tiempo.")]
    public float castTime = 1f;

    [Header("Teleport")]
    [Tooltip("Distancia maxima del teletransporte en la direccion de movimiento.")]
    public float teleportDistance = 6f;

    [Tooltip("Layer mask de los muros (para no teletransportarse dentro de uno).")]
    public LayerMask wallLayer;

    [Tooltip("Margen para detenerse antes del muro al teletransportarse.")]
    public float wallPadding = 0.3f;

    [Header("AoE on landing")]
    [Tooltip("Radio del area de efecto en el destino del teletransporte.")]
    public float aoeRadius = 3.5f;

    [Tooltip("Duracion del FearedEffect aplicado a Prey en el AoE.")]
    public float aoeFearDuration = 2f;

    [Tooltip("Duracion del SlowedEffect aplicado a Prey en el AoE.")]
    public float aoeSlowDuration = 2f;

    [Tooltip("Multiplicador de velocidad del slow [0..1].")]
    [Range(0f, 1f)]
    public float aoeSlowMultiplier = 0.5f;

    [Tooltip("Layer mask que define que objetos son Prey.")]
    public LayerMask preyLayer;

    [Tooltip("Prefab de particulas que se instancia en el punto de llegada y queda en el mundo.")]
    public GameObject aoeFXPrefab;

    [Header("VFX - Aura (sigue al Hunter)")]
    [Tooltip("Prefab del aura que se adjunta al Hunter tras el teletransporte.")]
    public GameObject auraFXPrefab;

    [Tooltip("Segundos que dura el aura en el Hunter antes de apagarse.")]
    public float auraDuration = 3f;

    [Header("Audio")]
    [Tooltip("Sonido al EMPEZAR la canalizacion (cuando el Hunter se inmoviliza y empieza el channeling).")]
    public AudioCue sfxOnCastStart;

    [Tooltip("Sonido al LLEGAR a la posicion B (teletransporte ejecutado). Se reproduce en el punto de llegada.")]
    public AudioCue sfxOnLanding;

    [Header("Self buff (Hunter)")]
    [Tooltip("Duracion del HastedEffect aplicado al Hunter tras teletransportarse.")]
    public float hasteDuration = 3f;

    [Tooltip("Multiplicador de velocidad del haste (>= 1).")]
    public float hasteMultiplier = 1.6f;

    public override float IndicatorRange   => teleportDistance;
    public override float IndicatorRadius  => aoeRadius;

    public override Ability CreateRuntime() => new TeleportSmashAbility(this);
}
