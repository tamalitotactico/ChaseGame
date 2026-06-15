using UnityEngine;

/// <summary>
/// Indicador PROCEDURAL (reemplaza los sprite-indicators). Dibuja la forma EXACTA por SDF en un quad
/// escalado a unidades de mundo, con el material Game/AbilityIndicator. El borde cae justo en el rango
/// real, el grosor del arrow = diametro real del proyectil (AbilityData.ProjectileRadius), y el AoE se
/// dibuja en el punto apuntado (no a maxRange). Crea sus quads en runtime; tunables serializados.
///
/// Formas (AbilityData.ResolvedShape): Ring/AoE -> circulo (radio = IndicatorRadius); Arrow -> lane
/// (largo = IndicatorRange clampeado a muro, ancho = diametro del proyectil); ArrowAoE -> lane +
/// disco en el aterrizaje (radio = IndicatorRadius); Cone -> sector (IndicatorConeHalfAngle).
/// </summary>
public class ProceduralIndicator : AimIndicator
{
    [Header("Render")]
    [Tooltip("Material Game/AbilityIndicator. Si es null se crea de Shader.Find en runtime.")]
    [SerializeField] Material material;
    [SerializeField] int sortingOrder = 5;
    [SerializeField] string sortingLayer = "Default";

    [Header("Color (si el SO no define indicatorColor)")]
    [ColorUsage(true, true)] [SerializeField] Color hunterColor = new Color(4f, 0.8f, 0.35f, 1f);
    [ColorUsage(true, true)] [SerializeField] Color preyColor   = new Color(0.35f, 0.9f, 4f, 1f);
    [Tooltip("Grosor del borde en unidades de mundo (constante a cualquier rango).")]
    [SerializeField] float edgeWorld = 0.12f;

    [Header("Muros")]
    [Tooltip("Recortar el largo del arrow al muro (coincide con donde muere el proyectil).")]
    [SerializeField] bool clampToWalls = true;

    AbilityData    _data;
    IndicatorShape _shape;
    Color          _edgeColor, _fillColor;
    Renderer       _primary, _secondary;
    MaterialPropertyBlock _mpb;
    static Mesh _quad;

    static readonly int IdShape = Shader.PropertyToID("_Shape");
    static readonly int IdEdge  = Shader.PropertyToID("_EdgeColor");
    static readonly int IdFill  = Shader.PropertyToID("_FillColor");
    static readonly int IdSize  = Shader.PropertyToID("_SizeWorld");
    static readonly int IdEdgeW = Shader.PropertyToID("_EdgeWorld");
    static readonly int IdCone  = Shader.PropertyToID("_ConeHalfAngle");
    static readonly int IdFillP = Shader.PropertyToID("_Fill");

    public override void Begin(Character owner, AbilityData data)
    {
        _data  = data;
        _shape = data != null ? data.ResolvedShape : IndicatorShape.None;

        Color baseCol = (data != null && data.indicatorColor.a > 0f)
            ? data.indicatorColor
            : (owner != null && owner.Team == CharacterTeam.Hunter ? hunterColor : preyColor);
        _edgeColor = baseCol;
        _fillColor = new Color(baseCol.r, baseCol.g, baseCol.b, 0.18f);

        EnsureRenderers();
        _mpb ??= new MaterialPropertyBlock();
        if (_secondary != null) _secondary.enabled = _shape == IndicatorShape.ArrowAoE;
        if (_primary   != null) _primary.enabled   = _shape != IndicatorShape.None;
    }

