using UnityEngine;

/// <summary>
/// Reacciones de audio a nivel de partida (no de un objeto concreto). Vive en un GO
/// de la escena (junto a AudioManager/MusicManager). Escucha eventos del EventBus y
/// dispara sonidos globales.
///
/// Hoy: la risa del hunter cuando un prey es derribado se reproduce GLOBAL (todos la
/// oyen, sin importar la distancia) via IAudioService.PlayGlobal. La misma cue Laugh,
/// cuando se usa al colocar un remanente, suena localizada siguiendo al hunter (eso lo
/// maneja sfxOnCast del Remnant -> PlayAttached, no este componente).
/// </summary>
public class MatchAudioDirector : MonoBehaviour
{
    [Tooltip("Risa/taunt que suena GLOBAL cuando un prey es derribado. Asignar la cue Laugh.")]
    [SerializeField] AudioCue preyDownedLaugh;

    void OnEnable()  => EventBus.Subscribe<CharacterDownedEvent>(OnDowned);
    void OnDisable() => EventBus.Unsubscribe<CharacterDownedEvent>(OnDowned);

    void OnDowned(CharacterDownedEvent e)
    {
        if (preyDownedLaugh == null || e.Character == null) return;
        if (e.Character.Team != CharacterTeam.Prey) return;
        ServiceLocator.Resolve<IAudioService>()?.PlayGlobal(preyDownedLaugh);
    }
}
