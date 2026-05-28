using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tuning de bots. Una clase, pero se crea un asset por rol (HunterBotTuning.asset,
/// PreyBotTuning.asset). Los states leen via bot.Tuning.X — todo editable en
/// el inspector sin recompilar.
///
/// Si BotBrain no tiene asset asignado, en runtime se crea un instance con
/// estos defaults para no crashear (logueando warning).
/// </summary>
[CreateAssetMenu(fileName = "BotTuning", menuName = "ChaseGame/Data/Bot Tuning")]
public class BotTuningData : ScriptableObject
{
    [Header("Common")]
    [Tooltip("Distancia maxima a la que detecta enemigos visibles.")]
    public float visionRange = 14f;

    [Tooltip("Distancia para entrar a AttackState (Hunter).")]
    public float attackRange = 1.5f;

    [Header("Hunter - Patrol")]
    public float patrolRadius          = 11f;
    public float patrolWaypointTimeout = 5f;

    [Header("Hunter - Chase")]
    public float chaseLosTimeout     = 1f;
    public float chaseRepathInterval = 0.2f;
    [Tooltip("Tiempo maximo de leading (extrapolacion del target). Cap para no extrapolar absurdo.")]
    public float huntLeadTimeMax     = 0.6f;

    [Header("Hunter - Attack")]
    [Tooltip("Tiempo que el Hunter queda comprometido al atacar antes de re-evaluar.")]
    public float attackLockDuration = 0.35f;

    [Header("Hunter - Search")]
    public float searchDuration   = 4f;
    public float searchScanRadius = 2f;

    [Header("Target Selection (scoring)")]
    [Tooltip("Bonus de score al target downed. Mayor = mayor preferencia por rematar.")]
    public float targetBonusDowned    = 1000f;
    [Tooltip("Bonus de score al target con HP bajo 100%.")]
    public float targetBonusWounded   = 100f;
    [Tooltip("Bonus de score si no hay aliados del target dentro de targetIsolationRadius.")]
    public float targetBonusIsolated  = 50f;
    [Tooltip("Radio para considerar 'aislado' a un target.")]
    public float targetIsolationRadius = 5f;

    [Header("Abilities (data-driven)")]
    [Tooltip("Reglas de uso de habilidades. Iteradas en orden; dispara la primera que cumpla.")]
    public List<AbilityUseRule> abilityRules = new List<AbilityUseRule>();

    [Tooltip("Cooldown global entre CUALQUIER habilidad. Evita combos absurdos.")]
    public float globalAbilityCooldown = 0.8f;

    [Tooltip("Umbral de velocidad para considerar al target 'moviendose' (TargetCondition.TargetMoving).")]
    public float targetMovingVelocityThreshold = 0.5f;

    [Tooltip("Dot product minimo (con vector target→hunter) para considerar 'huyendo en linea recta'.")]
    [Range(0f, 1f)]
    public float targetFleeingStraightDot = 0.85f;

    [Header("Prey - Wander")]
    public float wanderRadius          = 9f;
    public float wanderWaypointTimeout = 5f;

    [Header("Prey - Flee")]
    public float fleeDistance            = 6f;
    public float fleeRepathInterval      = 0.3f;
    public float fleeLoseVisionTimeout   = 2f;
    public float fleeAbilityCastDistance = 4f;

    [Header("Prey - Flee (escape fan)")]
    [Tooltip("Cantidad de direcciones candidatas evaluadas con raycast al elegir destino de huida.")]
    [Range(3, 21)]
    public int   fleeFanSamples  = 9;
    [Tooltip("Angulo maximo (grados) hacia ambos lados desde awayDir al generar candidatos. 150 = casi semicirculo.")]
    [Range(15f, 180f)]
    public float fleeFanMaxAngle = 150f;
    [Tooltip("Tras detectar stuck, fuerza un destino perpendicular durante este tiempo (s).")]
    public float fleeLateralBurstDuration = 0.4f;

    [Header("Prey - Revive")]
    [Tooltip("Radio en el que un Prey bot considera 'estar encima' de un downed teammate.")]
    public float reviveDetectionRadius = 1.2f;
}
