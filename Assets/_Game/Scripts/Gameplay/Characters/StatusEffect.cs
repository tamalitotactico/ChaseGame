using UnityEngine;

/// <summary>Aspecto de ocultamiento que aplica el shader Game/CharacterEffect a quien SI ve al
/// oculto (aliado, uno mismo, o enemigo dentro del radio de camuflaje). Quien lo ve y con cuanto
/// alpha lo decide StateVisibility; esto solo elige el LOOK.</summary>
public enum StealthStyle { None = 0, Invisible = 1, Camouflage = 2 }

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

    /// <summary>
    /// Tint de overlay que el efecto pinta sobre el sprite (shader Game/CharacterEffect).
    /// Alpha 0 = sin tint. CharacterVisuals elige el de mayor VisualPriority entre los
    /// efectos activos, sin necesitar conocer los tipos concretos (OCP).
    /// </summary>
    public virtual Color VisualTint => new Color(0f, 0f, 0f, 0f);

    /// <summary>Prioridad del tint cuando hay varios efectos activos. Mayor gana.</summary>
    public virtual int VisualPriority => 0;

    /// <summary>True para efectos NEGATIVOS de control (fear, slow, stun, charm). Lo consultan la
    /// inmunidad a CC (rechaza aplicarlos) y el dispel de Repel (los remueve). Los buffs lo dejan en false.</summary>
    public virtual bool IsControlEffect => false;

    /// <summary>True si mientras este efecto este activo el ataque basico del owner es LETAL
    /// (derriba en 1 golpe). Lo consulta CombatController via StatusEffects.HasLethalAttack. True Form lo activa.</summary>
    public virtual bool GrantsLethalAttack => false;

    /// <summary>True si mientras este efecto este activo el owner es INMUNE a nuevos efectos de
    /// control. Lo consulta StatusEffectController.Apply para rechazar IsControlEffect. Repel lo otorga.</summary>
    public virtual bool GrantsCCImmunity => false;

    /// <summary>True si oculta al owner de los ENEMIGOS (canal de visibilidad por estado). Los aliados
    /// siempre lo ven. Lo consulta StateVisibility para decidir el alpha del sprite. Camuflaje/Invisible.</summary>
    public virtual bool HidesFromEnemies => false;

    /// <summary>Solo si HidesFromEnemies: distancia a partir de la cual un enemigo NO lo ve (fade gradual
    /// dentro del radio). &lt;= 0 = invisible total para enemigos (nunca visible). &gt; 0 = camuflaje por distancia.</summary>
    public virtual float EnemyRevealRadius => 0f;

    /// <summary>Look que el shader Game/CharacterEffect aplica a quien SI puede ver al oculto. None =
    /// sin look especial. CharacterVisuals toma el del GetHidingEffect activo. Ver [[StealthStyle]].</summary>
    public virtual StealthStyle Stealth => StealthStyle.None;

    /// <summary>Tinte del look de ocultamiento (solo si Stealth != None). Frio para invisible, verdoso
    /// para camuflaje, por defecto.</summary>
    public virtual Color StealthColor => new Color(0.55f, 0.7f, 1f, 1f);

    /// <summary>True si el efecto se ROMPE cuando el owner actua (ataca o usa una habilidad). Lo remueve
    /// StatusEffectController.BreakActionSensitiveEffects, llamado por Combat/Abilities. Camuflaje/Invisible.</summary>
    public virtual bool BreaksOnOwnerAction => false;

    /// <summary>
    /// Id del icono de estado a mostrar sobre el personaje (StatusIconDisplay lo mapea a un Sprite
    /// via su lista serializada). null = sin icono. Mantener estable: es la clave de assignment en
    /// el inspector (ej. "stun", "slow", "fear", "haste", "charm", "blind", "camo", "invisible").
    /// </summary>
    public virtual string IconId => null;

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