    public override void Tick(in AimIndicatorState s)
    {
        if (_data == null || _primary == null) return;
        float z = transform.position.z;

        switch (_shape)
        {
            case IndicatorShape.Ring:
            case IndicatorShape.AoE:
            {
                // Circulo centrado en el caster (reach/self-AoE). Radio = IndicatorRadius.
                float r = Mathf.Max(0.01f, _data.IndicatorRadius);
                PlaceCircle(_primary, s.Origin, z, r, _shape == IndicatorShape.Ring ? 0 : 1, s.CastProgress);
                break;
            }
            case IndicatorShape.Cone:
            {
                float range = Mathf.Max(0.01f, _data.IndicatorRange);
                float w = range * Mathf.Tan(_data.IndicatorConeHalfAngle * Mathf.Deg2Rad) * 2f;
                PlaceArrowLike(_primary, s.Origin, z, s.Direction, range, w, 3, s.CastProgress,
                               _data.IndicatorConeHalfAngle * Mathf.Deg2Rad);
                break;
            }
            case IndicatorShape.ArrowAoE:
            {
                // Lane desde el caster hasta el aterrizaje + disco AoE en el aterrizaje.
                float maxLen = _data.IndicatorRange;
                float len = s.HasTarget ? Mathf.Min(maxLen, Vector2.Distance(s.Origin, s.Target)) : maxLen;
                len = ClampWall(s.Origin, s.Direction, len);
                Vector2 landing = s.Origin + s.Direction.normalized * len;
                float w = ArrowWidth();
                PlaceArrowLike(_primary, s.Origin, z, s.Direction, len, w, 2, s.CastProgress, 0f);
                if (_secondary != null)
                    PlaceCircle(_secondary, landing, z, Mathf.Max(0.01f, _data.IndicatorRadius), 1, s.CastProgress);
                break;
            }
            case IndicatorShape.Arrow:
            default:
            {
                float len = ClampWall(s.Origin, s.Direction, _data.IndicatorRange);
                PlaceArrowLike(_primary, s.Origin, z, s.Direction, len, ArrowWidth(), 2, s.CastProgress, 0f);
                break;
            }
        }
    }

    float ArrowWidth() => _data.ProjectileRadius > 0f ? _data.ProjectileRadius * 2f : _data.IndicatorWidth;

    float ClampWall(Vector2 origin, Vector2 dir, float len)
    {
        if (!clampToWalls || len <= 0f) return len;
        float rr = _data.ProjectileRadius > 0f ? _data.ProjectileRadius * 0.5f : 0.05f;
        var hit = Physics2D.CircleCast(origin, rr, dir.normalized, len, GameLayers.WallMask);
        return hit.collider != null ? Mathf.Max(0f, hit.distance) : len;
    }

    void PlaceCircle(Renderer r, Vector2 center, float z, float radius, int shape, float fill)
    {
        var t = r.transform;
        t.position   = new Vector3(center.x, center.y, z);
        t.rotation   = Quaternion.identity;
        t.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        SetMPB(r, shape, new Vector4(radius * 2f, radius * 2f), 0f, fill);
    }

    // shape 2 = arrow lane, 3 = cone. Para arrow/cone el quad nace en el caster y crece hacia dir.
    void PlaceArrowLike(Renderer r, Vector2 origin, float z, Vector2 dir, float length, float width, int shape, float fill, float coneHalf)
    {
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        Vector2 center = origin + dir * (length * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // local +Y -> dir
        var t = r.transform;
        t.position   = new Vector3(center.x, center.y, z);
        t.rotation   = Quaternion.Euler(0f, 0f, angle);
        t.localScale = new Vector3(Mathf.Max(0.01f, width), Mathf.Max(0.01f, length), 1f);
        SetMPB(r, shape, new Vector4(width, length), coneHalf, fill);
    }

    void SetMPB(Renderer r, int shape, Vector4 sizeWorld, float coneHalf, float fill)
    {
        _mpb ??= new MaterialPropertyBlock();
        r.GetPropertyBlock(_mpb);
        _mpb.SetFloat(IdShape, shape);
        _mpb.SetColor(IdEdge, _edgeColor);
        _mpb.SetColor(IdFill, _fillColor);
        _mpb.SetVector(IdSize, sizeWorld);
        _mpb.SetFloat(IdEdgeW, edgeWorld);
        _mpb.SetFloat(IdCone, coneHalf);
        _mpb.SetFloat(IdFillP, fill);
        r.SetPropertyBlock(_mpb);
    }

    void EnsureRenderers()
    {
        if (_quad == null) _quad = BuildQuad();
        if (material == null)
        {
            var sh = Shader.Find("Game/AbilityIndicator");
            if (sh != null) material = new Material(sh);
        }
        if (_primary   == null) _primary   = CreateQuadChild("IndPrimary");
        if (_secondary == null) _secondary = CreateQuadChild("IndSecondary");
    }

    Renderer CreateQuadChild(string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = _quad;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayer)) mr.sortingLayerName = sortingLayer;
        return mr;
    }

    static Mesh BuildQuad()
    {
        var m = new Mesh { name = "IndicatorQuad" };
        m.vertices  = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
        m.uv        = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        m.RecalculateBounds();
        return m;
    }
}
