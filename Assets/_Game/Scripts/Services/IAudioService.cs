using UnityEngine;

/// <summary>
/// Servicio de audio del juego. Registrado en ServiceLocator por AudioManager.
/// Usar via: ServiceLocator.Resolve&lt;IAudioService&gt;()?.PlayAtPoint(cue, pos);
///
/// El operador ?. es intencional: si no hay AudioManager en la escena el juego
/// sigue funcionando sin audio (util en escenas de prueba sin setup completo).
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Reproduce un AudioCue una sola vez en una posicion del mundo.
    /// Selecciona un clip aleatorio del cue para variacion natural.
    /// </summary>
    AudioHandle PlayAtPoint(AudioCue cue, Vector2 worldPosition);

    /// <summary>
    /// Detiene un sonido obtenido por PlayAtPoint antes de que termine.
    /// Util para loops (cue.loop = true) adjuntos a habilidades o efectos.
    /// </summary>
    void Stop(AudioHandle handle);

    /// <summary>
    /// Controla el volumen de un bus del AudioMixer via su Exposed Parameter.
    /// normalizedVolume: 0 (silencio) a 1 (maximo). La conversion a dB es interna.
    /// Nombres de parametro del mixer: "Vol_Master", "Vol_SFX", "Vol_Music", "Vol_Ambient".
    /// </summary>
    void SetBusVolume(string exposedParam, float normalizedVolume);
}
