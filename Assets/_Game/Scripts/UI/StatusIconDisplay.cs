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

    [System.Serializable]
    public struct IconEntry
    {
        [Tooltip("Id del efecto (StatusEffect.IconId): stun/slow/fear/haste/charm/blind/camo/invisible/immune/trueform.")]
        public string id;
        [Tooltip("Sprite a mostrar (puede ser un slice de IconSet1).")]
        public Sprite sprite;
    }

    [Header("Iconos por id de efecto")]
    [Tooltip("Mapa id->sprite. Cada StatusEffect declara su IconId; aqui se le asigna el arte. " +
             "Asignar los slices de IconSet1 a cada id en el inspector.")]
    [SerializeField] IconEntry[] icons;

    StatusEffectController _fx;
    readonly Dictionary<System.Type, GameObject> _icons = new Dictionary<System.Type, GameObject>();
    readonly Dictionary<string, Sprite> _byId = new Dictionary<string, Sprite>();

    void Awake()
    {
        _fx = GetComponentInParent<StatusEffectController>();
        if (icons != null)
            foreach (var e in icons)
                if (!string.IsNullOrEmpty(e.id)) _byId[e.id] = e.sprite;
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

    Sprite SpriteFor(StatusEffect e)
    {
        var id = e != null ? e.IconId : null;
        return !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out var s) ? s : null;
    }
}
