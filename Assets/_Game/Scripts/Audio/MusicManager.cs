using System.Collections;
using UnityEngine;

/// <summary>
/// Implementacion de IMusicService. Reproduccion de musica de fondo con cross-fade
/// entre canciones usando dos AudioSources alternados.
///
/// Setup:
///   1. GameObject "MusicManager" en la escena principal con este componente.
///   2. El volumen global de musica se controla via AudioMixer (parametro expuesto
///      "Vol_Music"). El cue.volume actua como volumen relativo de cada cancion.
///
/// Para agregar musica nueva al juego:
///   1. Crear un AudioCue (loop = true, mixerGroup = Music).
///   2. Arrastrarlo a un campo AudioCue en GameManager o el script que lo use.
///   3. Llamar ServiceLocator.Resolve&lt;IMusicService&gt;()?.PlayMusic(cue) cuando corresponda.
/// </summary>
public class MusicManager : MonoBehaviour, IMusicService
{
    AudioSource _currentSource;
    AudioSource _nextSource;
    Coroutine   _fadeCoroutine;
    bool        _isPaused;

    void Awake()
    {
        _currentSource = CreateSource("MusicSource_Current");
        _nextSource    = CreateSource("MusicSource_Next");
        ServiceLocator.Register<IMusicService>(this);
    }

    void OnDestroy()
    {
        ServiceLocator.Unregister<IMusicService>();
    }

    AudioSource CreateSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop        = true;
        src.spatialBlend = 0f; // musica nunca es espacial
        return src;
    }

    // --- IMusicService ---

    public void PlayMusic(AudioCue cue, float fadeDuration = 1f)
    {
        if (cue == null)
        {
            Debug.LogWarning("[MusicManager] PlayMusic llamado con cue=null. Verifica los campos del GameManager.");
            return;
        }
        if (cue.clips == null || cue.clips.Length == 0)
        {
            Debug.LogWarning($"[MusicManager] AudioCue '{cue.name}' no tiene clips asignados.");
            return;
        }
        if (cue.volume <= 0f)
        {
            Debug.LogWarning($"[MusicManager] AudioCue '{cue.name}' tiene volume = 0. Subelo en el Inspector.");
            return;
        }

        // Si ya esta sonando este mismo cue, no hacer nada (evita reinicio innecesario).
        if (_currentSource.isPlaying && _currentSource.clip != null
            && System.Array.IndexOf(cue.clips, _currentSource.clip) >= 0)
            return;

        Debug.Log($"[MusicManager] Reproduciendo '{cue.name}' (vol={cue.volume}, fade={fadeDuration}s).");

        StopFadeCoroutine();

        if (_currentSource.isPlaying)
        {
            ApplyCue(_nextSource, cue);
            _nextSource.volume = 0f;
            _nextSource.Play();
            _fadeCoroutine = StartCoroutine(CrossFade(_currentSource, _nextSource, cue.volume, fadeDuration));
        }
        else
        {
            ApplyCue(_currentSource, cue);
            _currentSource.volume = 0f;
            _currentSource.Play();
            _fadeCoroutine = StartCoroutine(FadeIn(_currentSource, cue.volume, fadeDuration));
        }
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        if (!_currentSource.isPlaying) return;
        StopFadeCoroutine();
        _fadeCoroutine = StartCoroutine(FadeOut(_currentSource, fadeDuration));
    }

    public void PauseMusic()
    {
        if (!_currentSource.isPlaying || _isPaused) return;
        _currentSource.Pause();
        _isPaused = true;
    }

    public void ResumeMusic()
    {
        if (!_isPaused) return;
        _currentSource.UnPause();
        _isPaused = false;
    }

    public bool IsPlaying => _currentSource.isPlaying && !_isPaused;

    // --- Internos ---

    static bool IsValidCue(AudioCue cue) =>
        cue != null && cue.clips != null && cue.clips.Length > 0;

    // Consistente con AudioManager.ApplyCue: aplica los mismos campos del AudioCue.
    // Forzamos loop=true y spatialBlend=0 porque son invariantes de la musica.
    void ApplyCue(AudioSource src, AudioCue cue)
    {
        src.clip     = cue.clips[Random.Range(0, cue.clips.Length)];
        src.pitch    = Random.Range(cue.pitchMin, cue.pitchMax);
        src.priority = cue.priority;
        src.outputAudioMixerGroup = cue.mixerGroup;
        src.loop         = true;
        src.spatialBlend = 0f;
    }

    void StopFadeCoroutine()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }

    IEnumerator FadeIn(AudioSource source, float targetVolume, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }
        source.volume = targetVolume;
    }

    IEnumerator FadeOut(AudioSource source, float duration)
    {
        float elapsed = 0f;
        float startVolume = source.volume;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        source.volume = 0f;
        source.Stop();
    }

    IEnumerator CrossFade(AudioSource outSource, AudioSource inSource, float targetVolume, float duration)
    {
        float elapsed = 0f;
        float outStartVolume = outSource.volume;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            outSource.volume = Mathf.Lerp(outStartVolume, 0f, t);
            inSource.volume  = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }
        outSource.volume = 0f;
        outSource.Stop();
        inSource.volume = targetVolume;

        // Swap: el nuevo source es ahora "current".
        (_currentSource, _nextSource) = (_nextSource, _currentSource);
    }
}
