using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla Customize. Hostea 3 sub-vistas (Hunters grid, Preys grid, Emotes) con una sidebar de
/// pestanas. Cambiar de pestana NO toca el back-stack (solo conmuta sub-vistas); "Atras" hace
/// IScreenService.Back() al Hub. Abre en Hunters. Tocar una tarjeta deja la seleccion en
/// MetaSelection y navega a CharacterDetail (sin referencia directa a esa pantalla).
/// </summary>
public class CustomizeScreen : ScreenController
{
    public override string ScreenId => "Customize";

    [Header("Sidebar")]
    [SerializeField] Button huntersTab;
    [SerializeField] Button preysTab;
    [SerializeField] Button emotesTab;
    [SerializeField] Button backButton;

    [Header("Sub-vistas")]
    [SerializeField] GameObject huntersView;
    [SerializeField] GameObject preysView;
    [SerializeField] GameObject emotesViewGo;
    [SerializeField] CharacterGridView huntersGrid;
    [SerializeField] CharacterGridView preysGrid;
    [SerializeField] EmotesView emotesView;

    enum Tab { Hunters, Preys, Emotes }
    Tab _tab = Tab.Hunters;

    void Awake()
    {
        if (huntersTab != null) huntersTab.onClick.AddListener(() => SetTab(Tab.Hunters));
        if (preysTab != null)   preysTab.onClick.AddListener(() => SetTab(Tab.Preys));
        if (emotesTab != null)  emotesTab.onClick.AddListener(() => SetTab(Tab.Emotes));
        if (backButton != null) backButton.onClick.AddListener(() => Screens?.Back());

        if (huntersGrid != null) { huntersGrid.Role = CharacterTeam.Hunter; huntersGrid.Init(OnCardSelected); }
        if (preysGrid != null)   { preysGrid.Role = CharacterTeam.Prey;     preysGrid.Init(OnCardSelected); }
    }

    public override void OnShow() => SetTab(_tab);

    void SetTab(Tab tab)
    {
        _tab = tab;
        if (huntersView != null)  huntersView.SetActive(tab == Tab.Hunters);
        if (preysView != null)    preysView.SetActive(tab == Tab.Preys);
        if (emotesViewGo != null) emotesViewGo.SetActive(tab == Tab.Emotes);

        switch (tab)
        {
            case Tab.Hunters: if (huntersGrid != null) huntersGrid.Refresh(); break;
            case Tab.Preys:   if (preysGrid != null)   preysGrid.Refresh();   break;
            case Tab.Emotes:  if (emotesView != null)  emotesView.Refresh();  break;
        }
    }

    void OnCardSelected(MetaCharacter c)
    {
        if (c == null) return;
        MetaSelection.Set(c.role, c.id);
        Screens?.Show("CharacterDetail");
    }
}
