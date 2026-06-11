/// <summary>
/// Navegacion entre PANTALLAS del meta (no entre pestanas internas de una pantalla).
/// Maneja un back-stack. Las pantallas se registran solas; nadie referencia a otra pantalla
/// directamente: piden transiciones por aqui (resuelto via ServiceLocator). Meta-scene-scoped.
/// </summary>
public interface IScreenService
{
    string Current { get; }

    void Register(ScreenController screen);
    void Unregister(ScreenController screen);

    /// <summary>Transicion push: apila la actual y muestra la pedida.</summary>
    void Show(string screenId);

    /// <summary>Pop del back-stack: vuelve a la anterior.</summary>
    void Back();

    /// <summary>Limpia el back-stack y fija una nueva raiz (ej. volver al Hub desde resultados).</summary>
    void ReplaceRoot(string screenId);
}
