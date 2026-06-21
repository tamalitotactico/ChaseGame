#if FUSION2
using Fusion;
using UnityEngine;

/// <summary>
/// Replica la geometria que un placeable calcula EN RUNTIME en el host (posicion, rotacion y, para
/// sprites Sliced, el SpriteRenderer.size) hacia los clientes. Necesario para objetos como el muro del
/// Engineer (RaiseWallPlaceable), que se reposicionan/redimensionan DESPUES del spawn via raycasts: sin
/// NetworkTransform/NetworkRigidbody2D esos cambios no viajan y el cliente ve el prefab base ("un punto").
///
/// El host llama HostSet(...) tras calcular la geometria; los clientes la aplican en Render() (polling,
/// patron Fusion 2). En Solo (sin runner) es inerte: el placeable aplica su geometria directo como antes.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkedPlaceableVisual : NetworkBehaviour
{
    [Networked] Vector2 NetPos      { get; set; }
    [Networked] float   NetAngleDeg { get; set; }
    [Networked] Vector2 NetSize     { get; set; } // SpriteRenderer.size (Sliced); (0,0) = no tocar size

    SpriteRenderer _sr;
    bool _applied;

    public override void Spawned()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        Apply();
    }

    /// <summary>HOST: fija la geometria calculada. size=(0,0) si el placeable no usa SpriteRenderer.size.</summary>
    public void HostSet(Vector2 pos, float angleDeg, Vector2 size)
    {
        if (Object == null || !Object.HasStateAuthority) return;
        NetPos = pos; NetAngleDeg = angleDeg; NetSize = size;
        Apply();
    }

    public override void Render()
    {
        // El cliente recibe los valores despues de Spawned; aplicar hasta que lleguen valores reales.
        if (!_applied || !Object.HasStateAuthority) Apply();
    }

    void Apply()
    {
        if (NetPos == Vector2.zero && NetSize == Vector2.zero) return; // aun sin datos
        transform.position = new Vector3(NetPos.x, NetPos.y, transform.position.z);
        transform.rotation = Quaternion.Euler(0f, 0f, NetAngleDeg);

        if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
        if (_sr != null && NetSize.sqrMagnitude > 0.0001f)
        {
            _sr.drawMode = SpriteDrawMode.Sliced;
            _sr.size     = NetSize;
        }
        _applied = true;
    }
}
#endif
