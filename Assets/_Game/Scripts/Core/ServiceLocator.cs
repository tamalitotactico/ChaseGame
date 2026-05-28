using System;
using System.Collections.Generic;

/// <summary>
/// Registro tipado para servicios cuya implementacion debe poder cambiar
/// (ISpawnService, IAuthorityContext, ITimeService, etc).
/// Phase 0 usa implementaciones locales; Phase 3 las swappea por las de Fusion
/// sin tocar codigo gameplay.
/// </summary>
public static class ServiceLocator
{
    static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public static T Resolve<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var s) ? s as T : null;
    }

    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    public static void Clear() => _services.Clear();
}
