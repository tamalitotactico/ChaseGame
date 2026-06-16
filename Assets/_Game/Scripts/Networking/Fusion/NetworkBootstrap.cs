#if FUSION2
using System.Threading.Tasks;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

/// <summary>
/// Arranca el NetworkRunner para una partida de red (Host Mode). Lo crea/posee GameManager cuando
/// MatchConfig.Mode == Multiplayer. El modo Solo NO usa esto todavia (sigue local); cuando el Character
/// sea NetworkObject se unificara a GameMode.Single.
///
/// Hito 1: SessionName fijo (sala unica) + AutoHostOrClient (la primera instancia es host y llena con
/// bots, las demas son clientes). El lobby/matchmaking real reemplazara esto en un hito posterior,
/// enganchado al area "Buscando jugadores..." de MatchSetupScreen.
///
/// La partida corre en la escena ya cargada (00_InGame): no se hace additive-load (Scene invalida).
/// </summary>
public sealed class NetworkBootstrap : MonoBehaviour
{
    public const string DefaultRoom = "ChaseGame-Hito1";

    /// <summary>
    /// Region de Photon Cloud. Vacio = "Best Region" (cada peer puede caer en region distinta y no
    /// verse). Fijada a "sa" (Sudamerica/Sao Paulo) por defecto para que el host y el cliente coincidan
    /// en las pruebas. Cambiar a "us"/"eu"/etc. segun donde esten los jugadores.
    /// Alternativa equivalente en editor: Fusion Hub > PhotonAppSettings > FixedRegion.
    /// </summary>
    public static string FixedRegion = "sa";

    public NetworkRunner Runner { get; private set; }
    public bool IsRunning => Runner != null && Runner.IsRunning;

    /// <summary>Arranca el runner. gameMode: Host/Client/AutoHostOrClient/Single.</summary>
    public async Task StartNetwork(GameMode gameMode, string sessionName = DefaultRoom)
    {
        if (Runner != null) return;

        // Region fija (si se configuro): garantiza que todos los peers caigan en la misma region.
        // Si FixedRegion esta vacio, Photon usa "Best Region" (cada peer puede elegir distinta).
        // Equivale a setear Fusion Hub > PhotonAppSettings > FixedRegion, pero por sesion.
        if (!string.IsNullOrEmpty(FixedRegion))
        {
            var settings = Fusion.Photon.Realtime.PhotonAppSettings.Global;
            if (settings != null && settings.AppSettings != null)
                settings.AppSettings.FixedRegion = FixedRegion;
        }

        Runner = gameObject.AddComponent<NetworkRunner>();
        Runner.ProvideInput = true; // este peer aporta input local

        // Recolector de input local (PlayerBrain -> NetworkInputData).
        var collector = gameObject.GetComponent<FusionInputCollector>() ?? gameObject.AddComponent<FusionInputCollector>();
        Runner.AddCallbacks(collector);

        var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Simulacion de fisica 2D dentro del tick de Fusion (necesaria para colision de muros con
        // NetworkRigidbody2D). Pone Physics2D en modo Script mientras el runner corre.
        if (gameObject.GetComponent<RunnerSimulatePhysics2D>() == null)
            gameObject.AddComponent<RunnerSimulatePhysics2D>();

        var args = new StartGameArgs
        {
            GameMode     = gameMode,
            SessionName  = sessionName,
            SceneManager = sceneManager,
            // Scene por defecto (invalida) -> corre en la escena ya cargada, sin additive-load.
        };

        var result = await Runner.StartGame(args);
        if (!result.Ok)
            Debug.LogError($"[NetworkBootstrap] StartGame fallo: {result.ShutdownReason}");
        else
            Debug.Log($"[NetworkBootstrap] Runner iniciado: mode={gameMode} room={sessionName} isServer={Runner.IsServer}");
    }
}
#endif
