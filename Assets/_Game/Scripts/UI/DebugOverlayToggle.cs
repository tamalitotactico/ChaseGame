using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggle de gizmos de debug via tecla configurable (default F1).
/// Agregar a cualquier GameObject en la escena (recomendado: hijo del Canvas).
/// Muestra opcionalmente un label en pantalla con el estado actual.
/// </summary>
public class DebugOverlayToggle : MonoBehaviour
{
    [SerializeField] KeyCode toggleKey  = KeyCode.F1;
    [SerializeField] Text    statusLabel;   // opcional, Text legacy o null

    void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;

        DebugGizmoSettings.MasterEnabled = !DebugGizmoSettings.MasterEnabled;

        if (statusLabel != null)
            statusLabel.text = "Gizmos: " + (DebugGizmoSettings.MasterEnabled ? "ON" : "OFF");
    }
}
