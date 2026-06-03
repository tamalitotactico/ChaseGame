using System;
using UnityEngine;

/// <summary>
/// Ataque basico del personaje (no es ability). Solo se agrega como componente
/// a personajes con CharacterData.hasBasicAttack = true (Hunter en Phase 0).
///
/// Phase 0: OverlapCircle area attack al pulsar AttackPressed. Phase 1+ podra
/// expandirse a hitboxes animadas, combos, etc.
/// </summary>
public class CombatController : MonoBehaviour
{
    [SerializeField] float attackRange    = 1.5f;
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] int   damage         = 1;
    [Tooltip("Layers que recibe el ataque. Default 0 (nada): asignar la capa de personajes en el prefab.")]
    [SerializeField] LayerMask targetLayers = 0;

    // Buffer compartido para evitar GC alloc por ataque (Unity es single-thread).
    static readonly Collider2D[] _hitBuffer = new Collider2D[16];

    Character _owner;
    float     _cooldownRemaining;

    public event Action<float> OnAttackUsed; // pasa cooldown total
    public bool  IsReady => _cooldownRemaining <= 0f;
    public float CooldownRemaining => _cooldownRemaining;
    public float AttackRange      => attackRange;
    public float AttackCooldown   => attackCooldown;
    public int   Damage           => damage;

    void Awake()
    {
        _owner = GetComponent<Character>();
    }

    public void Setup(float range, float cooldown, int dmg)
    {
        attackRange    = range;
        attackCooldown = cooldown;
        damage         = dmg;
    }

    public void Tick(in BrainIntent intent, float dt)
    {
        if (_cooldownRemaining > 0f) _cooldownRemaining -= dt;

        if (intent.AttackPressed && IsReady)
            DoAttack();
    }

    void DoAttack()
    {
        _cooldownRemaining = attackCooldown;
        OnAttackUsed?.Invoke(attackCooldown);

        Vector2 origin = transform.position;
        int count = Physics2D.OverlapCircleNonAlloc(origin, attackRange, _hitBuffer, targetLayers);
        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];
            if (col == null || col.gameObject == gameObject) continue;
            if (col.TryGetComponent<IDamageable>(out var d) && d.IsTargetable)
            {
                d.TakeDamage(new DamageInfo
                {
                    Amount    = damage,
                    Source    = _owner,
                    Origin    = origin,
                    Direction = ((Vector2)col.transform.position - origin).normalized
                });
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
