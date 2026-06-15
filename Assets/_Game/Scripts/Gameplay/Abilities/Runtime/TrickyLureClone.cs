using UnityEngine;

/// <summary>
/// Clon-senuelo del TrickyWizard: avanza en linea recta imitando AL personaje del dueño (mismo
/// animator/sprite/escala/material), animando su locomocion como si caminara en esa direccion.
/// NO rota el GameObject (la direccion la da la animacion 8-direccional, igual que el personaje).
/// NO atraviesa muros (raycast-clamp contra WallMask). 1 HP: muere de un golpe soltando una risa.
///
/// Visibilidad por-viewer (matriz, igual idea que [[StateVisibility]]): el ENEMIGO (hunter) lo ve
/// IDENTICO al prey; el ALIADO lo ve con un outline indicador de "esto es un clon". Local (Phase 0):
/// el viewer es PlayerBrain.Local; en red se resolvera por-cliente.
///
/// Es IDamageable para que el ataque basico del hunter (targetLayers = Everything) lo alcance.
/// Prefab: Rigidbody2D (Kinematic) + Collider2D (IsTrigger, solo para recibir golpes) + SpriteRenderer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TrickyLureClone : MonoBehaviour, IDamageable
{
    // Octantes y sets: misma convencion que CharacterAnimator (Run_{E,NE,N,NW,W,SW,S,SE}).
    static readonly string[] DirSuffix = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

    Vector2  _dir;
    float    _speed, _timer, _wallPadding;
    int      _hp;
    AudioCue _laugh;

    SpriteRenderer _sr;
    Animator       _animator;
    Material       _mat;

    static readonly int ID_OutlineEnabled = Shader.PropertyToID("_OutlineEnabled");
    static readonly int ID_OutlineColor   = Shader.PropertyToID("_OutlineColor");

    public bool IsAlive      => _hp > 0;
    public bool IsTargetable => true;

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    /// <summary>
    /// Configura el clon copiando el rig visual del dueño. dir = direccion de avance (constante).
    /// </summary>
    public void Init(Character owner, Vector2 dir, float speed, float lifetime, int hp,
                     AudioCue laugh, float wallPadding, Color allyOutlineColor)
    {
        _dir         = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        _speed       = speed;
        _timer       = lifetime;
        _hp          = Mathf.Max(1, hp);
        _laugh       = laugh;
        _wallPadding = Mathf.Max(0f, wallPadding);

        if (_sr == null) _sr = GetComponent<SpriteRenderer>();

        // Copiar el aspecto del dueño: sprite/material/orden/escala => identico para el enemigo.
        var ownerSr = owner != null ? owner.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (ownerSr != null)
        {
            _sr.sprite         = ownerSr.sprite;        // frame inicial (el animator lo reemplaza enseguida)
            _sr.sharedMaterial = ownerSr.sharedMaterial; // CharacterEffect (outline/efectos)
            _sr.sortingLayerID = ownerSr.sortingLayerID;
            _sr.sortingOrder   = ownerSr.sortingOrder;
            _sr.color          = Color.white;            // prey "sano": sin tints transitorios del dueño
            transform.localScale = ownerSr.transform.lossyScale; // mismo tamaño rendizado
        }

        // Animator con el MISMO controller del dueño, en el GO del SpriteRenderer (los clips
        // keyean SpriteRenderer.sprite). Asi anima la locomocion tal cual el personaje.
        // OJO: NO usar `??` con componentes Unity (no respeta el fake-null). El prefab ya trae
        // Animator; si faltara, TryGetComponent + AddComponent lo resuelve sin lanzar.
        var ownerAnim = owner != null ? owner.GetComponentInChildren<Animator>(true) : null;
        if (ownerAnim != null && ownerAnim.runtimeAnimatorController != null)
        {
            if (!TryGetComponent(out _animator)) _animator = gameObject.AddComponent<Animator>();
            _animator.runtimeAnimatorController = ownerAnim.runtimeAnimatorController;
            _animator.applyRootMotion = false;
            PlayRun();
        }

        // Outline indicador SOLO para aliados (incluido el propio TrickyWizard). Enemigo => identico.
        var local  = PlayerBrain.Local;
        var viewer = local != null ? local.GetComponent<Character>() : null;
        bool ally  = viewer != null && owner != null && viewer.Team == owner.Team;
        if (ally && _sr != null && _sr.sharedMaterial != null)
        {
            _mat = _sr.material; // instancia (no toca el material compartido del dueño)
            _mat.SetFloat(ID_OutlineEnabled, 1f);
            _mat.SetColor(ID_OutlineColor, allyOutlineColor);
        }
    }

    void PlayRun()
    {
        if (_animator == null) return;
        int oct = DirToOctant(_dir);
        int h = Animator.StringToHash("Run_" + DirSuffix[oct]);
        if (_animator.HasState(0, h)) _animator.Play(h);
        else
        {
            int idle = Animator.StringToHash("Idle_S");
            if (_animator.HasState(0, idle)) _animator.Play(idle);
        }
    }

    void Update()
    {
        // Mover por transform con clamp contra muros (no atraviesa). Sin rotacion.
        float dt = Time.deltaTime;
        Vector2 pos = transform.position;
        float step = _speed * dt;
        if (step > 0f && GameLayers.WallMask != 0)
        {
            var hit = Physics2D.Raycast(pos, _dir, step + _wallPadding, GameLayers.WallMask);
            float travel = hit.collider != null ? Mathf.Max(0f, hit.distance - _wallPadding) : step;
            pos += _dir * travel;
        }
        else
        {
            pos += _dir * step;
        }
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        _timer -= dt;
        if (_timer <= 0f) Destroy(gameObject);
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (_hp <= 0) return;
        _hp -= Mathf.Max(1, info.Amount);
        if (_hp <= 0)
        {
            ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_laugh, transform.position);
            Destroy(gameObject);
        }
    }

    // atan2 CCW desde +X; /45 redondeado -> octante 0..7 (igual que CharacterAnimator).
    static int DirToOctant(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return 6; // South
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (ang < 0f) ang += 360f;
        return Mathf.RoundToInt(ang / 45f) % 8;
    }
}
