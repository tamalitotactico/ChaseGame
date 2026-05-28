using UnityEngine;

/// <summary>
/// Configuracion estatica de las reglas de partida. Independiente del modo;
/// los modos concretos (Survival, Collection) leeran este SO mas su propio data.
/// </summary>
[CreateAssetMenu(fileName = "MatchSettings", menuName = "ChaseGame/Data/Match Settings")]
public class MatchSettings : ScriptableObject
{
    [Header("Duracion")]
    [Tooltip("Duracion total de la partida en segundos.")]
    public float matchDuration = 60f;

    [Tooltip("Cuenta atras antes de comenzar.")]
    public float countdownDuration = 3f;

    [Header("Composicion objetivo")]
    public int huntersTarget = 1;
    public int preysTarget   = 3;

    [Header("Reglas")]
    public bool reviveAllowed = false;
    public int  maxRevivesPerMatch = 2;
}
