using UnityEngine;

/// <summary>
/// Servicio de musica del juego. Registrado en ServiceLocator por MusicManager.
/// Usar via: ServiceLocator.Resolve&lt;IMusicService&gt;()?.PlayMusic(cue);
///
/// El operador ?. es intencional: si no hay MusicManager en la escena el juego
/// sigue funcionando sin musica.
/// </summary>
public interface IMusicService
{
    /// <summary>
    /// Reproduce una cancion de musica. Si hay musica actual, hace transicion suave.
    /// </summary>
    void PlayMusic(AudioCue cue, float fadeDuration = 1f);

    /// <summary>
    /// Detiene la musica actual con fade out.
    /// </summary>
    void StopMusic(float fadeDuration = 1f);

    /// <summary>
    /// Pausa la musica sin detenerla (puede reanudarse con ResumeMusic).
    /// </summary>
    void PauseMusic();

    /// <summary>
    /// Reanuda la musica pausada.
    /// </summary>
    void ResumeMusic();

    /// <summary>
    /// Obtiene si hay musica reproduciendo actualmente.
    /// </summary>
    bool IsPlaying { get; }
}
