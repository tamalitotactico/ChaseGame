using UnityEngine;

/// <summary>
/// Handle de ciclo de vida para un efecto VFX adjunto a un personaje.
/// Obtenido via VFXSpawner.Attach; descartado llamando Stop().
///
/// Stop() detiene la emision y destruye el GO despues de que las particulas
/// existentes terminen su vida, sin cortar el efecto bruscamente.
/// </summary>
public class VFXHandle
{
    readonly GameObject _go;
    bool _stopped;

    internal VFXHandle(GameObject go) { _go = go; }

    /// <summary>Detiene la emision y destruye el GO al cabo de la duracion maxima de particula.</summary>
    public void Stop()
    {
        if (_stopped || _go == null) return;
        _stopped = true;

        float longestLife = 0f;
        foreach (var ps in _go.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            longestLife = Mathf.Max(longestLife, ps.main.startLifetime.constantMax);
        }

        Object.Destroy(_go, Mathf.Max(longestLife, 0.5f));
    }

    public bool IsStopped => _stopped || _go == null;
}
