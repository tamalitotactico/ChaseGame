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

    /// <summary>Golpe LETAL: lleva la vida del objetivo a 0 sin importar Amount ni su HP actual
    /// (derriba en 1 golpe). Lo usa el ataque basico bajo True Form. Sigue respetando la
    /// invulnerabilidad salvo que IgnoreInvulnerability tambien sea true.</summary>
    public bool    Lethal;

    /// <summary>True si el golpe proviene de un ataque BASICO (no de una habilidad). Lo estampa
    /// CombatController; Character lo usa para publicar BasicAttackLandedEvent (carga de ults).</summary>
    public bool    FromBasicAttack;

    public static DamageInfo Simple(int amount, Character source, Vector2 origin)
        => new DamageInfo { Amount = amount, Source = source, Origin = origin };
}
