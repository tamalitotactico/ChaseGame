using UnityEngine;

/// <summary>
/// Controla la visibilidad de un personaje en la niebla de guerra basandose
/// en la posicion de sus pies (pivot), no pixel-a-pixel como el SpriteMask.
///
/// Atacha al root del Character. Gestiona TODOS los SpriteRenderer del jerarquia
/// (root y descendientes) — necesario porque los prefabs Hunter/Prey tienen
/// multiples SR (root + child "Sprite").
///
/// Comportamiento:
///  - Local viewer (mismo Character que la PrimarySource): siempre visible y
///    su sortingOrder se eleva por encima del FogOverlay para que la niebla
///    nunca lo oscurezca.
///  - Otros characters: visibles solo si estan dentro del radio Y con linea
///    de vision al jugador local (sin muro entre medio).
/// </summary>
public class CharacterFogVisibility : MonoBehaviour
{

    SpriteRenderer[] _srs;
    bool[] _initialEnabled;
    int[]  _initialOrder;

    void Awake()
    {
        // El componente debe vivir en el root del Character. Si esta en un
        // child (ej. "Sprite" del prefab antiguo), auto-destruirse: el
        // FogOfWarManager agrega un CFV nuevo en el root al spawnear. Esto
        // evita el bug donde el child Sprite estaba inactivo en Prey y
        // LateUpdate nunca corria.
        if (transform != transform.root)
        {
            Destroy(this);
            return;
        }

        // Capturar TODOS los SR del personaje (root y descendientes).
        _srs = transform.root.GetComponentsInChildren<SpriteRenderer>(true);
        _initialEnabled = new bool[_srs.Length];
        _initialOrder   = new int[_srs.Length];
        for (int i = 0; i < _srs.Length; i++)
        {
            _initialEnabled[i] = _srs[i].enabled;
            _initialOrder[i]   = _srs[i].sortingOrder;
            _srs[i].maskInteraction = SpriteMaskInteraction.None;
        }
    }

    void LateUpdate()
    {
        if (_srs == null) return; // marcado para destruir en Awake

        var fow = FogOfWarManager.Instance;
        if (fow == null) { Apply(visible: true, boost: false); return; }

        var src = fow.PrimarySource;
        if (src == null) { Apply(visible: true, boost: false); return; }

        // Local viewer: el Character con VisionSource (root del player) o
        // cualquier descendiente. Nunca se oculta y siempre va sobre la niebla.
        bool isLocal = transform == src.transform || transform.IsChildOf(src.transform);
        if (isLocal) { Apply(visible: true, boost: true); return; }

        Vector2 pivot  = transform.position;
        Vector2 eye    = src.transform.position;
        float   radius = src.VisionRadius;

        if (Vector2.SqrMagnitude(pivot - eye) > radius * radius)
        {
            Apply(visible: false, boost: false);
            return;
        }

        bool blocked = Physics2D.Linecast(eye, pivot, fow.wallMask).collider != null;
        Apply(visible: !blocked, boost: false);
    }

    void Apply(bool visible, bool boost)
    {
        for (int i = 0; i < _srs.Length; i++)
        {
            var sr = _srs[i];
            if (sr == null) continue;
            // boost=true (local viewer): siempre visible, ignorar initial state.
            // boost=false: solo activar los que estaban activos en el prefab original.
            sr.enabled = boost ? true : (visible && _initialEnabled[i]);
            sr.sortingOrder = _initialOrder[i];
        }
    }
}
