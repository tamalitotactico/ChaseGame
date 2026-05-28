using UnityEngine;

/// <summary>
/// Configuracion estatica de un personaje (stats, bando, abilities).
/// Hunter y Prey usan la misma clase; difieren solo en datos.
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "ChaseGame/Data/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public CharacterTeam team;
    public Sprite icon;

    [Header("Stats")]
    [Tooltip("Velocidad base en unidades/segundo.")]
    public float baseSpeed = 4f;

    [Tooltip("Golpes hasta morir.")]
    public int maxHealth = 2;

    [Tooltip("Segundos de invulnerabilidad tras recibir dano.")]
    public float invulnerabilityOnHit = 1.5f;

    [Header("Basic Attack (opcional)")]
    [Tooltip("Si false, no se procesa ataque basico (Prey por defecto).")]
    public bool hasBasicAttack = false;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public int   attackDamage = 1;

    [Header("Abilities (slots 0=Q, 1=E, 2=R)")]
    public AbilityData[] abilities = new AbilityData[0];
}
