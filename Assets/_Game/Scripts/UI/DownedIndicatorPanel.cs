using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Subpanel del HUD que muestra el estado de cada Prey: alive (icono normal),
/// downed (icono atenuado + barra de bleed-out), dead (icono X).
///
/// Tambien muestra un prompt central "Reviving teammate [progress]" cuando el
/// player local (Prey) esta contribuyendo al revive de algun teammate.
///
/// Asignar slots desde inspector — uno por slot esperado de Prey (en Phase 1,
/// 2 slots). El mapeo prey→slot se hace por orden de CharacterSpawnedEvent.
/// </summary>
public class DownedIndicatorPanel : MonoBehaviour
{
    [System.Serializable]
    public struct Slot
    {
        public GameObject       root;
        public Image            icon;
        public GameObject       deadOverlay;   // X mark
        public Slider           bleedOutBar;
        public Slider           reviveProgressBar;
        public TextMeshProUGUI  label;
    }

    [Header("Prey slots")]
    [SerializeField] Slot[] slots = new Slot[3];

    [Header("Colors")]
    [SerializeField] Color colorAlive  = Color.white;
    [SerializeField] Color colorDowned = new Color(1f, 0.6f, 0.2f);
    [SerializeField] Color colorDead   = new Color(0.4f, 0.4f, 0.4f);

    [Header("Revive prompt (player local)")]
    [SerializeField] GameObject       revivePromptRoot;
    [SerializeField] Slider           revivePromptBar;
    [SerializeField] TextMeshProUGUI  revivePromptText;

    readonly Dictionary<Character, int> _preyToSlot = new();
    readonly List<Character> _preys = new();
    Character _localPlayer; // si el player local es Prey, contributora a revives

    void OnEnable()
    {
        EventBus.Subscribe<CharacterSpawnedEvent>(OnSpawned);
        EventBus.Subscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Subscribe<CharacterRevivedEvent>(OnRevived);
        EventBus.Subscribe<CharacterDiedEvent>(OnDied);
        EventBus.Subscribe<ReviveProgressChangedEvent>(OnReviveProgress);

        for (int i = 0; i < slots.Length; i++) SetSlotAlive(i, false); // ocultar hasta spawn

        if (revivePromptRoot != null) revivePromptRoot.SetActive(false);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSpawnedEvent>(OnSpawned);
        EventBus.Unsubscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Unsubscribe<CharacterRevivedEvent>(OnRevived);
        EventBus.Unsubscribe<CharacterDiedEvent>(OnDied);
        EventBus.Unsubscribe<ReviveProgressChangedEvent>(OnReviveProgress);
    }

    void OnSpawned(CharacterSpawnedEvent e)
    {
        if (e.Character == null) return;
        if (e.Character.GetComponent<PlayerBrain>() != null && e.Character.Team == CharacterTeam.Prey)
            _localPlayer = e.Character;

        if (e.Character.Team != CharacterTeam.Prey) return;
        int idx = _preys.Count;
        if (idx >= slots.Length) return;
        _preys.Add(e.Character);
        _preyToSlot[e.Character] = idx;
        SetSlotAlive(idx, true);
        SetIconColor(idx, colorAlive);
        if (slots[idx].label != null) slots[idx].label.text = e.Character.Data != null ? e.Character.Data.displayName : "Prey";
    }

    void OnDowned(CharacterDownedEvent e)
    {
        if (e.Character == null || !_preyToSlot.TryGetValue(e.Character, out int idx)) return;
        SetIconColor(idx, colorDowned);
        if (slots[idx].bleedOutBar != null)
        {
            slots[idx].bleedOutBar.gameObject.SetActive(true);
            slots[idx].bleedOutBar.value = 1f;
        }
        if (slots[idx].reviveProgressBar != null)
        {
            slots[idx].reviveProgressBar.gameObject.SetActive(true);
            slots[idx].reviveProgressBar.value = 0f;
        }
    }

    void OnRevived(CharacterRevivedEvent e)
    {
        if (e.Character == null || !_preyToSlot.TryGetValue(e.Character, out int idx)) return;
        SetIconColor(idx, colorAlive);
        if (slots[idx].bleedOutBar != null)        slots[idx].bleedOutBar.gameObject.SetActive(false);
        if (slots[idx].reviveProgressBar != null)  slots[idx].reviveProgressBar.gameObject.SetActive(false);
        HidePromptIf(e.Character);
    }

    void OnDied(CharacterDiedEvent e)
    {
        if (e.Character == null || !_preyToSlot.TryGetValue(e.Character, out int idx)) return;
        SetIconColor(idx, colorDead);
        if (slots[idx].deadOverlay != null)        slots[idx].deadOverlay.SetActive(true);
        if (slots[idx].bleedOutBar != null)        slots[idx].bleedOutBar.gameObject.SetActive(false);
        if (slots[idx].reviveProgressBar != null)  slots[idx].reviveProgressBar.gameObject.SetActive(false);
        HidePromptIf(e.Character);
    }

    void OnReviveProgress(ReviveProgressChangedEvent e)
    {
        if (e.Character == null || !_preyToSlot.TryGetValue(e.Character, out int idx)) return;
        var rev = e.Character.Revivable;
        if (slots[idx].bleedOutBar != null && rev != null && rev.BleedOutDuration > 0f)
            slots[idx].bleedOutBar.value = Mathf.Clamp01(e.BleedOutRemaining / rev.BleedOutDuration);
        if (slots[idx].reviveProgressBar != null)
            slots[idx].reviveProgressBar.value = e.Progress;

        // Prompt si el player local Prey esta contribuyendo (en proximidad)
        if (_localPlayer != null && _localPlayer.IsAlive && rev != null && e.HasReviver)
        {
            float dist = Vector2.Distance(_localPlayer.transform.position, e.Character.transform.position);
            if (dist <= rev.ReviveProximityRadius)
            {
                ShowPrompt(e.Character, e.Progress, rev.ReviveDuration);
                return;
            }
        }
        HidePromptIf(e.Character);
    }

    void SetSlotAlive(int idx, bool visible)
    {
        if (idx < 0 || idx >= slots.Length) return;
        if (slots[idx].root != null) slots[idx].root.SetActive(visible);
        if (slots[idx].deadOverlay != null) slots[idx].deadOverlay.SetActive(false);
        if (slots[idx].bleedOutBar != null) slots[idx].bleedOutBar.gameObject.SetActive(false);
        if (slots[idx].reviveProgressBar != null) slots[idx].reviveProgressBar.gameObject.SetActive(false);
    }

    void SetIconColor(int idx, Color c)
    {
        if (idx < 0 || idx >= slots.Length) return;
        if (slots[idx].icon != null) slots[idx].icon.color = c;
    }

    Character _promptTarget;
    void ShowPrompt(Character target, float progress, float duration)
    {
        _promptTarget = target;
        if (revivePromptRoot != null) revivePromptRoot.SetActive(true);
        if (revivePromptBar  != null) revivePromptBar.value = progress;
        if (revivePromptText != null)
            revivePromptText.text = $"Reviving teammate [{progress * duration:F1}/{duration:F1}s]";
    }

    void HidePromptIf(Character target)
    {
        if (_promptTarget != target && target != null) return;
        _promptTarget = null;
        if (revivePromptRoot != null) revivePromptRoot.SetActive(false);
    }
}
