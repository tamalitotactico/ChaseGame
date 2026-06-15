using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de canalizacion independiente. Se muestra solo cuando una habilidad
/// esta en fase de cast REAL (aimer.IsCasting=true). NO se muestra durante la
/// fase de aim previa (drag-aim), aunque la habilidad termine en canalizacion.
///
/// Estructura esperada en el prefab:
///   CastingBarUI (MonoBehaviour en raiz)
///   └── barRoot (GameObject — activar/desactivar)
///       ├── abilityIcon  (Image, lado izquierdo) — icono de la habilidad canalizando
///       ├── barFill      (Image, Filled, Horizontal, Fill Origin Left) — progreso 0..1
///       └── timerLabel   (TextMeshProUGUI, sobre barFill) — cuenta regresiva "1.0"
///
/// Se bindea automaticamente al jugador local via CharacterSpawnedEvent.
/// Funciona con cualquier Aimer cuya propiedad IsCasting pase a true en algun
/// punto (ej. AimThenCastAimer durante su fase 2).
/// </summary>
public class CastingBarUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] GameObject      barRoot;     // raiz que se activa/desactiva
    [SerializeField] Image           barFill;     // Image Filled, Horizontal
    [SerializeField] Image           abilityIcon; // icono de la habilidad (lado izquierdo)
    [SerializeField] TextMeshProUGUI timerLabel;  // texto de cuenta regresiva

    Character         _player;
    AbilityController _controller;

    void OnEnable()
    {
        EventBus.Subscribe<CharacterSpawnedEvent>(OnSpawned);
        // Pull de la ref retenida por si este binder se activo despues del spawn (ver PlayerBrain.Local).
        if (PlayerBrain.Local != null) Bind(PlayerBrain.Local.GetComponent<Character>());
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
        {
            _controller.OnCastingStarted += OnCastingStarted;
            _controller.OnCastingTick    += OnCastingTick;
            _controller.OnCastingStopped += OnCastingStopped;
        }

        // Asegurar que la barra comience oculta
        if (barRoot != null) barRoot.SetActive(false);
    }

    void Unbind()
    {
        if (_controller != null)
        {
            _controller.OnCastingStarted -= OnCastingStarted;
            _controller.OnCastingTick    -= OnCastingTick;
            _controller.OnCastingStopped -= OnCastingStopped;
        }
        _controller = null;
        _player     = null;
    }

    void OnCastingStarted(int slot)
    {
        var data = _controller?.GetAbilityData(slot);

        if (abilityIcon != null)
            abilityIcon.sprite = data != null ? data.icon : null;

        if (barFill != null)
            barFill.fillAmount = 0f;

        if (barRoot != null)
            barRoot.SetActive(true);
    }

    void OnCastingTick(float progress, float remainingSeconds)
    {
        if (barFill != null)
            barFill.fillAmount = progress;

        if (timerLabel != null)
            timerLabel.text = "Chaneling " + remainingSeconds.ToString("F1");
    }

    void OnCastingStopped()
    {
        if (barRoot != null)
            barRoot.SetActive(false);
    }
}
