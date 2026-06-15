using UnityEngine;

/// <summary>
/// Emote a nivel de CUENTA (no por personaje). El jugador equipa 3 en una rueda radial.
/// </summary>
[CreateAssetMenu(fileName = "Emote", menuName = "ChaseGame/Meta/Emote")]
public class EmoteData : ScriptableObject
{
    public string id;
    public string displayName;

    [Tooltip("Frame estatico. Se usa SIEMPRE en el menu (grid/slots) y como fallback de la burbuja " +
             "si no hay animacion. Obligatorio (si dejas animFrames sin icon, OnValidate copia el frame 0).")]
    public Sprite icon;

    [Header("Animacion (opcional)")]
    [Tooltip("Si tiene frames, la burbuja in-game los reproduce como flipbook. Vacio = emote estatico.")]
    public Sprite[] animFrames;
    [Tooltip("Frames por segundo de la animacion.")]
    public float frameRate = 12f;
    [Tooltip("Repetir en loop mientras dure la burbuja.")]
    public bool loop = true;

    [Tooltip("Sonido opcional, se reproduce 1 sola vez al usar el emote.")]
    public AudioClip sound;
    public Rarity rarity = Rarity.Comun;

    /// <summary>True si el emote tiene una secuencia de frames para reproducir en la burbuja.</summary>
    public bool IsAnimated => animFrames != null && animFrames.Length > 0;

#if UNITY_EDITOR
    void OnValidate()
    {
        // El menu siempre necesita un icono estatico: si solo se asignaron frames, usa el primero.
        if (icon == null && IsAnimated && animFrames[0] != null) icon = animFrames[0];
        if (frameRate < 1f) frameRate = 1f;
    }
#endif
}
