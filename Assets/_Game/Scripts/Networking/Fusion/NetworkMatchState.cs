#if FUSION2
using Fusion;
using UnityEngine;

/// <summary>
/// Estado de partida REPLICADO (host-autoritativo): fase, timer, cuenta atras y ganador. El host lo
/// escribe (GameManager.WriteNetMatchState) y los clientes lo espejan (GameManager.MirrorNetworkedMatch)
/// para que el temporizador y el resultado sean IDENTICOS en todas las pantallas (antes cada peer corria
/// su propio reloj -> desincronizado, con riesgo de ganador divergente).
///
/// Lo spawnea el host una vez por partida (GameManager.SpawnMatchStateObject). Instance se setea en
/// Spawned en todos los peers.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkMatchState : NetworkBehaviour
{
    public static NetworkMatchState Instance { get; private set; }

    [Networked] public int   Phase           { get; set; } // 0 starting, 1 playing, 2 ending
    [Networked] public float TimeRemaining   { get; set; }
    [Networked] public int   CountdownSecond { get; set; }
    [Networked] public int   WinnerTeam      { get; set; } // (int)CharacterTeam

    public override void Spawned() => Instance = this;

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>HOST: vuelca el estado autoritativo de la partida.</summary>
    public void HostWrite(int phase, float timeRemaining, int countdownSecond, int winnerTeam)
    {
        if (Object == null || !Object.HasStateAuthority) return;
        Phase           = phase;
        TimeRemaining   = timeRemaining;
        CountdownSecond = countdownSecond;
        WinnerTeam      = winnerTeam;
    }
}
#endif
