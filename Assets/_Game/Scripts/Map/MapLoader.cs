using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Localiza y expone los puntos de spawn del mapa al inicio de la escena.
/// </summary>
public class MapLoader : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Se rellenan automaticamente en Awake buscando por tag.")]
    [SerializeField] private List<Transform> playerSpawnPoints = new();
    [SerializeField] private Transform hunterSpawnPoint;

    private void Awake()
    {
        CollectSpawnPoints();
    }

    void CollectSpawnPoints()
    {
        playerSpawnPoints.Clear();

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("SpawnPoint_Player"))
            playerSpawnPoints.Add(go.transform);

        // Tag "SpawnPoint_Pursuer" conservado por compatibilidad con escenas existentes;
        // semánticamente representa al Hunter spawn.
        GameObject hunterGo = GameObject.FindWithTag("SpawnPoint_Pursuer");
        if (hunterGo != null)
            hunterSpawnPoint = hunterGo.transform;

        if (playerSpawnPoints.Count == 0)
            Debug.LogWarning("[MapLoader] No se encontraron SpawnPoint_Player en la escena.");
        if (hunterSpawnPoint == null)
            Debug.LogWarning("[MapLoader] No se encontro SpawnPoint_Pursuer (Hunter spawn) en la escena.");
    }

    /// <summary>Retorna un SpawnPoint_Player aleatorio.</summary>
    public Vector3 GetPlayerSpawn()
    {
        if (playerSpawnPoints.Count == 0) return Vector3.zero;
        return playerSpawnPoints[Random.Range(0, playerSpawnPoints.Count)].position;
    }

    /// <summary>Retorna la posicion del SpawnPoint del Hunter (tag legacy "SpawnPoint_Pursuer").</summary>
    public Vector3 GetHunterSpawn()
    {
        return hunterSpawnPoint != null ? hunterSpawnPoint.position : Vector3.zero;
    }

    public int PlayerSpawnCount => playerSpawnPoints.Count;
}
