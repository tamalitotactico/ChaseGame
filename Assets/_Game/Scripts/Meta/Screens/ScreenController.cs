using UnityEngine;

/// <summary>
/// Base de toda pantalla del meta (Hub, Customize, CharacterDetail, GameMode, Shop, Chest).
/// El ScreenService la registra (por ScreenId) y controla su visibilidad. Las subclases
/// sobreescriben OnShow/OnHide para refrescar datos y suscribir/desuscribir eventos.
/// </summary>
public abstract class ScreenController : MonoBehaviour
{
    public abstract string ScreenId { get; }

    [Tooltip("Raiz visual a activar/desactivar. Si es null se usa este GameObject.")]
    [SerializeField] protected GameObject root;

    GameObject Root => root != null ? root : gameObject;

    protected IScreenService Screens => ServiceLocator.Resolve<IScreenService>();
    protected IProfileService Profile => ServiceLocator.Resolve<IProfileService>();

    public bool IsVisible => Root.activeSelf;

    public void SetVisible(bool visible)
    {
        if (visible) { Root.SetActive(true); OnShow(); }
        else         { OnHide(); Root.SetActive(false); }
    }

    /// <summary>Hook al mostrarse (refrescar UI, suscribir eventos). Base no hace nada.</summary>
    public virtual void OnShow() { }

    /// <summary>Hook al ocultarse (desuscribir). Base no hace nada.</summary>
    public virtual void OnHide() { }
}
