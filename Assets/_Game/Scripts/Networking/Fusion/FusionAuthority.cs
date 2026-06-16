#if FUSION2
using Fusion;

/// <summary>
/// Implementacion de <see cref="IAuthorityContext"/> sobre un NetworkObject de Fusion (Host Mode).
/// Reemplaza a LocalAuthority en partidas de red. La asignacion se hace por Character.SetAuthority en
/// el spawn (ver GameManager).
///
/// Host Mode: el HOST tiene StateAuthority de TODOS los objetos (incl. bots) y es quien simula; los
/// clientes solo tienen InputAuthority de su propio personaje. Por eso CanSimulate == HasStateAuthority:
/// la simulacion autoritativa corre en el host. (Con el addon de fisica + prediccion, los clientes
/// resimulan localmente su personaje; eso es refinamiento de un hito posterior.)
/// </summary>
public sealed class FusionAuthority : IAuthorityContext
{
    readonly NetworkObject _obj;

    public FusionAuthority(NetworkObject obj) { _obj = obj; }

    public bool IsLocal     => _obj != null && _obj.HasInputAuthority;
    public bool IsAuthority => _obj != null && _obj.HasStateAuthority;
    public bool CanSimulate => _obj != null && _obj.HasStateAuthority;
}
#endif
