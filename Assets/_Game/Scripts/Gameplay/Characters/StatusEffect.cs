using UnityEngine;

/// <summary>
/// Base de todos los efectos de estado (stun, slow, mark, etc.).
/// Cada efecto concreto hereda de esta clase y sobreescribe las propiedades relevantes.
/// </summary>
public abstract class StatusEffect
{
    public float Duration  { get; protected set; }
    public float Remaining { get; protected set; }
    public bool  IsExpired => Remaining <= 0f;

    /// <summary>Si true, el Motor del personaje se pone a 0 mientras este efecto este activo.</summary>
    public virtual bool  BlocksMovement => false;

    /// <summary>Si true, el intent de ataque y habilidades se borra antes de pasar a Combat/Abilities.</summary>
    public virtual bool  BlocksActions  => false;

    /// <summary>Multiplicador de velocidad aplicado al Motor (1 = sin cambio, 0.5 = 50% de velocidad).</summary>
    public virtual float SpeedModifier  => 1f;

    /// <summary>
    /// Si tiene valor, sobreescribe el MoveInput del personaje por este vector.
    /// Usado por FearedEffect para forzar al objetivo a huir en una direccion fija.
    /// Default null = no override.
    /// </summary>
    public virtual System.Nullable<UnityEngine.Vector2> ForceMoveInput => null;

    /// <summary>Llamado una vez cuando el efecto se aplica al personaje.</summary>
    public abstract void OnApply(Character target);

    /// <summary>Llamado una vez cuando el efecto expira o es removido manualmente.</summary>
    public abstract void OnRemove(Character target);

    /// <summary>Tick del efecto. Por defecto solo cuenta el tiempo restante.</summary>
    public virtual void Tick(float dt)
    {
        Remaining = Mathf.Max(0f, Remaining - dt);
    }
}
