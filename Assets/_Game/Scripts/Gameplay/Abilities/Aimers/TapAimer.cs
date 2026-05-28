/// <summary>
/// Aimer "vacio" para abilities tap-only (Remnant, etc). No lee aim ni dibuja
/// preview. Su unica funcion es esperar el Released del boton para que la
/// habilidad se dispare al soltar (no al presionar) — uniformando la sensacion
/// de input con las abilities apuntables.
///
/// La UI debe configurar el boton de estas habilidades con allowAim=false para
/// que no se interprete drag como aim.
/// </summary>
public sealed class TapAimer : Aimer
{
    public override bool IsCasting     => false;
    public override bool IsCancellable => true;

    public override void Tick(in BrainIntent intent) { }

    public override ReleaseDecision HandleRelease(in BrainIntent intent)
        => ReleaseDecision.Fire;

    public override AimResult GetResult() => default;
}
