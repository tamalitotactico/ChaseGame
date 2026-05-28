/// <summary>
/// Referencia liviana a un AudioSource activo del pool. Permite detener sonidos
/// en loop antes de que terminen naturalmente (ej. aura de habilidad al cancelar).
///
/// Uso:
///   AudioHandle h = audio.PlayAtPoint(cue, pos);
///   audio.Stop(h);
///
/// AudioHandle.Null representa un handle invalido (no hay sonido activo).
/// </summary>
public struct AudioHandle
{
    public static readonly AudioHandle Null = default;

    internal UnityEngine.AudioSource Source;

    public bool IsValid  => Source != null && Source.isPlaying;
    public bool IsNull   => Source == null;
}
