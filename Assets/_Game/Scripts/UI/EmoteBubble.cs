using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Burbuja de emote (globo de dialogo) world-space sobre un personaje/fantasma. La spawnea
/// EmoteBubblePresenter, que la posiciona segun la matriz de visibilidad. Sigue un transform
/// objetivo y se autodestruye al expirar.
///
/// Es un PREFAB editable (Assets/_Game/Prefabs/EmoteBubble.prefab): el cuerpo del globo, la colita,
/// la escala world y el offset se tunean en el inspector del prefab. Solo el icono se asigna en runtime.
/// </summary>
public class EmoteBubble : MonoBehaviour
{
    [Tooltip("Image donde se pone el icono del emote.")]
    [SerializeField] Image iconImage;
    [Tooltip("Offset world-space sobre el objetivo (altura del globo). El valor REAL vive en el prefab " +
             "(este default solo aplica a instancias creadas sin prefab).")]
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 2.21f, 0f);

    Transform _target;
    Vector3   _fallbackPos;
    float     _expire;

    public void Show(EmoteData data, Transform target, Vector3 fallbackPos, float duration)
    {
        _target = target;
        _fallbackPos = fallbackPos;

        Sprite icon = data != null ? data.icon : null;
        if (iconImage != null) { iconImage.sprite = icon; iconImage.enabled = icon != null; }

        // Animado: un flipbook sobre el mismo Image. Get-or-add porque la burbuja es efimera
        // (se instancia y se destruye); funciona este o no el componente en el prefab.
        if (iconImage != null && data != null && data.IsAnimated)
        {
            var seq = iconImage.GetComponent<UISpriteSequencePlayer>()
                      ?? iconImage.gameObject.AddComponent<UISpriteSequencePlayer>();
            seq.Play(data.animFrames, data.frameRate, data.loop);
        }

        // El sonido lo dispara EmoteBubblePresenter via IAudioService (espacial, atado al emisor).
        _expire = Time.time + Mathf.Max(0.1f, duration);
        UpdatePos();
        gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        UpdatePos();
        if (Time.time >= _expire) Destroy(gameObject);
    }

    void UpdatePos()
    {
        Vector3 basePos = _target != null ? _target.position : _fallbackPos;
        transform.position = basePos + worldOffset;
    }
}
