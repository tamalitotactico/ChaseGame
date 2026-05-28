using UnityEngine;

/// <summary>
/// Valores tunneables de dificultad. Crear instancias en Assets/_Game/ScriptableObjects/Data/.
/// </summary>
[CreateAssetMenu(fileName = "DifficultySettings", menuName = "Chase Game/Difficulty Settings")]
public class DifficultySettings : ScriptableObject
{
    [Header("Speed")]
    [Tooltip("Velocidad del bot Pursuer.")]
    public float pursuerSpeed = 4f;

    [Tooltip("Velocidad del bot Survivor.")]
    public float survivorSpeed = 4.5f;

    [Header("Detection")]
    [Tooltip("Radio de deteccion del Pursuer.")]
    public float pursuerDetectionRadius = 6f;

    [Header("Timer")]
    [Tooltip("Duracion de la partida en segundos.")]
    public float survivalTime = 60f;

    [Header("Vision")]
    [Tooltip("Radio de vision del jugador cuando es Survivor.")]
    public float playerVisionRadius = 5f;

    [Tooltip("Radio de vision del jugador cuando es Pursuer.")]
    public float pursuerVisionRadius = 8f;
}
