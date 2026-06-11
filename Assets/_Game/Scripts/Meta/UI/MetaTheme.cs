using UnityEngine;

/// <summary>
/// Paleta y sprites compartidos del meta-UI (Phase 2), afinados a los mockups (grayscale, tema
/// claro, esquinas redondeadas). Centralizado para que cards/slots construidos en runtime y los
/// paneles construidos por MCP usen los mismos valores. Arte final = Phase 6.
/// </summary>
public static class MetaTheme
{
    public static readonly Color Page         = new Color(0.78f, 0.78f, 0.80f, 1f);
    public static readonly Color Panel        = new Color(0.83f, 0.83f, 0.85f, 1f);
    public static readonly Color Card         = new Color(0.60f, 0.60f, 0.63f, 1f);
    public static readonly Color Button       = new Color(0.70f, 0.70f, 0.73f, 1f);
    public static readonly Color ButtonActive = new Color(0.52f, 0.54f, 0.60f, 1f);
    public static readonly Color TextDark     = new Color(0.13f, 0.13f, 0.15f, 1f);
    public static readonly Color Locked       = new Color(0.40f, 0.40f, 0.43f, 1f);
    public static readonly Color Accent       = new Color(0.20f, 0.60f, 0.32f, 1f); // CTA (READY/SELECT)

    static Sprite _rounded;
    static Sprite _circle;

    /// <summary>Sprite redondeado (9-slice) generado en Assets/_Game/Resources/MetaUI. Persistente,
    /// asi se serializa en escena (paneles MCP) y carga en runtime (cards).</summary>
    public static Sprite Rounded()
    {
        if (_rounded == null) _rounded = Resources.Load<Sprite>("MetaUI/rounded");
        return _rounded;
    }

    /// <summary>Sprite circular para slots de emote/habilidad e iconos de sidebar.</summary>
    public static Sprite Circle()
    {
        if (_circle == null) _circle = Resources.Load<Sprite>("MetaUI/circle");
        return _circle;
    }
}
