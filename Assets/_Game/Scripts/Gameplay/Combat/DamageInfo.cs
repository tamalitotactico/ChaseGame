using UnityEngine;

/// <summary>
/// Datos de un golpe. Producido por CombatController, consumido por IDamageable.
/// Phase 3 se serializara via NetworkInput cuando los hits sean RPCs validados
/// por la autoridad.
/// </summary>
public struct DamageInfo
{
    public int    Amount;
    public Character Source;       // quien aplica el dano (puede ser null para hazards)
    public Vector2 Origin;         // posicion del impacto
    public Vector2 Direction;      // direccion del golpe (para knockback)
    public float   KnockbackForce; // 0 = sin knockback
    public bool    IgnoreInvulnerability;

    public static DamageInfo Simple(int amount, Character source, Vector2 origin)
        => new DamageInfo { Amount = amount, Source = source, Origin = origin };
}
