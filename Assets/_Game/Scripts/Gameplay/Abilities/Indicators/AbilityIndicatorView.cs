using UnityEngine;

/// <summary>
/// Vive en el root del Character LOCAL (jugador). Se suscribe a los eventos
/// del AbilityController y muestra el indicator del AbilityData activo durante
/// el aim.
///
/// Lifecycle:
///   - OnAimingStarted(slot): instancia AbilityData.indicatorPrefab como child,
///     llama Begin(owner, data).
///   - Update: cada frame, si hay indicador activo, llama Tick(origin, dir)
///     con la posicion del owner y la direccion actual del Aimer.
///   - OnAimingStopped: llama End() y destruye el GameObject.
///
/// Si la ability no tiene indicatorPrefab asignado, no hace nada (silent).
///
/// Auto-attach: agregar este componente al root del Hunter/Prey prefab, o que
/// algun bootstrap (similar a CharacterFogVisibility) lo agregue al spawn.
/// </summary>
public class AbilityIndicatorView : MonoBehaviour
{
    Character          _character;
    AbilityController  _ac;
    AimIndicator       _current;
    bool               _subscribed;

    void Awake()
    {
        _character = GetComponent<Character>();
    }

    void OnEnable()
    {
        _ac = _character != null ? _character.Abilities : null;
        if (_ac == null) return;
        if (_subscribed) return;
        _ac.OnAimingStarted += OnAimStart;
        _ac.OnAimingStopped += OnAimStop;
        _subscribed = true;
    }

    void OnDisable()
    {
        if (!_subscribed || _ac == null) return;
        _ac.OnAimingStarted -= OnAimStart;
        _ac.OnAimingStopped -= OnAimStop;
        _subscribed = false;
        DestroyCurrent();
    }

    void OnAimStart(int slot)
    {
        DestroyCurrent();
        var data = _ac.GetAbilityData(slot);
        if (data == null || data.indicatorPrefab == null) return;

        // instantiateInWorldSpace=true hace que el indicador preserve el scale/rotation/position
        // del prefab en WORLD space, ignorando el lossyScale del Character. Sin esto, si el
        // Character tiene scale != 1 (comun en sprites), el indicador hereda esa escala
        // y las dimensiones nunca son exactas al valor de IndicatorRange/Radius/Width.
        var go = (GameObject)Instantiate(data.indicatorPrefab, _character.transform, true);
        _current = go.GetComponent<AimIndicator>();
        if (_current == null)
        {
            Debug.LogWarning($"[AbilityIndicatorView] Prefab '{data.indicatorPrefab.name}' no tiene AimIndicator.", go);
            Destroy(go);
            return;
        }
        _current.Begin(_character, data);
        // Tick inicial inmediato para evitar 1-frame de indicador descolocado.
        _current.Tick(_character.transform.position, _ac.ActiveAimer != null
            ? _ac.ActiveAimer.CurrentDirection
            : _character.FacingDirection);
    }

    void OnAimStop()
    {
        DestroyCurrent();
    }

    void DestroyCurrent()
    {
        if (_current == null) return;
        _current.End();
        Destroy(_current.gameObject);
        _current = null;
    }

    void Update()
    {
        if (_current == null || _ac == null || _ac.ActiveAimer == null) return;
        _current.Tick(_character.transform.position, _ac.ActiveAimer.CurrentDirection);
    }
}
