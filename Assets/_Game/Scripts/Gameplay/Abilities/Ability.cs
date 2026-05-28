/// <summary>
/// Instancia runtime de una habilidad. Creada por AbilityData.CreateRuntime()
/// y persistida en el AbilityController del personaje.
///
/// Lifecycle (drive desde AbilityController):
///   - Tick(dt) cada frame para reducir cooldown.
///   - BeginActivation(ctx) al pulsar el slot. Devuelve Aimer (o null = instant).
///     - Si null: AbilityController llama Execute() inmediatamente.
///     - Si no null: AbilityController itera Aimer.Tick(intent) hasta release,
///       luego llama Execute(ctx, aimer.GetResult()).
///   - StartCooldown() al ejecutar.
/// </summary>
public abstract class Ability
{
    public AbilityData Data { get; }
    public float CooldownRemaining { get; protected set; }
    public bool  IsReady => CooldownRemaining <= 0f;

    protected Ability(AbilityData data)
    {
        Data = data;
    }

    /// <summary>Devuelve un Aimer si la ability requiere apuntar; null si es instant.</summary>
    public virtual Aimer BeginActivation(in AbilityContext ctx) => null;

    /// <summary>
    /// Llamado por AbilityController en el frame exacto en que el aimer entra en fase de
    /// canalizacion (IsCasting pasa a true). Solo relevante para habilidades con AimThenCastAimer.
    /// Sobreescribir para reproducir SFX o efectos visuales que deben coincidir con el inicio
    /// del channeling, no con el press del boton.
    /// </summary>
    public virtual void OnCastingBegan(in AbilityContext ctx) { }

    /// <summary>Aplica el efecto. Llamado tras GetResult() o inmediatamente si instant.</summary>
    public abstract void Execute(in AbilityContext ctx, in AimResult aim);

    public virtual void Tick(float dt)
    {
        if (CooldownRemaining > 0f)
            CooldownRemaining -= dt;
    }

    public void StartCooldown() => CooldownRemaining = Data.cooldown;
    public void ResetCooldown() => CooldownRemaining = 0f;
}
