using UnityEngine;

/// <summary>
/// Raiz persistente de la app (DontDestroyOnLoad). Vive en la escena Meta y SOBREVIVE el
/// cambio Meta&lt;-&gt;Gameplay. Registra los servicios meta que deben persistir cross-scene
/// (IProfileService), para que el loadout equipado este disponible cuando GameManager
/// spawnea en la escena de partida.
///
/// Idempotente: al recargar la escena Meta, el AppRoot duplicado se autodestruye.
/// Execution order temprano para registrar el servicio antes de que el Hub/GameManager lo usen.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class AppRoot : MonoBehaviour
{
    public static AppRoot Instance { get; private set; }

    [Tooltip("Catalogo maestro (personajes, skins, emotes, modos). Asignar el MetaCatalog.asset.")]
    [SerializeField] MetaCatalog catalog;

    public MetaCatalog Catalog => catalog;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (ServiceLocator.Resolve<IProfileService>() == null)
        {
            if (catalog == null)
                Debug.LogError("[AppRoot] MetaCatalog no asignado: el meta-layer no podra resolver personajes.");
            ServiceLocator.Register<IProfileService>(new ProfileService(catalog));
        }
    }
}
