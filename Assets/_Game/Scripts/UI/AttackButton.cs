using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Boton tactil/mouse para el ataque basico del Hunter.
///
/// Comportamiento:
///  - Press → llama PlayerBrain.OnAttackButton() (queue 1-frame attack press)
///  - Auto-toggle de visibilidad por team: solo activo si el player local tiene
///    CharacterData.hasBasicAttack = true (i.e. Hunter; Prey lo oculta).
///  - Sin drag, sin aim — el ataque basico apunta hacia FacingDirection del
///    Character (lo maneja CombatController.Tick).
///  - Opcionalmente muestra cooldown radial si se asigna cooldownFill, usando
///    CombatController.AttackCooldownRemaining (si esta expuesto).
///
/// Setup en escena:
///   - GameObject UI con Image y este componente.
///   - Auto-suscripcion a CharacterSpawnedEvent — no hace falta wiring manual.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AttackButton : MonoBehaviour, IPointerDownHandler
{
    [Header("Optional cooldown overlay")]
    [Tooltip("Image Filled Radial360. Si se asigna, refleja el cooldown del attack.")]
    [SerializeField]
    Image cooldownFill;

    [Tooltip("Panel oscuro visible durante cooldown.")]
    [SerializeField]
    GameObject cooldownOverlay;

    PlayerBrain _pb;
    Character _player;
    CanvasGroup _cg;

    void OnEnable()
    {
        EventBus.Subscribe<CharacterSpawnedEvent>(OnSpawned);
        // Pull: si el jugador local ya existe (spawn ocurrido antes de habilitarse), re-evaluar ya.
        if (PlayerBrain.Local != null)
            OnSpawned(new CharacterSpawnedEvent { Character = PlayerBrain.Local.GetComponent<Character>() });
    }

    void OnDisable() => EventBus.Unsubscribe<CharacterSpawnedEvent>(OnSpawned);

    void OnSpawned(CharacterSpawnedEvent e)
    {
        if (e.Character == null)
            return;
        var pb = e.Character.GetComponent<PlayerBrain>();
        if (pb == null)
            return;

        _pb = pb;
        _player = e.Character;

        // Visibilidad por rol SIN desactivar el GameObject. Si hicieramos gameObject.SetActive(false)
        // para el Prey, este componente dejaria de escuchar CharacterSpawnedEvent y NADA lo volveria a
        // activar -> el boton de ataque quedaba muerto en todas las partidas siguientes. Con CanvasGroup
        // el GO sigue vivo y suscrito, y se re-evalua en cada spawn (Hunter lo muestra, Prey lo oculta).
        bool hasAttack = _player.Data != null && _player.Data.hasBasicAttack;
        SetShown(hasAttack);

        if (cooldownFill != null)
            cooldownFill.fillAmount = 1f;
        if (cooldownOverlay != null)
            cooldownOverlay.SetActive(false);
    }

    void SetShown(bool shown)
    {
        if (_cg == null)
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        }
        _cg.alpha = shown ? 1f : 0f;
        _cg.interactable = shown;
        _cg.blocksRaycasts = shown;
    }

    void Update()
    {
        if (_player == null || _player.Combat == null)
            return;
        if (cooldownFill == null && cooldownOverlay == null)
            return;

        // CombatController.Tick decrementa _cooldownTimer interno; lo aproximamos
        // via reflection-free chequeo: el CombatController expone AttackCooldown
        // total via Setup() y Combat.Tick chequea Time. Sin API publica, se usa
        // un best-effort: si CombatController expone CooldownRemaining/AttackCooldown
        // como propiedades en futuras versiones, aqui se conectan.
        // Por ahora, simplemente ocultar overlay (placeholder).
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (_pb != null)
            _pb.OnAttackButton();
    }
}
