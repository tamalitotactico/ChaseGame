using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD de slots de habilidad. Bindea con el AbilityController del jugador local
/// y refresca cooldown radial + texto de segundos via OnCooldownChanged.
/// Soporta botones tactiles que llaman a PlayerBrain.OnAbilityButton(Down|Up).
///
/// El indicador de canalización es responsabilidad de CastingBarUI (componente separado).
///
/// Estructura esperada por slot en el prefab:
///   root (GameObject activo/inactivo segun si hay habilidad)
///   ├── icon            (Image) — sprite de la habilidad, asignado en Bind
///   ├── cooldownFill    (Image, Filled Radial360) — fill 0 = CD lleno, 1 = listo
///   ├── cooldownOverlay (GameObject) — panel oscuro visible solo en cooldown
///   ├── cooldownText    (TextMeshProUGUI) — segundos restantes, oculto cuando listo
///   └── keyLabel        (TextMeshProUGUI) — tecla: Q / E / R
/// </summary>
public class AbilityHUD : MonoBehaviour
{
    [System.Serializable]
    public struct Slot
    {
        public GameObject       root;
        public Image            icon;            // sprite de la habilidad
        public Image            cooldownFill;    // Image type Filled, Radial360
        public GameObject       cooldownOverlay; // panel oscuro activo en cooldown
        public TextMeshProUGUI  cooldownText;    // segundos restantes
        public TextMeshProUGUI  keyLabel;        // Q / E / R
    }

    [SerializeField] Slot[] slots = new Slot[3];
    [SerializeField] string[] keyLabels = { "Q", "E", "R" };

    Character         _player;
    AbilityController _controller;

    void OnEnable()
    {
        EventBus.Subscribe<CharacterSpawnedEvent>(OnSpawned);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSpawnedEvent>(OnSpawned);
        Unbind();
    }

    void OnSpawned(CharacterSpawnedEvent e)
    {
        if (e.Character == null) return;
        if (e.Character.GetComponent<PlayerBrain>() == null) return;
        Bind(e.Character);
    }

    void Bind(Character player)
    {
        Unbind();
        _player     = player;
        _controller = player.Abilities;
        if (_controller != null)
            _controller.OnCooldownChanged += OnCooldownChanged;

        int n = (player.Data != null && player.Data.abilities != null) ? player.Data.abilities.Length : 0;
        for (int i = 0; i < slots.Length; i++)
        {
            bool active = i < n;
            if (slots[i].root != null) slots[i].root.SetActive(active);

            if (!active) continue;

            if (slots[i].cooldownFill    != null) slots[i].cooldownFill.fillAmount = 1f;
            if (slots[i].cooldownOverlay != null) slots[i].cooldownOverlay.SetActive(false);
            if (slots[i].cooldownText    != null) slots[i].cooldownText.gameObject.SetActive(false);
            if (slots[i].keyLabel        != null && i < keyLabels.Length)
                slots[i].keyLabel.text = keyLabels[i];

            if (slots[i].icon != null && player.Data.abilities[i] != null)
                slots[i].icon.sprite = player.Data.abilities[i].icon;
        }
    }

    void Unbind()
    {
        if (_controller != null)
            _controller.OnCooldownChanged -= OnCooldownChanged;
        _controller = null;
        _player     = null;
    }

    void OnCooldownChanged(int slot, float fillAmount, float remaining)
    {
        if (slot < 0 || slot >= slots.Length) return;

        bool onCooldown = remaining > 0.05f;

        if (slots[slot].cooldownFill != null)
            slots[slot].cooldownFill.fillAmount = fillAmount;

        if (slots[slot].cooldownOverlay != null)
            slots[slot].cooldownOverlay.SetActive(onCooldown);

        if (slots[slot].cooldownText != null)
        {
            slots[slot].cooldownText.gameObject.SetActive(onCooldown);
            if (onCooldown)
                slots[slot].cooldownText.text = remaining < 10f
                    ? remaining.ToString("F1")
                    : Mathf.CeilToInt(remaining).ToString();
        }
    }

    // ----- Hooks para botones tactiles (asignar en EventTrigger PointerDown/Up) -----
    public void OnButtonDown(int slot)
    {
        if (_player == null) return;
        var pb = _player.GetComponent<PlayerBrain>();
        pb?.OnAbilityButtonDown(slot);
    }

    public void OnButtonUp(int slot)
    {
        if (_player == null) return;
        var pb = _player.GetComponent<PlayerBrain>();
        pb?.OnAbilityButtonUp(slot);
    }
}
