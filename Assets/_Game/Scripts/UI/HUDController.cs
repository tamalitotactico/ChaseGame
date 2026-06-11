using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// HUD principal de partida. Lee eventos del EventBus para actualizarse;
/// solo identifica al jugador local detectando PlayerBrain en el spawned event.
///
/// Phase 3: en multi-cliente cada cliente solo bindea con su propio Character.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Health (jugador local)")]
    [SerializeField] GameObject[] heartIcons;

    [Header("Timer")]
    [SerializeField] TextMeshProUGUI timerText;

    [Header("Countdown")]
    [SerializeField] GameObject countdownPanel;
    [SerializeField] TextMeshProUGUI countdownText;

    [Header("Result")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TextMeshProUGUI resultText;
    [Tooltip("Boton para rejugar con la misma config (recarga la escena).")]
    [SerializeField] Button rematchButton;
    [Tooltip("Boton para volver al lobby (recarga la escena y muestra seleccion de rol).")]
    [SerializeField] Button lobbyButton;
    [Tooltip("Boton para salir de la partida y volver al Meta (Hub).")]
    [SerializeField] Button exitButton;
    [Tooltip("Nombre de la escena Meta a la que vuelve 'Salir'.")]
    [SerializeField] string metaSceneName = "00_Meta";

    Character _player;

    void Awake()
    {
        // Listeners una sola vez (no en OnEnable para no apilarlos).
        if (rematchButton != null) rematchButton.onClick.AddListener(() => GameManager.Instance?.Rematch());
        if (lobbyButton   != null) lobbyButton.onClick.AddListener(()   => GameManager.Instance?.ReturnToLobby());
        if (exitButton    != null) exitButton.onClick.AddListener(ExitToMeta);
    }

    void ExitToMeta()
    {
        if (!string.IsNullOrEmpty(metaSceneName)) SceneManager.LoadScene(metaSceneName);
    }

    void OnEnable()
    {
        EventBus.Subscribe<CharacterSpawnedEvent>(OnSpawned);
        EventBus.Subscribe<CharacterDamagedEvent>(OnDamaged);
        EventBus.Subscribe<MatchTimerTickEvent>(OnTimer);
        EventBus.Subscribe<CountdownTickEvent>(OnCountdown);
        EventBus.Subscribe<MatchStartedEvent>(OnMatchStarted);
        EventBus.Subscribe<MatchEndedEvent>(OnMatchEnded);
        EventBus.Subscribe<LobbyEnteredEvent>(OnLobbyEntered);

        if (resultPanel != null) resultPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSpawnedEvent>(OnSpawned);
        EventBus.Unsubscribe<CharacterDamagedEvent>(OnDamaged);
        EventBus.Unsubscribe<MatchTimerTickEvent>(OnTimer);
        EventBus.Unsubscribe<CountdownTickEvent>(OnCountdown);
        EventBus.Unsubscribe<MatchStartedEvent>(OnMatchStarted);
        EventBus.Unsubscribe<MatchEndedEvent>(OnMatchEnded);
        EventBus.Unsubscribe<LobbyEnteredEvent>(OnLobbyEntered);
    }

    // Reset in-place (rematch/lobby): el panel de resultado debe ocultarse al empezar
    // una nueva partida o al volver al lobby (no hay recarga de escena que lo resetee).
    void OnLobbyEntered(LobbyEnteredEvent _)
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
    }

    void OnSpawned(CharacterSpawnedEvent e)
    {
        if (e.Character == null) return;
        if (e.Character.GetComponent<PlayerBrain>() == null) return;
        _player = e.Character;
        UpdateHearts(_player.Health.CurrentHealth, _player.Health.MaxHealth);
    }

    void OnDamaged(CharacterDamagedEvent e)
    {
        if (e.Character != _player) return;
        UpdateHearts(e.CurrentHealth, e.MaxHealth);
    }

    void UpdateHearts(int current, int max)
    {
        if (heartIcons == null) return;
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] == null) continue;
            heartIcons[i].SetActive(i < current);
        }
    }

    void OnTimer(MatchTimerTickEvent e)
    {
        if (timerText == null) return;
        int total = Mathf.Max(0, Mathf.CeilToInt(e.SecondsRemaining));
        int m = total / 60;
        int s = total % 60;
        timerText.text = $"{m:00}:{s:00}";
    }

    void OnCountdown(CountdownTickEvent e)
    {
        // Nueva partida arrancando (incl. rematch in-place): ocultar el panel de resultado.
        if (resultPanel != null) resultPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(e.SecondsLeft > 0);
        if (countdownText != null) countdownText.text = e.SecondsLeft > 0 ? e.SecondsLeft.ToString() : "GO!";
    }

    void OnMatchStarted(MatchStartedEvent _)
    {
        if (countdownPanel != null) countdownPanel.SetActive(false);
    }

    void OnMatchEnded(MatchEndedEvent e)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText == null) return;
        bool playerWon = _player != null && _player.Team == e.WinningTeam;
        resultText.text = (playerWon ? "VICTORY" : "DEFEAT") + "\n" + e.Reason;
    }
}
