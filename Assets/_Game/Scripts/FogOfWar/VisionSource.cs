using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente que indica que esta entidad genera vision propia (rompe la niebla).
/// Solo el jugador humano tiene este componente en Fase 1.
///
/// El radio efectivo = radio base * producto de los multiplicadores activos. Esto permite
/// modificarlo temporalmente sin pisar el valor base: BlindedEffect lo reduce (Smoke Trap,
/// Disparo cegador), y buffs de vision lo aumentarian con el mismo mecanismo.
/// </summary>
public class VisionSource : MonoBehaviour
{
    [Header("Vision")]
    [Tooltip("Radio de vision en unidades de mundo. Hunter: 8, Prey: 5.")]
    [SerializeField] public float visionRadius = 5f;

    readonly Dictionary<object, float> _multipliers = new();
    float _effective = 1f;

    /// <summary>Radio de vision efectivo (base * multiplicadores activos).</summary>
    public float VisionRadius => visionRadius * _effective;

    /// <summary>Aplica/actualiza un multiplicador de radio identificado por 'key' (ej. el efecto).</summary>
    public void SetRadiusMultiplier(object key, float multiplier)
    {
        if (key == null) return;
        _multipliers[key] = Mathf.Max(0f, multiplier);
        Recompute();
    }

    /// <summary>Quita el multiplicador asociado a 'key'.</summary>
    public void ClearRadiusMultiplier(object key)
    {
        if (key != null && _multipliers.Remove(key)) Recompute();
    }

    void Recompute()
    {
        float p = 1f;
        foreach (var v in _multipliers.Values) p *= v;
        _effective = p;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, VisionRadius);
    }
}
