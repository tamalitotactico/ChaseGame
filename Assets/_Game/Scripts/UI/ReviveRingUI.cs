using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aro radial (Image Radial360) sobre un Prey que muestra el progreso de resurrección.
///
/// Suscribe al EventBus directamente — no requiere cambios en CharacterVisuals.
/// El Character padre se detecta via GetComponentInParent, por lo que el GameObject
/// puede estar en cualquier nivel de la jerarquía del prefab.
///
/// VFX:
///   revivingFXPrefab — loop Attach mientras hay un reviver en rango.
///   revivedFXPrefab  — PlayOnce en la posición del personaje al completar el revive.
///
/// Setup en prefab:
///   ReviveRing (este script)
///   └── Canvas (World Space, PixelPerUnit=100, scale=0.01)
///       └── ringRoot
///           └── ringFill  (Image, Filled, Radial360, Fill Origin=Top, clockwise)
/// </summary>
public class ReviveRingUI : MonoBehaviour
{
    [Header("Ring UI")]
    [SerializeField] GameObject ringRoot;
    [SerializeField] Image      ringFill;

    [Header("VFX")]
    [SerializeField] GameObject revivingFXPrefab;
    [SerializeField] GameObject revivedFXPrefab;

    Character _char;
    VFXHandle _revivingFX;
    bool      _hasReviver;

    void Awake()
    {
        _char = GetComponentInParent<Character>();
        if (ringRoot != null) ringRoot.SetActive(false);
    }

    void LateUpdate() => transform.rotation = Quaternion.identity;

    void OnEnable()
    {
        EventBus.Subscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Subscribe<ReviveProgressChangedEvent>(OnReviveProgress);
        EventBus.Subscribe<CharacterRevivedEvent>(OnRevived);
        EventBus.Subscribe<CharacterDiedEvent>(OnDied);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Unsubscribe<ReviveProgressChangedEvent>(OnReviveProgress);
        EventBus.Unsubscribe<CharacterRevivedEvent>(OnRevived);
        EventBus.Unsubscribe<CharacterDiedEvent>(OnDied);
        StopRevivingFX();
    }

    void OnDowned(CharacterDownedEvent e)
    {
        if (e.Character != _char) return;
        _hasReviver = false;
        if (ringFill != null) ringFill.fillAmount = 0f;
        if (ringRoot != null) ringRoot.SetActive(true);
    }

    void OnReviveProgress(ReviveProgressChangedEvent e)
    {
        if (e.Character != _char) return;
        if (ringFill != null) ringFill.fillAmount = e.Progress;

        if (e.HasReviver && !_hasReviver)
        {
            StopRevivingFX();
            if (revivingFXPrefab != null)
                _revivingFX = VFXSpawner.Attach(revivingFXPrefab, transform);
        }
        else if (!e.HasReviver && _hasReviver)
        {
            StopRevivingFX();
        }
        _hasReviver = e.HasReviver;
    }

    void OnRevived(CharacterRevivedEvent e)
    {
        if (e.Character != _char) return;
        StopRevivingFX();
        if (ringRoot != null) ringRoot.SetActive(false);
        if (revivedFXPrefab != null && _char != null)
            VFXSpawner.PlayOnce(revivedFXPrefab, _char.transform.position);
    }

    void OnDied(CharacterDiedEvent e)
    {
        if (e.Character != _char) return;
        StopRevivingFX();
        if (ringRoot != null) ringRoot.SetActive(false);
    }

    void StopRevivingFX()
    {
        _revivingFX?.Stop();
        _revivingFX = null;
    }
}
