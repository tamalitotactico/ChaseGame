#if FUSION2
using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Recolecta el input del jugador LOCAL para Fusion. En OnInput (una vez por tick) toma el gesto del
/// PlayerBrain local (mismo CaptureIntent de siempre) y lo empaqueta en NetworkInputData. El Character
/// (NetworkBehaviour) lo leera en FixedUpdateNetwork via GetInput.
///
/// Implementa INetworkRunnerCallbacks completo (la mayoria vacios). Se registra con
/// runner.AddCallbacks(this) desde NetworkBootstrap.
///
/// Hito 1: solo se consume Move/Aim aguas abajo; el resto del intent ya viaja para hitos futuros.
/// </summary>
public sealed class FusionInputCollector : MonoBehaviour, INetworkRunnerCallbacks
{
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var brain = PlayerBrain.Local;
        if (brain == null) return;
        BrainIntent intent = brain.CaptureIntent();
        input.Set(NetworkInputData.FromIntent(in intent));
    }

    // Spawn host-only de personajes (lo decide GameManager).
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        GameManager.Instance?.OnNetworkPlayerJoined(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}
#endif
