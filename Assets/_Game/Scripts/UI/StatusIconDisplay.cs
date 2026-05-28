using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra iconos de estado activos en un World Space Canvas encima del personaje.
/// Se suscribe directamente a StatusEffectController — sin acoplamiento con CharacterVisuals.
///
/// Prefab setup (ver plan):
///   StatusIcons (raiz, este componente)
///     └── Canvas (WorldSpace, PixelPerUnit=100, scale=0.01)
///           └── IconRow (HorizontalLayoutGroup + ContentSizeFitter)
/// Asignar IconRow a iconContainer en Inspector.
/// </summary>
public class StatusIconDisplay : MonoBehaviour
{
    [Header("Container")]
    [Tooltip("RectTransform con HorizontalLayoutGroup donde se insertan los iconos.")]
    [SerializeField] RectTransform iconContainer;
    [Tooltip("Tamano de cada icono en pixeles (el Canvas usa PixelPerUnit=100).")]
    [SerializeField] float iconSize = 40f;

    [Header("Sprites por efecto")]
    [SerializeField] Sprite stunIcon;
    [SerializeField] Sprite slowIcon;
    [SerializeField] Sprite fearIcon;
    [SerializeField] Sprite hasteIcon;

    StatusEffectController _fx;
    readonly Dictionary<System.Type, GameObject> _icons = new Dictionary<System.Type, GameObject>();

    void Awake()
    {
        _fx = GetComponentInParent<StatusEffectController>();
    }

    void OnEnable()
    {
        if (_fx != null)
        {
            _fx.OnEffectApplied += Add;
            _fx.OnEffectRemoved += Remove;
        }
    }

    void OnDisable()
    {
        if (_fx != null)
        {
            _fx.OnEffectApplied -= Add;
            _fx.OnEffectRemoved -= Remove;
        }
    }

    // Mantiene el Canvas orientado hacia la camara independientemente de la rotacion del padre.
    void LateUpdate() => transform.rotation = Quaternion.identity;

    void Add(StatusEffect e)
    {
        var t = e.GetType();
        if (_icons.ContainsKey(t)) return;  // refresh de duracion: no duplicar

        var sprite = SpriteFor(e);
        if (sprite == null) return;

        var go = new GameObject(t.Name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(iconContainer, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
        go.GetComponent<Image>().sprite = sprite;
        _icons[t] = go;
    }

    void Remove(StatusEffect e)
    {
        var t = e.GetType();
        if (_icons.TryGetValue(t, out var go))
        {
            Destroy(go);
            _icons.Remove(t);
        }
    }

    Sprite SpriteFor(StatusEffect e) => e switch
    {
        StunnedEffect _ => stunIcon,
        SlowedEffect  _ => slowIcon,
        FearedEffect  _ => fearIcon,
        HastedEffect  _ => hasteIcon,
        _               => null
    };
}
