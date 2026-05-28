/// <summary>
/// Implementacion local-only de IAuthorityContext. Todo es siempre autoritativo
/// porque solo existe un cliente. Singleton estatico para evitar referencias.
/// </summary>
public sealed class LocalAuthority : IAuthorityContext
{
    public static readonly LocalAuthority Instance = new();

    public bool IsLocal     => true;
    public bool IsAuthority => true;
    public bool CanSimulate => true;

    LocalAuthority() { }
}
