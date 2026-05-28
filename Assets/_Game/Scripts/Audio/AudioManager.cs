using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Implementacion de IAudioService. Gestiona un pool fijo de AudioSources para
/// evitar instanciar GameObjects en runtime. Se auto-registra en ServiceLocator.
///
/// Setup en escena:
///   1. Crear un GameObject vacio "AudioManager" en la escena principal.
///   2. Adjuntar este componente.
///   3. Asignar el campo 'mixer' con el asset GameAudioMixer.mixer.
///   4. El pool se crea automaticamente en Awake.
///
/// Para agregar buses al AudioMixer:
///   - Abrir GameAudioMixer.mixer en Unity.
///   - Click derecho en Master → "Add child group" → nombrar SFX / Music / Ambient.
///   - En cada grupo: Inspector → click en el valor de Volume → "Expose to script".
///   - Nombrar los parametros: "Vol_Master", "Vol_SFX", "Vol_Music", "Vol_Ambient".
/// </summary>
public class AudioManager : MonoBehaviour, IAudioService
{
    [Tooltip("Asset del AudioMixer (GameAudioMixer.mixer). Necesario para SetBusVolume.")]
    [SerializeField]
    AudioMixer mixer;

    [Tooltip("Cantidad de AudioSources pre-instanciados. Aumentar si hay cortes de sonido.")]
    [SerializeField]
    int poolSize = 20;

    AudioSource[] _pool;
    int _nextSlot; // indice circular para busqueda de fuente libre

    void Awake()
    {
        _pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"PooledAudioSource_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _pool[i] = src;
        }

        ServiceLocator.Register<IAudioService>(this);
    }

    void OnDestroy()
    {
        ServiceLocator.Unregister<IAudioService>();
    }

    // --- IAudioService ---

    public AudioHandle PlayAtPoint(AudioCue cue, Vector2 worldPosition)
    {
        if (!IsValidCue(cue))
            return AudioHandle.Null;

        var src = GetFreeSource();
        src.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        ApplyCue(src, cue);
        src.Play();
        return new AudioHandle { Source = src };
    }

    public void Stop(AudioHandle handle)
    {
        if (!handle.IsNull)
            handle.Source.Stop();
    }

    public void SetBusVolume(string exposedParam, float normalizedVolume)
    {
        if (mixer == null)
            return;
        // AudioMixer espera dB logaritmicos. 0.0001 evita log(0) = -infinito.
        float db = normalizedVolume > 0.0001f ? Mathf.Log10(normalizedVolume) * 20f : -80f;
        mixer.SetFloat(exposedParam, db);
    }

    // --- Internos ---

    static bool IsValidCue(AudioCue cue) =>
        cue != null && cue.clips != null && cue.clips.Length > 0;

    AudioSource GetFreeSource()
    {
        // Busqueda circular: primer slot libre.
        for (int i = 0; i < _pool.Length; i++)
        {
            int idx = (_nextSlot + i) % _pool.Length;
            if (!_pool[idx].isPlaying)
            {
                _nextSlot = (idx + 1) % _pool.Length;
                return _pool[idx];
            }
        }
        // Pool lleno: robar el slot mas antiguo (siguiente en el circulo).
        var stolen = _pool[_nextSlot];
        stolen.Stop();
        _nextSlot = (_nextSlot + 1) % _pool.Length;
        return stolen;
    }

    static void ApplyCue(AudioSource src, AudioCue cue)
    {
        src.clip = cue.clips[Random.Range(0, cue.clips.Length)];
        src.volume = cue.volume;
        src.pitch = Random.Range(cue.pitchMin, cue.pitchMax);
        src.loop = cue.loop;
        src.priority = cue.priority;
        src.outputAudioMixerGroup = cue.mixerGroup;

        if (cue.spatial)
        {
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = cue.minDistance;
            src.maxDistance = cue.maxDistance;
        }
        else
        {
            src.spatialBlend = 0f;
        }
    }
}
