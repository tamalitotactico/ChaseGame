using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Niebla de guerra estilo Dota 2:
///   - El terreno siempre es visible pero oscurecido fuera del campo de vision.
///   - Personajes y objetos dinamicos se ocultan via CharacterFogVisibility.
///
/// Implementacion (procedural mesh + RenderTexture):
///   1. N raycasts en 360 deg desde el viewer.
///   2. Construye un Mesh tipo Triangle Fan en coordenadas normalizadas
///      ([-1..1] relativo al radio). Centro = (0,0); perimetro = hit points normalizados.
///   3. CommandBuffer renderiza el mesh con shader Hidden/FogVisionMesh a una
///      RenderTexture (blanco dentro del poligono, negro fuera).
///   4. La RT se asigna a _VisionTex0 del FogOverlay shader, que la muestrea
///      por posicion de mundo (worldXY) para calcular visibility.
///
/// Optimizaciones:
///   - Cantidad de rayos fija para evitar GC Alloc y re-creacion de arrays/triangulos.
///   - Corrección del Y-flip en RenderTexture usando GL.GetGPUProjectionMatrix.
///   - Eliminación del corner-detection (ahorros de CPU en raycasts e indexación).
/// </summary>
public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    /// <summary>Primera VisionSource registrada (jugador local).</summary>
    public VisionSource PrimarySource => _primarySource;

    /// <summary>Origen de mundo desde el que se raycasteo el ultimo poligono de vision.
    /// El shader centra la niebla aqui (_VisionPos0); asi centro y forma nunca se desfasan.</summary>
    public Vector2 LastViewOrigin { get; private set; }

    /// <summary>Cuando true, la niebla se APAGA por completo (overlay oculto + todos los personajes
    /// visibles via CharacterFogVisibility). Lo usa el modo fantasma: el jugador downed debe ver TODO
    /// el mapa mientras explora. Se restaura al revivir.</summary>
    public bool RevealAll { get; private set; }

    /// <summary>Enciende/apaga el modo "ver todo" (sin niebla). Apaga el overlay y, via RevealAll,
    /// CharacterFogVisibility muestra a todos. LateUpdate corta los raycasts mientras esta activo.</summary>
    public void SetRevealAll(bool on)
    {
        RevealAll = on;
        if (fogOverlay != null) fogOverlay.enabled = !on;
    }

    [Header("Fog")]
    [Tooltip("SpriteRenderer que usa el material Game/FogOverlay. MaskInteraction=None.")]
    [SerializeField] SpriteRenderer fogOverlay;
    [Tooltip("Ancho del fade suave en el borde del radio de vision, en unidades de mundo.")]
    [SerializeField] float fadeWidth = 1.5f;

    [Header("Actualizacion")]
    [Tooltip("Segundos entre rebuilds del poligono de vision. 0 = cada frame. 0.05 = 20 fps.")]
    [SerializeField][Min(0f)] float updateInterval = 0f;

    [Header("Raycasting")]
    [Tooltip("Rayos en 360 grados (fijo en runtime para evitar reasignar memoria).")]
    [SerializeField] int rayCount = 120;
    [Tooltip("Layers que bloquean la vision.")]
    [SerializeField] public LayerMask wallMask;
    [Tooltip("Resolucion del RenderTexture de vision. Potencias de 2.")]
    [SerializeField] int textureSize = 256;

    // IDs de propiedad del shader (cacheados para evitar hash por frame).
    static readonly int ID_VisionTex0    = Shader.PropertyToID("_VisionTex0");
    static readonly int ID_VisionPos0    = Shader.PropertyToID("_VisionPos0");
    static readonly int ID_VisionRadius0 = Shader.PropertyToID("_VisionRadius0");
    static readonly int ID_FadeWidth     = Shader.PropertyToID("_FadeWidth");

    // Proyeccion ortho [-1..1] → NDC.
    static readonly Matrix4x4 _orthoProj =
        Matrix4x4.Ortho(-1f, 1f, -1f, 1f, -1f, 1f);

    Material _fogMat;
    Material _visionMeshMat;
    float    _rebuildTimer;

    // Estado unico para el source principal (player)
    VisionSource  _primarySource;
    RenderTexture _rt;
    Mesh          _mesh;
    CommandBuffer _cmd;
    List<Vector3> _verts;
    List<int>     _tris;

    void Awake()
    {
        Instance = this;

        // Auto-instancia el material del fogOverlay para no afectar el shared.
        if (fogOverlay != null)
        {
            _fogMat = fogOverlay.material;
            _fogMat.SetFloat(ID_FadeWidth, fadeWidth);
        }

        // Material temporal para renderizar el mesh de vision a la RT.
        // IMPORTANTE: en builds (mobile/desktop) Unity strippea shaders no
        // referenciados por materiales en escena/Resources. Para que esto
        // funcione en build, agregar 'Hidden/FogVisionMesh' a:
        //   Project Settings > Graphics > Always Included Shaders
        // Sin eso, Shader.Find retorna null en builds y el FoW queda invisible
        // o completamente negro segun como interprete el FogOverlay la RT vacia.
        var shader = Shader.Find("Hidden/FogVisionMesh");
        if (shader != null)
        {
            _visionMeshMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        else
        {
            Debug.LogError(
                "[FogOfWar] Shader 'Hidden/FogVisionMesh' no encontrado. " +
                "Probablemente fue stripped del build. " +
                "Fix: Project Settings > Graphics > Always Included Shaders, " +
                "agrega Hidden/FogVisionMesh y Game/FogOverlay.");
        }
        if (Shader.Find("Game/FogOverlay") == null)
            Debug.LogError("[FogOfWar] Shader 'Game/FogOverlay' tampoco esta presente. Agregar a Always Included Shaders.");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ReleaseResources();
    }

    void ReleaseResources()
    {
        if (_rt   != null) { _rt.Release(); _rt = null; }
        if (_mesh != null) { Destroy(_mesh); _mesh = null; }
        if (_cmd  != null) { _cmd.Release(); _cmd = null; }
        if (_visionMeshMat != null) { DestroyImmediate(_visionMeshMat); _visionMeshMat = null; }
    }

    // Auto-conexion al jugador local
    void OnEnable()  => EventBus.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
    void OnDisable() => EventBus.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);

    void OnCharacterSpawned(CharacterSpawnedEvent e)
    {
        if (e.Character == null) return;

        // CharacterFogVisibility en el root de TODOS los characters (raycast-based).
        if (e.Character.GetComponent<CharacterFogVisibility>() == null)
            e.Character.gameObject.AddComponent<CharacterFogVisibility>();

        if (e.Character.GetComponent<PlayerBrain>() == null) return;
        var vs = e.Character.GetComponent<VisionSource>();
        if (vs != null)
        {
            InitializeSource(vs);
        }
    }

    /// <summary>
    /// Reapunta la fuente de vision primaria en runtime. Lo usa el modo fantasma: al caer downed el
    /// jugador local, la vision pasa al fantasma; al revivir, vuelve al cuerpo. Reusa la ruta de init
    /// (recrea RT/mesh y resetea LastViewOrigin). Son eventos raros, el costo es aceptable.
    /// </summary>
    public void SetPrimarySource(VisionSource source)
    {
        if (source == null) return;
        InitializeSource(source);
    }

    void InitializeSource(VisionSource source)
    {
        _primarySource = source;
        // Evita un primer frame con centro en (0,0) antes del primer BuildMesh.
        LastViewOrigin = source.transform.position;

        if (_rt != null) _rt.Release();
        _rt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };
        _rt.Create();

        // Limpiar textura inicial para evitar destellos de basura en memoria
        var prevActive = RenderTexture.active;
        RenderTexture.active = _rt;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prevActive;

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "VisionMesh" };
            _mesh.MarkDynamic();
            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 0.1f));
        }

        if (_cmd == null)
        {
            _cmd = new CommandBuffer { name = "FoW_VisionMesh" };
        }

        // Estructura fija de vertices y triangulos (cero realloc en runtime)
        int vertCount = rayCount + 1;
        _verts = new List<Vector3>(vertCount);
        for (int i = 0; i < vertCount; i++)
        {
            _verts.Add(Vector3.zero);
        }

        _tris = new List<int>(rayCount * 3);
        for (int i = 0; i < rayCount; i++)
        {
            int next = (i + 1) % rayCount;
            _tris.Add(0);
            _tris.Add(i + 1);
            _tris.Add(next + 1);
        }

        _mesh.Clear();
        _mesh.SetVertices(_verts);
        _mesh.SetTriangles(_tris, 0, false);
    }

    void LateUpdate()
    {
        if (_primarySource == null) return;
        if (RevealAll) return; // niebla apagada (modo fantasma): sin rebuild ni overlay

        _rebuildTimer += Time.deltaTime;
        // updateInterval=0 => rebuild cada frame (recomendado para fluidez). Un valor >0 throttlea
        // el FoW completo de forma UNIFORME (centro Y forma juntos), nunca por separado.
        // NO reintroducir un clamp que solo throttlee BuildMesh (la forma) dejando el centro por
        // frame: eso desfasa la sombra de muro y produce el temblor (ver comentario CRITICO abajo).
        bool rebuild = updateInterval <= 0f || _rebuildTimer >= updateInterval;
        if (rebuild)
        {
            _rebuildTimer = 0f;
            BuildMesh(); // actualiza LastViewOrigin (origen de los raycasts)
        }

        if (_fogMat == null) return;

        // CRITICO (anti-temblor): el centro del shader (_VisionPos0) debe ser EXACTAMENTE el punto
        // desde el que se raycasteo el poligono actual = LastViewOrigin. Si en su lugar leyeramos
        // _primarySource.transform.position cada frame mientras BuildMesh va throttleada, el shader
        // re-centraria un poligono viejo sobre la posicion nueva: la sombra "sigue al player y
        // vuelve a su sitio" en cada rebuild = el temblor reportado. Atar centro==origen lo elimina
        // por construccion. Con updateInterval=0 (cada frame) LastViewOrigin == posicion del sprite.
        Vector2 pos = LastViewOrigin;

        _fogMat.SetVector(ID_VisionPos0,   new Vector4(pos.x, pos.y, 0f, 0f));
        _fogMat.SetFloat(ID_VisionRadius0, _primarySource.VisionRadius);
        _fogMat.SetTexture(ID_VisionTex0,  _rt);
    }

    void BuildMesh()
    {
        if (_visionMeshMat == null || _primarySource == null || _rt == null) return;

        // Misma posicion cruda que usa _VisionPos0 (ver LateUpdate): el poligono de raycast y su
        // colocacion en el shader deben partir del mismo punto exacto = el del sprite del player.
        Vector2 origin = _primarySource.transform.position;
        float   radius = _primarySource.VisionRadius;
        float   angleStep = 360f / rayCount;

        LastViewOrigin = origin;

        _verts[0] = Vector3.zero; // Origen local

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var hit = Physics2D.Raycast(origin, dir, radius, wallMask);
            float dist = hit.collider != null ? hit.distance : radius;
            float n = dist / radius;
            _verts[i + 1] = new Vector3(dir.x * n, dir.y * n, 0f);
        }

        // Subir vertices modificados in-place
        _mesh.SetVertices(_verts);

        // Render al RenderTexture via CommandBuffer
        _cmd.Clear();
        _cmd.SetRenderTarget(_rt);
        _cmd.ClearRenderTarget(true, true, Color.black);

        // Corregir Y-flip para Direct3D/Metal/Vulkan en RenderTextures
        Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(_orthoProj, true);
        _cmd.SetViewProjectionMatrices(Matrix4x4.identity, gpuProj);
        _cmd.DrawMesh(_mesh, Matrix4x4.identity, _visionMeshMat);
        
        Graphics.ExecuteCommandBuffer(_cmd);
    }
}
