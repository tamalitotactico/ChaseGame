/// <summary>
/// Capa de aim/preview LOCAL. No se replica. El servidor (Phase 3) recibe solo
/// el AimResult final cuando el jugador confirma.
///
/// Lifecycle (estilo Brawl Stars):
///   1. AbilityController llama Begin(ctx) cuando el slot pasa a Pressed.
///   2. Tick(intent) cada frame mientras esta activo.
///   3. HandleRelease(intent) en el frame del Released — el Aimer decide:
///      - Fire: ejecutar la habilidad ahora.
///      - Cancel: descartar la habilidad sin ejecutar (ej. drag y vuelta al centro).
///      - Continue: seguir activo sin ejecutar (ej. AimThenCast pasa a fase de cast).
///   4. Si IsComplete pasa a true en algun Tick, el controller ejecuta automaticamente
///      (timer de canalizacion expirado).
///   5. GetResult() devuelve el AimResult final.
///   6. End()/Cancel() siempre, sea por confirmacion o cancelacion.
///
/// Cada subclase puede manejar su propia visualizacion (linea, cono, circulo)
/// instanciando un preview prefab via ISpawnService o LineRenderer.
/// </summary>
public abstract class Aimer
{
    protected AbilityContext Ctx;

    /// <summary>
    /// True cuando el Aimer decide que debe ejecutarse sin esperar Release del input.
    /// Usado por canalizaciones con timer fijo (AimThenCastAimer fase 2).
    /// </summary>
    public virtual bool IsComplete    => false;

    /// <summary>
    /// False en Aimers que no se pueden cancelar por perdida de input (ej. canalizaciones
    /// ya iniciadas). En la fase de aim previa, suele ser true.
    /// </summary>
    public virtual bool IsCancellable => true;

    /// <summary>
    /// True solo cuando el aimer esta canalizando una habilidad (no apuntando).
    /// La UI de la barra de canalizacion se muestra solo cuando esto es true.
    /// Apuntar (DirectionalAimer puro, AimThenCastAimer fase de aim) NO es canalizar.
    /// </summary>
    public virtual bool IsCasting => false;

    /// <summary>
    /// Progreso del Aimer de 0 (inicio) a 1 (completo). Usado para animar la barra de
    /// canalizacion. Solo relevante cuando IsCasting es true.
    /// </summary>
    public virtual float Progress => 0f;

    /// <summary>
    /// Segundos restantes hasta completar la canalizacion. Solo relevante cuando
    /// IsCasting es true.
    /// </summary>
    public virtual float RemainingSeconds => 0f;

    /// <summary>
    /// Direccion actual del aim. Usada por AimIndicators (linea/cono/teleport)
    /// para renderizar la preview en runtime. Default: facing direction del owner.
    /// Subclases que tienen direccion real (DirectionalAimer, AimThenCastAimer)
    /// la sobreescriben con su valor interno.
    /// </summary>
    public virtual UnityEngine.Vector2 CurrentDirection
        => Ctx.FacingDirection.sqrMagnitude > 0f ? Ctx.FacingDirection : UnityEngine.Vector2.right;

    public void Begin(in AbilityContext ctx)
    {
        Ctx = ctx;
        OnBegin();
    }

    public abstract void Tick(in BrainIntent intent);

    /// <summary>
    /// Llamado en el frame en que el slot pasa a Released. El Aimer decide que hacer.
    /// Default: Fire (las abilities simples disparan al soltar).
    /// </summary>
    public virtual ReleaseDecision HandleRelease(in BrainIntent intent) => ReleaseDecision.Fire;

    public abstract AimResult GetResult();

    public void End()    => OnEnd();
    public void Cancel() => OnEnd();

    protected virtual void OnBegin() { }
    protected virtual void OnEnd()   { }
}

/// <summary>Decision del Aimer en el frame del Released.</summary>
public enum ReleaseDecision
{
    /// <summary>Disparar la habilidad ahora.</summary>
    Fire,
    /// <summary>Descartar la habilidad sin ejecutar. Va al cooldown? No — solo se cancela.</summary>
    Cancel,
    /// <summary>Seguir activo (ej. transicion a fase de canalizacion).</summary>
    Continue
}
