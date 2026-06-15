using UnityEngine;

/// <summary>
/// Mapeo serializado IndicatorShape -> prefab de indicador. Permite que el indicador se
/// auto-seleccione desde AbilityData.ResolvedShape, sin asignar un prefab por SO (el roster es
/// data-driven; agregar una habilidad nueva no requiere cablear su indicador a mano).
///
/// Un AbilityData puede forzar un prefab puntual via su campo 'indicatorPrefab' (override
/// explicito), que tiene prioridad sobre el registro.
///
/// Crear el asset: Assets > Create > ChaseGame > Indicator Registry, asignar los prefabs de
/// Assets/_Game/Prefabs/Indicators/ y referenciarlo en GameManager.
/// </summary>
[CreateAssetMenu(fileName = "IndicatorRegistry", menuName = "ChaseGame/Indicator Registry")]
public class IndicatorRegistry : ScriptableObject
{
    [Tooltip("Direccional (proyectil, dash). Lee IndicatorRange + IndicatorWidth.")]
    public GameObject arrow;        // ArrowIndicator.prefab
    [Tooltip("Anillo de alcance (Area/AllyTarget/Assault). Lee IndicatorRange como radio.")]
    public GameObject ring;         // RangeIndicator.prefab
    [Tooltip("Circulo relleno de AoE (auras, trampas, heal). Lee IndicatorRadius.")]
    public GameObject aoe;          // AOEIndicator.prefab
    [Tooltip("Cono frontal. Lee IndicatorRange (escala uniforme).")]
    public GameObject cone;         // ConeIndicator.prefab
    [Tooltip("Flecha + AoE de aterrizaje (TeleportSmash, Ejecucion). Lee IndicatorRange + IndicatorRadius.")]
    public GameObject arrowAoe;     // RangedAOE.prefab

    /// <summary>Prefab para una forma ya resuelta (Auto debe resolverse antes de llamar aqui). Null si None.</summary>
    public GameObject Resolve(IndicatorShape shape)
    {
        switch (shape)
        {
            case IndicatorShape.Arrow:    return arrow;
            case IndicatorShape.Ring:     return ring;
            case IndicatorShape.AoE:      return aoe;
            case IndicatorShape.Cone:     return cone;
            case IndicatorShape.ArrowAoE: return arrowAoe;
            default:                      return null; // None / Auto
        }
    }
}
