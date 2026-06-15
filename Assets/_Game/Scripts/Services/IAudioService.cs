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
    /// Reproduce un AudioCue que SIGUE a un Transform: el source se reposiciona sobre
    /// el objeto cada frame mientras dura el clip. Para sonidos emitidos por el hunter,
    /// proyectil u otro objeto en movimiento. El source es del pool y NO se parenta, asi
    /// que destruir el objeto no corrompe el pool; si el objeto desaparece, el sonido se detiene.
    /// </summary>
    AudioHandle PlayAttached(AudioCue cue, Transform follow);

    /// <summary>
    /// Reproduce un AudioClip suelto (sin AudioCue). Va al bus SFX por defecto.
    /// spatial=true: sigue al Transform y atenua por distancia oyente-emisor (con spread alto
    /// para no panear duro al caminar). spatial=false: 2D centrado, se oye normal sin paneo
    /// (para el sonido "propio", ej. tu emote). Para emisores remotos usar spatial=true.
    /// </summary>
    AudioHandle PlayAttached(AudioClip clip, Transform follow, float volume = 1f, bool spatial = true);

    /// <summary>
    /// Reproduce un AudioCue en 2D GLOBAL (spatialBlend=0): se escucha en todo el mapa al
    /// mismo volumen, ignorando cue.spatial. Para taunts/anuncios que todos deben oir
    /// (ej. la risa del hunter al derribar un prey).
    /// </summary>
    AudioHandle PlayGlobal(AudioCue cue);

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
