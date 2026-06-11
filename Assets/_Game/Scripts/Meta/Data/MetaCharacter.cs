using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entrada de personaje del META-juego (grid, detalle, loadout). ENVUELVE el
/// CharacterData de gameplay (stats/abilities) en vez de reemplazarlo: las habilidades
/// y stats viven en gameplayData; aqui solo el meta (rareza, titulo, skins, arte demostrativo).
///
/// Se llama MetaCharacter (no Character) porque 'Character' ya es el MonoBehaviour de gameplay.
///
/// Placeholders locked: pueden tener gameplayData = null (aun no jugables); igual aparecen
/// silueteados en el grid usando 'role' para clasificarlos en Hunters vs Preys.
/// </summary>
[CreateAssetMenu(fileName = "MetaCharacter", menuName = "ChaseGame/Meta/Character")]
public class MetaCharacter : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [Tooltip("Subtitulo/titulo, ej. 'Amo de los Cuervos'.")]
    public string title;
    [Tooltip("Clasifica el personaje en el grid de Hunters o Preys. El rol en partida se asigna al azar.")]
    public CharacterTeam role;
    public Rarity rarity = Rarity.Comun;

    [Header("Gameplay (null para placeholders aun no jugables)")]
    [Tooltip("Datos de gameplay envueltos. El meta-layer NO los reemplaza.")]
    public CharacterData gameplayData;

    [Header("Arte demostrativo (grid / detalle)")]
    public Sprite splash;
    public Sprite icon;

    [Header("Skins (la primera es la default)")]
    public List<Skin> skins = new();

    public Skin DefaultSkin => (skins != null && skins.Count > 0) ? skins[0] : null;

    public Skin GetSkin(string skinId)
    {
        if (skins == null) return null;
        for (int i = 0; i < skins.Count; i++)
            if (skins[i] != null && skins[i].id == skinId) return skins[i];
        return null;
    }
}
