/// <summary>
/// Cualquier entidad capaz de recibir dano implementa esta interface.
/// El emisor (CombatController, abilities) llama TakeDamage() sin importar
/// si el receptor es un Character, una pared destructible o una trampa.
/// </summary>
public interface IDamageable
{
    bool IsAlive { get; }

    /// <summary>
    /// True si esta entidad puede recibir dano AHORA. Default = IsAlive, pero un
    /// Character downed sigue siendo targetable (Hunter puede rematar). Implementar
    /// con default interface method (C# 8+) o override por implementador.
    /// </summary>
    bool IsTargetable => IsAlive;

    void TakeDamage(in DamageInfo info);
}
