using UnityEngine;

/// <summary>
/// Emote a nivel de CUENTA (no por personaje). El jugador equipa 3 en una rueda radial.
/// </summary>
[CreateAssetMenu(fileName = "Emote", menuName = "ChaseGame/Meta/Emote")]
public class EmoteData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public Rarity rarity = Rarity.Comun;
}
