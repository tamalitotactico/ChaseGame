using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puntero reutilizable a un objetivo del mundo: baliza sobre el target si esta en pantalla, flecha
/// clampeada al borde si esta fuera. Se auto-construye un Canvas Screen-Space Overlay y se autodestruye
/// tras 'duration'. Generaliza GhostBodyPointer (que es el caso especifico fantasma->cuerpo).
///
/// Usuarios: Bear Trap (revela al hunter a los preys 1s); a futuro reveals de Remnant/Smell/mordisco.
/// Crear via WorldTargetPointer.Show(...). Estilo (sprite/tamano/color) lo pasa el llamador desde su
/// config serializada (SO), respetando la politica runtime-UI (no hardcode de tunables).
/// </summary>
// RUNTIME-UI-REVIEW: UI construida en runtime (sin prefab). Tunables (sprite/size/color/duration)
// vienen de la AbilityData del llamador. Ver docs/RuntimeUIReview.md.
[DisallowMultipleComponent]
public class WorldTargetPointer : MonoBehaviour
{
    float markerSize    = 44f;
    float edgeMargin    = 48f;
    float onScreenYOffset = 56f;
    int   sortingOrder  = 205;
    Color _color        = Color.white;

    Transform _target;
    float     _timer;
    Sprite    _sprite;

    Canvas        _canvas;
    RectTransform _rt;
    Image         _img;

    static Sprite _generatedArrow;

    /// <summary>Crea y muestra un puntero al target por 'duration' segundos. sprite null => triangulo
    /// generado. size &lt;= 0 => default.</summary>
    public static WorldTargetPointer Show(Transform target, float duration, Sprite sprite, float size, Color color)
    {
        if (target == null || duration <= 0f) return null;
        var go = new GameObject("WorldTargetPointer");
        var p = go.AddComponent<WorldTargetPointer>();
        p._target = target;
        p._timer  = duration;
        p._sprite = sprite;
        if (size > 0f) p.markerSize = size;
        p._color = color;
        p.Build();
        return p;
    }

    void Build()
    {
        var canvasGo = new GameObject("PointerCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;

        var markerGo = new GameObject("Marker", typeof(RectTransform));
        markerGo.transform.SetParent(canvasGo.transform, false);
        _rt = (RectTransform)markerGo.transform;
        _rt.anchorMin = Vector2.zero;
        _rt.anchorMax = Vector2.zero;
        _rt.pivot     = new Vector2(0.5f, 0.5f);
        _rt.sizeDelta = new Vector2(markerSize, markerSize);

        _img = markerGo.AddComponent<Image>();
        _img.sprite        = _sprite != null ? _sprite : GeneratedArrow();
        _img.color         = _color;
        _img.raycastTarget = false;
        _img.preserveAspect = true;
    }

    void LateUpdate()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f || _target == null) { Destroy(gameObject); return; }

        var cam = Camera.main;
        if (cam == null || _canvas == null) return;

        Vector3 sp = cam.WorldToScreenPoint(_target.position);
        bool behind = sp.z < 0f;
        if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }

        bool onScreen = !behind
            && sp.x >= edgeMargin && sp.x <= Screen.width - edgeMargin
            && sp.y >= edgeMargin && sp.y <= Screen.height - edgeMargin;

        Vector2 pos;
        float angle; // la flecha base apunta hacia +Y

        if (onScreen)
        {
            pos   = new Vector2(sp.x, sp.y + onScreenYOffset);
            angle = 180f; // baliza apuntando hacia abajo, al target
        }
        else
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = (Vector2)sp - center;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();

            float halfW = Screen.width * 0.5f - edgeMargin;
            float halfH = Screen.height * 0.5f - edgeMargin;
            float sx = Mathf.Abs(dir.x) < 1e-4f ? float.MaxValue : halfW / Mathf.Abs(dir.x);
            float sy = Mathf.Abs(dir.y) < 1e-4f ? float.MaxValue : halfH / Mathf.Abs(dir.y);
            pos   = center + dir * Mathf.Min(sx, sy);
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        }

        _rt.anchoredPosition = pos;
        _rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
    }

    // Triangulo solido apuntando a +Y (cacheado), igual que GhostBodyPointer.
    static Sprite GeneratedArrow()
    {
        if (_generatedArrow != null) return _generatedArrow;
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[N * N];
        const int SS = 2;
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            int inside = 0;
            for (int sy = 0; sy < SS; sy++)
            for (int sx = 0; sx < SS; sx++)
            {
                float u = (x + (sx + 0.5f) / SS) / N;
                float v = (y + (sy + 0.5f) / SS) / N;
                float halfW = (1f - v) * 0.5f;
                if (Mathf.Abs(u - 0.5f) <= halfW) inside++;
            }
            px[y * N + x] = new Color32(255, 255, 255, (byte)(255 * inside / (SS * SS)));
        }
        tex.SetPixels32(px);
        tex.Apply();
        _generatedArrow = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        _generatedArrow.name = "WTP_GeneratedArrow";
        return _generatedArrow;
    }
}
