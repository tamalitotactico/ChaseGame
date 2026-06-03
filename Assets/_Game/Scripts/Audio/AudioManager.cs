using System.Collections.Generic;
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

    // Cache del AudioListener. En 2D la camara (listener) vive en z=-10; si el source
    // se coloca en z=0 la distancia 3D la domina ese offset y el audio espacial se
    // atenua parejo sin importar la posicion XY. Colocamos el source en la z del listener
    // para que la distancia sea puramente XY (player <-> fuente).
    Transform _listener;
    Transform Listener
    {
        get
        {
            if (_listener == null)
            {
                var l = FindFirstObjectByType<AudioListener>();
                if (l != null) _listener = l.transform;
            }
            return _listener;
        }
    }

    // Sonidos que siguen a un Transform cada frame (PlayAttached). El source es del pool
    // y NO se parenta (parentarlo lo destruiria si el objeto se destruye), asi que aqui
    // guardamos el target y reposicionamos el source en LateUpdate.
    readonly Dictionary<AudioSource, Transform> _attached = new();
    readonly List<AudioSource> _attachedCleanup = new();

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
        // Solo desregistrar si seguimos siendo el servicio activo. Tras recargar la escena
        // el nuevo AudioManager ya se registro en su Awake; quitarlo dejaria sin audio.
        if (ReferenceEquals(ServiceLocator.Resolve<IAudioService>(), this))
            ServiceLocator.Unregister<IAudioService>();
    }

    // --- IAudioService ---

    public AudioHandle PlayAtPoint(AudioCue cue, Vector2 worldPosition)
    {
        if (!IsValidCue(cue))
            return AudioHandle.Null;

        var src = GetFreeSource();
        // z del listener para que el audio espacial mida distancia XY pura (ver nota arriba).
        float z = Listener != null ? Listener.position.z : 0f;
        src.transform.position = new Vector3(worldPosition.x, worldPosition.y, z);
        ApplyCue(src, cue);
        src.Play();
        return new AudioHandle { Source = src };
    }

    public AudioHandle PlayAttached(AudioCue cue, Transform follow)
    {
        if (!IsValidCue(cue) || follow == null)
            return AudioHandle.Null;

        var src = GetFreeSource();
        float z = Listener != null ? Listener.position.z : 0f;
        var p = follow.position;
        src.transform.position = new Vector3(p.x, p.y, z);
        ApplyCue(src, cue);
        src.Play();
        _attached[src] = follow; // seguir cada frame en LateUpdate
        return new AudioHandle { Source = src };
    }

    public AudioHandle PlayGlobal(AudioCue cue)
    {
        if (!IsValidCue(cue))
            return AudioHandle.Null;

        var src = GetFreeSource();
        ApplyCue(src, cue);
        src.spatialBlend = 0f; // forzar 2D: mismo volumen en todo el mapa, ignora cue.spatial
        src.Play();
        return new AudioHandle { Source = src };
    }

    void LateUpdate()
    {
        if (_attached.Count == 0) return;
        float z = Listener != null ? Listener.position.z : 0f;

        _attachedCleanup.Clear();
        foreach (var kv in _attached)
        {
            var src = kv.Key;
            var tgt = kv.Value;
            if (src == null || !src.isPlaying) { _attachedCleanup.Add(src); continue; }
            if (tgt == null) { src.Stop(); _attachedCleanup.Add(src); continue; } // objeto destruido
            var p = tgt.position;
            src.transform.position = new Vector3(p.x, p.y, z);
        }
        for (int i = 0; i < _attachedCleanup.Count; i++)
            _attached.Remove(_attachedCleanup[i]);
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
        AudioSource chosen = null;
        // Busqueda circular: primer slot libre.
        for (int i = 0; i < _pool.Length; i++)
        {
            int idx = (_nextSlot + i) % _pool.Length;
            if (!_pool[idx].isPlaying)
            {
                _nextSlot = (idx + 1) % _pool.Length;
                chosen = _pool[idx];
                break;
            }
        }
        if (chosen == null)
        {
            // Pool lleno: robar el slot mas antiguo (siguiente en el circulo).
            chosen = _pool[_nextSlot];
            chosen.Stop();
            _nextSlot = (_nextSlot + 1) % _pool.Length;
        }
        // Si el source venia siguiendo a un objeto (PlayAttached), dejar de seguirlo al reutilizarlo.
        if (_attached.Count > 0) _attached.Remove(chosen);
        return chosen;
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
