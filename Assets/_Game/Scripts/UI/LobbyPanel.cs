using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de lobby (en la misma escena). Se muestra al entrar a LobbyState (via
/// LobbyEnteredEvent) y permite elegir rol. Al elegir, llama GameManager.StartMatch.
///
/// Escalable: para agregar seleccion de personaje/modo, sumar mas botones/listas aqui
/// y pasar la info a GameManager.StartMatch / MatchConfig sin tocar el flujo del match.
/// </summary>
public class LobbyPanel : MonoBehaviour
{
    [Tooltip("Raiz del panel que se muestra/oculta. Empieza inactivo en la escena.")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] Button hunterButton;
    [SerializeField] Button preyButton;

    void Awake()
    {
        if (hunterButton != null) hunterButton.onClick.AddListener(() => Choose(CharacterTeam.Hunter));
        if (preyButton   != null) preyButton.onClick.AddListener(()   => Choose(CharacterTeam.Prey));
    }

    void OnEnable()
    {
        EventBus.Subscribe<LobbyEnteredEvent>(OnLobbyEntered);
        EventBus.Subscribe<LobbyExitedEvent>(OnLobbyExited);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<LobbyEnteredEvent>(OnLobbyEntered);
        EventBus.Unsubscribe<LobbyExitedEvent>(OnLobbyExited);
    }

    void OnLobbyEntered(LobbyEnteredEvent _) => Show(true);
    void OnLobbyExited(LobbyExitedEvent _)   => Show(false);

    void Show(bool visible)
    {
        if (panelRoot != null) panelRoot.SetActive(visible);
    }

    void Choose(CharacterTeam team)
    {
        Show(false);
        if (GameManager.Instance != null) GameManager.Instance.StartMatch(team);
    }
}
