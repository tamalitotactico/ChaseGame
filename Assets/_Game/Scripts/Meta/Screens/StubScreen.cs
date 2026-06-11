using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla stub: solo entra/sale de la vista (titulo + boton atras -> pantalla anterior).
/// Usada para Shop y Chest en Phase 2 (contenido real en Phase 7). El ScreenId se asigna
/// en el inspector para reutilizar este componente en ambas.
/// </summary>
public class StubScreen : ScreenController
{
    [SerializeField] string screenId = "Shop";
    [SerializeField] Button backButton;

    public override string ScreenId => screenId;

    void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(() => Screens?.Back());
    }
}
