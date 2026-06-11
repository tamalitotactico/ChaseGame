using UnityEngine;

/// <summary>
/// Modo de juego del selector. Phase 2: solo Supervivencia es funcional; el resto se
/// marca isComingSoon. El "Modo del dia" rota a diario (determinista por fecha, ver GameModeScreen).
/// </summary>
[CreateAssetMenu(fileName = "GameMode", menuName = "ChaseGame/Meta/Game Mode")]
public class GameModeData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite splash;
    [Tooltip("Si true, se muestra como 'proximamente' y no es seleccionable/jugable.")]
    public bool isComingSoon;
}
