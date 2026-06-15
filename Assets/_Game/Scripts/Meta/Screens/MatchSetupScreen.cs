using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pantalla de configuracion de partida (post Hub). Una sola pantalla parametrizada por
/// MatchConfig.Mode (Solo vs Multiplayer):
///   - Toggle de rol Hunter/Prey (con preview del personaje equipado).
///   - Steppers +/- para cuantos hunters y cuantos preys (clamp a min/max serializados).
///   - Area "Buscando jugadores..." visible solo en Multiplayer (placeholder de la sala de Fusion).
///   - Boton iniciar: Solo = "INICIAR"; Multiplayer = "INICIAR / LLENAR CON BOTS" (host).
///
/// Al iniciar escribe la composicion en MatchConfig y carga la escena de juego. GameManager
/// la lee en Start y la IMatchSession decide que slot es humano/bot. La logica de red (Fusion)
/// solo cambiaria la sesion registrada y llenaria el area MP; esta pantalla no cambia.
/// </summary>
public class MatchSetupScreen : ScreenController
{
    public override string ScreenId => "MatchSetup";

    [Header("Rol")]
    [SerializeField] Image avatarImage;
    [SerializeField] TMP_Text roleLabel;
    [SerializeField] Button toggleRoleButton;

    [Header("Composicion - Hunters")]
    [SerializeField] Button huntersMinusButton;
    [SerializeField] Button huntersPlusButton;
    [SerializeField] TMP_Text huntersCountLabel;

    [Header("Composicion - Preys")]
    [SerializeField] Button preysMinusButton;
    [SerializeField] Button preysPlusButton;
    [SerializeField] TMP_Text preysCountLabel;

    [Header("Limites (tuneables)")]
    [SerializeField] int minHunters = 1;
    [SerializeField] int maxHunters = 4;
    [SerializeField] int minPreys = 1;
    [SerializeField] int maxPreys = 8;

    [Header("Multijugador")]
    [Tooltip("Raiz del area 'Buscando jugadores...'; se activa solo en modo Multiplayer.")]
    [SerializeField] GameObject multiplayerArea;

    [Header("Iniciar / Navegacion")]
    [SerializeField] Button startButton;
    [SerializeField] TMP_Text startLabel;
    [SerializeField] Button backButton;

    [Header("Partida")]
    [Tooltip("Escena de juego a cargar. Mismo valor que usaba HubScreen.")]
    [SerializeField] string gameplaySceneName = "00_Sandbox_TilesetTEST";

    CharacterTeam _role = CharacterTeam.Prey;
    int _hunters = 1;
    int _preys   = 4;

    void Awake()
    {
        if (toggleRoleButton != null)  toggleRoleButton.onClick.AddListener(ToggleRole);
        if (huntersMinusButton != null) huntersMinusButton.onClick.AddListener(() => StepHunters(-1));
        if (huntersPlusButton != null)  huntersPlusButton.onClick.AddListener(() => StepHunters(+1));
        if (preysMinusButton != null)   preysMinusButton.onClick.AddListener(() => StepPreys(-1));
        if (preysPlusButton != null)    preysPlusButton.onClick.AddListener(() => StepPreys(+1));
        if (startButton != null)        startButton.onClick.AddListener(StartMatch);
        if (backButton != null)         backButton.onClick.AddListener(() => Screens?.Back());
    }

    public override void OnShow()
    {
        // Sembrar desde MatchConfig (el Hub fijo Mode y PlayerTeam; los conteos persisten entre visitas).
        _role    = MatchConfig.PlayerTeam;
        _hunters = Mathf.Clamp(MatchConfig.HuntersTotal, minHunters, maxHunters);
        _preys   = Mathf.Clamp(MatchConfig.PreysTotal,   minPreys,   maxPreys);

        bool multiplayer = MatchConfig.Mode == MatchConfig.PlayMode.Multiplayer;
        if (multiplayerArea != null) multiplayerArea.SetActive(multiplayer);
        if (startLabel != null) startLabel.text = multiplayer ? "INICIAR / LLENAR CON BOTS" : "INICIAR";

        RefreshAvatar();
        RefreshCounts();
    }

    void ToggleRole()
    {
        _role = _role == CharacterTeam.Hunter ? CharacterTeam.Prey : CharacterTeam.Hunter;
        RefreshAvatar();
    }

    void StepHunters(int delta)
    {
        _hunters = Mathf.Clamp(_hunters + delta, minHunters, maxHunters);
        RefreshCounts();
    }

    void StepPreys(int delta)
    {
        _preys = Mathf.Clamp(_preys + delta, minPreys, maxPreys);
        RefreshCounts();
    }

    // Mismo patron que HubScreen.RefreshAvatar: skin.playablePreview -> character.splash -> character.icon.
    void RefreshAvatar()
    {
        if (roleLabel != null) roleLabel.text = _role == CharacterTeam.Hunter ? "HUNTER" : "PREY";
        if (Profile == null || avatarImage == null) return;

        var c = Profile.GetEquippedCharacter(_role);
        var skin = Profile.GetEquippedSkin(_role);
        Sprite s = (skin != null && skin.playablePreview != null) ? skin.playablePreview
                 : (c != null && c.splash != null) ? c.splash
                 : (c != null ? c.icon : null);
        avatarImage.sprite = s;
        avatarImage.enabled = s != null;
    }

    void RefreshCounts()
    {
        if (huntersCountLabel != null) huntersCountLabel.text = _hunters.ToString();
        if (preysCountLabel != null)   preysCountLabel.text   = _preys.ToString();
        if (huntersMinusButton != null) huntersMinusButton.interactable = _hunters > minHunters;
        if (huntersPlusButton != null)  huntersPlusButton.interactable  = _hunters < maxHunters;
        if (preysMinusButton != null)   preysMinusButton.interactable   = _preys > minPreys;
        if (preysPlusButton != null)    preysPlusButton.interactable    = _preys < maxPreys;
    }

    void StartMatch()
    {
        if (string.IsNullOrEmpty(gameplaySceneName)) return;
        // Escribe la composicion elegida; GameManager la lee en Start (Configured=true) y la
        // IMatchSession decide humano local vs bots ("llenar con bots" en host).
        MatchConfig.PlayerTeam   = _role;
        MatchConfig.HuntersTotal = _hunters;
        MatchConfig.PreysTotal   = _preys;
        MatchConfig.Configured   = true;
        SceneManager.LoadScene(gameplaySceneName);
    }
}
