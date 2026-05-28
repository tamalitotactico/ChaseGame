using System;
using System.Collections.Generic;

/// <summary>
/// Publish-subscribe estatico para comunicacion desacoplada entre sistemas.
/// En Phase 3 se pasara a una instancia scope-por-match (matchScopedBus) cuando
/// existan partidas concurrentes via Photon Fusion.
/// </summary>
public static class EventBus
{
    static readonly Dictionary<Type, Delegate> _handlers = new();

    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        if (_handlers.TryGetValue(typeof(T), out var existing))
            _handlers[typeof(T)] = (Action<T>)existing + handler;
        else
            _handlers[typeof(T)] = handler;
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        if (!_handlers.TryGetValue(typeof(T), out var existing)) return;
        var combined = (Action<T>)existing - handler;
        if (combined == null) _handlers.Remove(typeof(T));
        else _handlers[typeof(T)] = combined;
    }

    public static void Publish<T>(T evt) where T : struct
    {
        if (_handlers.TryGetValue(typeof(T), out var existing))
            ((Action<T>)existing)?.Invoke(evt);
    }

    public static void Clear() => _handlers.Clear();
}
