using UnityEngine;

/// <summary>
/// Base de los objetos COLOCABLES en el mundo (Raise Wall, Beacon, trampas). Maneja el dueno y
/// una vida opcional (lifetime 0 = infinito). Las subclases agregan su efecto (slow, buff, trampa)
/// en Update llamando base.Update().
///
/// Se spawnea via ISpawnService (PlaceableAbility) y se rastrea con cupo por owner+tipo
/// (PlaceableRegistry). Seam Fusion: en red el spawn/lifetime sera autoritativo.
/// </summary>
public class Placeable : MonoBehaviour
{
    protected Character Owner;

    float _timer;
    bool  _hasTimer;

    public virtual void Init(Character owner, float lifetime)
    {
        Owner = owner;
        SetLifetime(lifetime);
    }

    /// <summary>Fija (o re-fija) la vida restante. lifetime &lt;= 0 = infinito. Lo usa Raise Wall para
    /// poner 5s cuando un hunter lo toca.</summary>
    public void SetLifetime(float lifetime)
    {
        _hasTimer = lifetime > 0f;
        _timer    = lifetime;
    }

    protected virtual void Update()
    {
        if (!_hasTimer) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0f) NetDespawn.Despawn(gameObject);
    }
}
