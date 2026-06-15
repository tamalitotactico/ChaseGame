using System.Collections.Generic;
using UnityEngine;

#if ASTAR_EXISTS
using Pathfinding;
#endif

/// <summary>
/// Lobo fantasma autonomo (Ghost Wolf, hab 1 del Werewolf). Navega con A* hacia el prey VIVO
/// mas cercano (snapshot + re-path cada rePathInterval) y, al alcanzarlo, lo MUERDE una vez
/// (slow, SIN dano) y desaparece. Vive como mucho maxLifetime. No es controlable.
///
/// Direccion inicial = donde apunto el hunter: durante los primeros aimBiasSeconds avanza recto
/// en esa direccion (el lanzamiento "sale" hacia donde apuntaste); luego pasa a perseguir al
/// prey con A*. Si no hay grafo A* (o no hay ruta), cae a steering directo al prey.
///
/// Se mueve por transform (como el ghost del jugador): el lobo no colisiona fisicamente, navega
/// por encima y esquiva por ruta. El paso de simulacion vive en Tick(dt) -> testeable en bucle.
///
/// TODO (Bloque 5): el mordisco ademas debe REVELAR al prey via World Target Pointer (#7).
/// Hoy solo aplica slow; el reveal se conecta cuando exista el pointer.
///
/// Prefab requirements: Collider2D (trigger, opcional) + GhostWolfController.
/// </summary>
public class GhostWolfController : MonoBehaviour
{
    Character _owner;
    Vector2   _initialDir;
    float _speed, _maxLifetime, _biteSlowDuration, _biteSlowMultiplier, _biteRadius, _aimBiasSeconds, _rePathInterval, _wallPadding;
    float _revealDuration, _pointerSize;
    Color _pointerColor = Color.cyan;
    Sprite _pointerSprite;

    float _elapsed, _rePathTimer;
    bool  _bitten;

    readonly List<Vector3> _waypoints = new();
    int _wpIndex;

    public void Init(Vector2 initialDir, Character owner, float speed, float maxLifetime,
                     float biteSlowDuration, float biteSlowMultiplier, float biteRadius,
                     float aimBiasSeconds, float rePathInterval, float wallPadding,
                     float revealDuration = 0f, Sprite pointerSprite = null, float pointerSize = 44f,
                     Color pointerColor = default)
    {
        _initialDir         = initialDir.sqrMagnitude > 0.01f ? initialDir.normalized : Vector2.right;
        _owner              = owner;
        _speed              = speed;
        _maxLifetime        = maxLifetime;
        _biteSlowDuration   = biteSlowDuration;
        _biteSlowMultiplier = biteSlowMultiplier;
        _biteRadius         = biteRadius;
        _aimBiasSeconds     = aimBiasSeconds;
        _rePathInterval     = Mathf.Max(0.1f, rePathInterval);
        _wallPadding        = Mathf.Max(0f, wallPadding);
        _rePathTimer        = 0f;
        _revealDuration     = revealDuration;
        _pointerSprite      = pointerSprite;
        _pointerSize        = pointerSize;
        if (pointerColor.a > 0f) _pointerColor = pointerColor;
    }

    void Update() => Tick(Time.deltaTime);

    /// <summary>Paso de simulacion (movimiento + mordisco). Publico para poder simularlo en tests.</summary>
    public void Tick(float dt)
    {
        if (_bitten) return;

        _elapsed += dt;
        if (_elapsed >= _maxLifetime) { Destroy(gameObject); return; }

        var prey = NearestPrey();
        Vector2 pos = transform.position;

        Vector2 dir;
        if (_elapsed < _aimBiasSeconds || prey == null)
        {
            dir = _initialDir;
        }
        else
        {
            _rePathTimer -= dt;
            if (_rePathTimer <= 0f)
            {
                RecomputePath(pos, prey.transform.position);
                _rePathTimer = _rePathInterval;
            }
            dir = FollowWaypoints(pos);
            if (dir.sqrMagnitude < 0.01f)
                dir = (Vector2)prey.transform.position - pos;
        }

        if (dir.sqrMagnitude > 0.0001f)
        {
            // El lobo navega por A* pero NO atraviesa muros: clampea el paso si un muro se cruza.
            Vector2 ndir = dir.normalized;
            float   step = _speed * dt;
            var hit = Physics2D.Raycast(pos, ndir, step + _wallPadding, GameLayers.WallMask);
            float travel = hit.collider != null ? Mathf.Max(0f, hit.distance - _wallPadding) : step;
            pos += ndir * travel;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }

        if (prey != null &&
            ((Vector2)prey.transform.position - pos).sqrMagnitude <= _biteRadius * _biteRadius)
            Bite(prey);
    }

    Character NearestPrey()
    {
        if (_owner == null) return null;
        var world = ServiceLocator.Resolve<IWorldQueryService>();
        if (world == null) return null;

        var enemies = world.GetEnemiesOf(_owner.Team);
        Vector2 pos = transform.position;
        Character best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < enemies.Count; i++)
        {
            var c = enemies[i];
            if (c == null || !c.IsAlive) continue;
            float sqr = ((Vector2)c.transform.position - pos).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = c; }
        }
        return best;
    }

    void RecomputePath(Vector3 start, Vector3 end)
    {
        _waypoints.Clear();
        _wpIndex = 0;
#if ASTAR_EXISTS
        if (AstarPath.active != null)
        {
            var p = ABPath.Construct(start, end, null);
            AstarPath.StartPath(p);
            p.BlockUntilCalculated();
            if (!p.error && p.vectorPath != null && p.vectorPath.Count > 0)
                _waypoints.AddRange(p.vectorPath);
        }
#endif
        if (_waypoints.Count == 0) { _waypoints.Add(start); _waypoints.Add(end); }
    }

    Vector2 FollowWaypoints(Vector2 pos)
    {
        if (_waypoints.Count == 0) return Vector2.zero;
        while (_wpIndex < _waypoints.Count - 1 &&
               ((Vector2)_waypoints[_wpIndex] - pos).sqrMagnitude < 0.25f)
            _wpIndex++;
        return (Vector2)_waypoints[_wpIndex] - pos;
    }

    void Bite(Character prey)
    {
        _bitten = true;
        if (prey.StatusEffects != null)
            prey.StatusEffects.Apply(new SlowedEffect(_biteSlowDuration, _biteSlowMultiplier));

        // Revela la posicion del prey mordido a los HUNTERS (bando del owner) via World Target Pointer.
        if (_revealDuration > 0f && ShouldRevealToLocal())
            WorldTargetPointer.Show(prey.transform, _revealDuration, _pointerSprite, _pointerSize, _pointerColor);

        Destroy(gameObject);
    }

    // El reveal solo se muestra si el jugador LOCAL es del bando del owner (un hunter).
    bool ShouldRevealToLocal()
    {
        var local  = PlayerBrain.Local;
        var viewer = local != null ? local.GetComponent<Character>() : null;
        return viewer != null && _owner != null && viewer.Team == _owner.Team;
    }
}
