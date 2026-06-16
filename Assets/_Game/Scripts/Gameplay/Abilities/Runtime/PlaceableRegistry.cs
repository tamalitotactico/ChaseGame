using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rastrea los placeables activos por (owner, tipo de ability) para imponer un CUPO: al exceder
/// maxInstances, despawnea el mas antiguo. Compartido por Raise Wall (2), Beacon (1) y, a futuro,
/// las trampas del Trapper (Smoke 2 / Bear 3).
///
/// Nota: es estado estatico local (se limpia entries con GO destruidos al consultar). Para red
/// (Fusion) esto pasa a tracking autoritativo por jugador; la API (Register/Count) queda igual.
/// </summary>
public static class PlaceableRegistry
{
    static readonly Dictionary<(Character, System.Type), List<GameObject>> _map = new();

    public static void Register(Character owner, System.Type abilityType, GameObject go, int maxInstances)
    {
        if (owner == null || go == null) return;
        var key = (owner, abilityType);
        if (!_map.TryGetValue(key, out var list)) { list = new List<GameObject>(); _map[key] = list; }

        list.RemoveAll(g => g == null);
        list.Add(go);

        while (maxInstances > 0 && list.Count > maxInstances)
        {
            var oldest = list[0];
            list.RemoveAt(0);
            if (oldest != null) NetDespawn.Despawn(oldest);
        }
    }

    public static int Count(Character owner, System.Type abilityType)
    {
        if (owner != null && _map.TryGetValue((owner, abilityType), out var list))
        {
            list.RemoveAll(g => g == null);
            return list.Count;
        }
        return 0;
    }
}
