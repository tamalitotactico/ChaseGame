using UnityEngine;

/// <summary>
/// Skin de un personaje. Las skins NO modifican gameplay: mismas habilidades/stats,
/// solo cambian el arte demostrativo y el set jugable (sprites + animacion).
///
/// El set jugable se aplica al hijo "Visual" del prefab al spawnear: se reemplaza el
/// RuntimeAnimatorController del Animator. Ver el cableado en GameManager.SpawnOne (Seg. 7).
/// </summary>
[CreateAssetMenu(fileName = "Skin", menuName = "ChaseGame/Meta/Skin")]
public class Skin : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [Tooltip("Rareza propia de la skin, independiente de la del personaje.")]
    public Rarity rarity = Rarity.Comun;

    [Header("Arte demostrativo")]
    [Tooltip("Splash/ilustracion mostrada en el detalle al seleccionar la skin.")]
    public Sprite splash;
    public Sprite icon;

    [Header("Set jugable (aplicado al hijo 'Visual' al spawnear)")]
    [Tooltip("Controller de animacion que reemplaza al del prefab. Si es null, se usa el del prefab.")]
    public RuntimeAnimatorController animatorController;
    [Tooltip("Sprite estatico para el recuadro de preview in-game del detalle.")]
    public Sprite playablePreview;
}
