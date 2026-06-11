using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementacion de IScreenService. Descubre todas las ScreenController de la escena Meta
/// (incluye inactivas), las oculta y muestra la inicial. Mantiene un back-stack para Show/Back.
/// Meta-scene-scoped (no persiste cross-scene).
/// </summary>
[DefaultExecutionOrder(-500)]
public class ScreenService : MonoBehaviour, IScreenService
{
    [Tooltip("Pantalla mostrada al iniciar la escena Meta (ej. 'Hub').")]
    [SerializeField] string initialScreenId = "Hub";

    readonly Dictionary<string, ScreenController> _screens = new();
    readonly Stack<string> _stack = new();
    string _current;

    public string Current => _current;

    void Awake()
    {
        ServiceLocator.Register<IScreenService>(this);

        var all = FindObjectsByType<ScreenController>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++) Register(all[i]);
    }

    void Start()
    {
        foreach (var kv in _screens) kv.Value.SetVisible(false);
        _current = null;

        if (!string.IsNullOrEmpty(initialScreenId) && _screens.ContainsKey(initialScreenId))
            ShowInternal(initialScreenId);
        else
            Debug.LogWarning($"[ScreenService] initialScreenId '{initialScreenId}' no esta registrado.");
    }

    void OnDestroy()
    {
        if (ReferenceEquals(ServiceLocator.Resolve<IScreenService>(), this))
            ServiceLocator.Unregister<IScreenService>();
    }

    public void Register(ScreenController screen)
    {
        if (screen == null || string.IsNullOrEmpty(screen.ScreenId)) return;
        _screens[screen.ScreenId] = screen;
    }

    public void Unregister(ScreenController screen)
    {
        if (screen != null) _screens.Remove(screen.ScreenId);
    }

    public void Show(string screenId)
    {
        if (!_screens.ContainsKey(screenId)) { Warn("Show", screenId); return; }
        if (_current == screenId) return;
        if (!string.IsNullOrEmpty(_current)) _stack.Push(_current);
        ShowInternal(screenId);
    }

    public void Back()
    {
        if (_stack.Count == 0) return;
        ShowInternal(_stack.Pop());
    }

    public void ReplaceRoot(string screenId)
    {
        if (!_screens.ContainsKey(screenId)) { Warn("ReplaceRoot", screenId); return; }
        _stack.Clear();
        ShowInternal(screenId);
    }

    void ShowInternal(string screenId)
    {
        if (!string.IsNullOrEmpty(_current) && _screens.TryGetValue(_current, out var cur))
            cur.SetVisible(false);

        _current = screenId;
        _screens[screenId].SetVisible(true);
        EventBus.Publish(new ScreenChangedEvent { ScreenId = screenId });
    }

    static void Warn(string op, string id) =>
        Debug.LogWarning($"[ScreenService] {op}: pantalla '{id}' no registrada.");
}
