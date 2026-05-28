using UnityEngine;

/// <summary>
/// Partida en curso. Tick del timer + chequeo de condiciones de victoria.
/// Hunters ganan cuando ningun Prey esta vivo - los downed cuentan como derrota
/// (DeadState esta desactivado: los Prey no mueren, solo quedan downed). Preys
/// ganan si el timer expira y al menos uno sigue vivo.
/// </summary>
public class PlayingState : IMatchState
{
    public void Enter(GameManager gm)
    {
        gm.TimeRemaining = gm.Settings != null ? gm.Settings.matchDuration : 60f;
        EventBus.Publish(new MatchStartedEvent());

        var music = ServiceLocator.Resolve<IMusicService>();
        if (music == null)
            Debug.LogError("[PlayingState] No hay IMusicService registrado. Falta el GameObject MusicManager en la escena.");
        else if (gm.GameplayMusic == null)
            Debug.LogWarning("[PlayingState] GameManager.GameplayMusic = null. Asignalo en el Inspector del GameManager.");
        else
            music.PlayMusic(gm.GameplayMusic);
    }

    public void Tick(GameManager gm, float dt)
    {
        gm.TimeRemaining -= dt;
        EventBus.Publish(new MatchTimerTickEvent { SecondsRemaining = Mathf.Max(0f, gm.TimeRemaining) });

        // Chequeo: hunters ganan si todos los preys estan downed (ninguno vivo).
        // Con DeadState desactivado, el downed dura indefinidamente - pero la
        // partida termina apenas todos esten derribados, sin esperar revive.
        if (gm.AlivePreysCount == 0 && gm.Preys.Count > 0)
        {
            gm.States.ChangeState(new EndingState(CharacterTeam.Hunter, "All preys downed"));
            return;
        }

        // Chequeo: preys ganan si timer expiro
        if (gm.TimeRemaining <= 0f)
        {
            gm.States.ChangeState(new EndingState(CharacterTeam.Prey, "Time expired"));
            return;
        }
    }

    public void Exit(GameManager gm)
    {
        ServiceLocator.Resolve<IMusicService>()?.StopMusic();
    }
}
