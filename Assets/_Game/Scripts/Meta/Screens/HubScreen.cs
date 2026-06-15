using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla raiz del meta. Muestra el personaje equipado (con toggle Hunter/Prey), perfil,
/// monedas y los accesos (SHOP/CUSTOMIZE/CHEST), el selector de modo y JUGAR.
/// Solo se comunica por ServiceLocator (IProfileService/IScreenService) y EventBus.
/// </summary>
public class HubScreen : ScreenController
{
    public override string ScreenId => "Hub";

    [Header("Avatar")]
    [SerializeField] Image avatarImage;
    [SerializeField] TMP_Text roleLabel;
    [SerializeField] Button toggleRoleButton;

    [Header("Perfil")]
    [SerializeField] TMP_Text playerNameText;
    [SerializeField] TMP_Text coinsText;
    [SerializeField] TMP_Text gemsText;

    [Header("Modo")]
    [SerializeField] Button modeButton;
    [SerializeField] TMP_Text modeLabel;

    [Header("Navegacion")]
    [SerializeField] Button shopButton;
    [SerializeField] Button customizeButton;
    [SerializeField] Button chestButton;
    [Tooltip("JUGAR (Solo/Bots): arranque inmediato contra bots.")]
    [SerializeField] Button playButton;
    [Tooltip("MULTIJUGADOR: lleva a la sala de emparejamiento (host puede llenar con bots).")]
    [SerializeField] Button multiplayerButton;

    CharacterTeam _shownRole = CharacterTeam.Prey;

    void Awake()
    {
        if (toggleRoleButton != null) toggleRoleButton.onClick.AddListener(ToggleRole);
        if (shopButton != null)      shopButton.onClick.AddListener(() => Screens?.Show("Shop"));
        if (customizeButton != null) customizeButton.onClick.AddListener(() => Screens?.Show("Customize"));
        if (chestButton != null)     chestButton.onClick.AddListener(() => Screens?.Show("Chest"));
        if (modeButton != null)      modeButton.onClick.AddListener(() => Screens?.Show("GameMode"));
        if (playButton != null)        playButton.onClick.AddListener(() => OpenMatchSetup(MatchConfig.PlayMode.Solo));
        if (multiplayerButton != null) multiplayerButton.onClick.AddListener(() => OpenMatchSetup(MatchConfig.PlayMode.Multiplayer));
    }

    public override void OnShow()
    {
        EventBus.Subscribe<LoadoutChangedEvent>(OnLoadoutChanged);
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<GameModeSelectedEvent>(OnGameModeSelected);
        Refresh();
    }

    public override void OnHide()
    {
        EventBus.Unsubscribe<LoadoutChangedEvent>(OnLoadoutChanged);
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<GameModeSelectedEvent>(OnGameModeSelected);
    }

    void ToggleRole()
    {
        _shownRole = _shownRole == CharacterTeam.Hunter ? CharacterTeam.Prey : CharacterTeam.Hunter;
        RefreshAvatar();
    }

    void Refresh()
    {
        RefreshAvatar();
        RefreshCurrencies();
        RefreshMode();
        if (playerNameText != null && Profile != null) playerNameText.text = Profile.State.playerName;
    }

    void RefreshAvatar()
    {
        if (roleLabel != null) roleLabel.text = _shownRole == CharacterTeam.Hunter ? "HUNTER" : "PREY";
        if (Profile == null || avatarImage == null) return;

        var c = Profile.GetEquippedCharacter(_shownRole);
        var skin = Profile.GetEquippedSkin(_shownRole);
        Sprite s = (skin != null && skin.playablePreview != null) ? skin.playablePreview
                 : (c != null && c.splash != null) ? c.splash
                 : (c != null ? c.icon : null);
        avatarImage.sprite = s;
        avatarImage.enabled = s != null;
    }

    void RefreshCurrencies()
    {
        if (Profile == null) return;
        if (coinsText != null) coinsText.text = Profile.GetCurrency(CurrencyType.Coins).ToString();
        if (gemsText != null)  gemsText.text  = Profile.GetCurrency(CurrencyType.Gems).ToString();
    }

    void RefreshMode()
    {
        if (modeLabel == null) return;
        var mode = (Profile != null && Profile.Catalog != null) ? Profile.Catalog.GetGameMode(MatchConfig.SelectedModeId) : null;
        modeLabel.text = mode != null ? mode.displayName : "Supervivencia";
    }

    void OnLoadoutChanged(LoadoutChangedEvent e) { if (e.Role == _shownRole) RefreshAvatar(); }
    void OnCurrencyChanged(CurrencyChangedEvent e) => RefreshCurrencies();
    void OnGameModeSelected(GameModeSelectedEvent e) => RefreshMode();

    /// <summary>Abre MatchSetup para elegir composicion. Siembra el modo y el rol visible (el avatar
    /// del toggle) como valor inicial; MatchSetup permite cambiarlo y es quien carga la escena.</summary>
    void OpenMatchSetup(MatchConfig.PlayMode mode)
    {
        MatchConfig.Mode       = mode;
        MatchConfig.PlayerTeam = _shownRole;
        Screens?.Show("MatchSetup");
    }
}
