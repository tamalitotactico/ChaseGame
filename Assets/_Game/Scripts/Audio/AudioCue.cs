using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Dato de un sonido: uno o varios clips (variacion aleatoria), volumen, pitch y
/// al mixer group al que pertenece. Crear un asset por sonido o por familia de sonidos.
///
/// Crear: click derecho en Project → ChaseGame / Audio / AudioCue.
/// Asignar en AbilityData.sfxOnCast, sfxOnAimStart, o en cualquier campo AudioCue.
/// </summary>
[CreateAssetMenu(fileName = "AudioCue", menuName = "ChaseGame/Audio/AudioCue")]
public class AudioCue : ScriptableObject
{
    [Tooltip("Clips posibles. Se elige uno al azar cada reproduccion para variacion natural.")]
    public AudioClip[] clips;

    [Range(0f, 1f)]
    [Tooltip("Volumen de reproduccion (0 = silencio, 1 = maxima amplitud del clip).")]
    public float volume = 1f;

    [Tooltip("Pitch minimo. Con pitchMin != pitchMax cada reproduccion suena ligeramente distinta.")]
    public float pitchMin = 0.95f;
    [Tooltip("Pitch maximo.")]
    public float pitchMax = 1.05f;

    [Tooltip("Bus de destino (SFX, Music, Ambient). Arrastra el grupo del AudioMixer aqui.")]
    public AudioMixerGroup mixerGroup;

    [Tooltip("Si true, la fuente hace loop hasta que se detenga explicitamente via AudioHandle.")]
    public bool loop;

    [Range(0, 256)]
    [Tooltip("Prioridad de la fuente (0 = maxima, 256 = minima). Afecta que sonidos se roban del pool.")]
    public int priority = 128;

    [Header("Espacial (opcional)")]
    [Tooltip("Si true, el panning L/R y el volumen se atenuan segun distancia al AudioListener (camara/player). " +
             "Usar para SFX de impacto, pasos, explosiones. Dejar false para UI y musica.")]
    public bool spatial = false;

    [Tooltip("Distancia desde la que el sonido suena a volumen maximo. Solo aplica si spatial = true.")]
    public float minDistance = 1f;

    [Tooltip("Distancia a la que el sonido ya no se escucha. Solo aplica si spatial = true.")]
    public float maxDistance = 15f;
}
