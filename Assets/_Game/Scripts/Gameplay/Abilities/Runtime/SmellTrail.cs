using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trail visual de Smell (Werewolf): un LineRenderer que dibuja la ruta (waypoints) hacia un
/// prey y se auto-destruye tras 'duration'. Snapshot: los puntos se fijan al crear, no se
/// recalculan. Ancho/color/material son del prefab (designer-tunable); duracion y puntos los
/// inyecta la habilidad.
///
/// Prefab requirements: LineRenderer + SmellTrail.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SmellTrail : MonoBehaviour
{
    LineRenderer _lr;
    float _timer;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
    }

    public void Setup(IReadOnlyList<Vector3> points, float duration, float width, Color color)
    {
        if (_lr == null) _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = true;
        _lr.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            _lr.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
        _lr.startWidth = _lr.endWidth = width;
        _lr.startColor = _lr.endColor = color;
        _timer = duration;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f) Destroy(gameObject);
    }
}
