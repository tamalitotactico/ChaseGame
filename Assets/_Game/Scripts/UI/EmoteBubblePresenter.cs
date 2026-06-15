using UnityEngine;

/// <summary>
/// Escucha EmoteUsedEvent y muestra una burbuja en la posicion correcta segun la MATRIZ de
/// visibilidad (depende del estado del viewer LOCAL):
///   - Emote de un VIVO  -> sobre el jugador (lo ven todos).
///   - Emote de un FANTASMA -> los fantasmas lo ven sobre el fantasma; los vivos sobre el CUERPO.
/// Source.transform ES el cuerpo. Vive en la escena y referencia el prefab editable de la burbuja.
///
/// Local por ahora (solo emotea el jugador local, y el viewer es el local). Disenado para red: el
/// evento ya trae GhostPos/BodyPos como fallback para clientes sin el transform.
/// </summary>
public class EmoteBubblePresenter : MonoBehaviour
{
    [Tooltip("Prefab de la burbuja (Assets/_Game/Prefabs/EmoteBubble.prefab).")]
    [SerializeField] EmoteBubble bubblePrefab;
    [Tooltip("Duracion de la burbuja en segundos.")]
    [SerializeField] float duration = 2.5f;

    GhostModeController _ghostCtrl;

    void OnEnable()  => EventBus.Subscribe<EmoteUsedEvent>(OnEmote);
    void OnDisable() => EventBus.Unsubscribe<EmoteUsedEvent>(OnEmote);

    void OnEmote(EmoteUsedEvent e)
    {
        if (bubblePrefab == null || e.Source == null) return;

        var profile = ServiceLocator.Resolve<IProfileService>();
        var data = (profile != null && profile.Catalog != null) ? profile.Catalog.GetEmote(e.EmoteId) : null;

        if (_ghostCtrl == null) _ghostCtrl = Object.FindAnyObjectByType<GhostModeController>();
        bool localViewerGhost = _ghostCtrl != null && _ghostCtrl.IsGhostActive;

        Transform target;
        Vector3   fallback;
        if (!e.FromGhost)
        {
            target = e.Source.transform; fallback = e.BodyPos; // emote de vivo: sobre el jugador
        }
        else if (localViewerGhost && _ghostCtrl.GhostTransform != null)
        {
            target = _ghostCtrl.GhostTransform; fallback = e.GhostPos; // viewer fantasma: en el fantasma
        }
        else
        {
            target = e.Source.transform; fallback = e.BodyPos; // viewer vivo: en el cuerpo
        }

        var bubble = Instantiate(bubblePrefab);
        bubble.Show(data, target, fallback, duration);

        // Sonido del emote. El TUYO suena 2D centrado (lo oyes normal aunque camines: el
        // listener-camara se desfasa del emisor y panearia duro). El de OTROS suena espacial
        // (atenuado por distancia oyente-emisor).
        if (data != null && data.sound != null && target != null)
        {
            bool ownEmote = IsLocalSource(e.Source);
            ServiceLocator.Resolve<IAudioService>()?.PlayAttached(data.sound, target, 1f, spatial: !ownEmote);
        }
    }

    static bool IsLocalSource(Character source)
    {
        var local = PlayerBrain.Local;
        var localChar = local != null ? local.GetComponent<Character>() : null;
        return source != null && source == localChar;
    }
}
