using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador de vida flotante (World Space Canvas) para cualquier Character.
/// Se oculta cuando la vida esta al maximo; se muestra al recibir el primer golpe.
///
/// Cuando el personaje esta downed se muestra una barra de bleed-out (roja) y
/// una barra de progreso de revive (verde). Ambas son referencias opcionales:
/// si no estan asignadas en el inspector, se ignoran.
///
/// CharacterVisuals llama a UpdateHealth / ShowDowned / ShowReviving / HideDowned.
/// </summary>
public class FloatingHealthBar : MonoBehaviour
{
    [Header("Health bar")]
    [Tooltip("Contenedor raiz del canvas de la barra.")]
    [SerializeField] GameObject barRoot;
    [Tooltip("Imagen de relleno de la barra de vida.")]
    [SerializeField] Image fillImage;
    [Tooltip("Gradiente de color (verde → amarillo → rojo).")]
    [SerializeField] Gradient colorGradient;

    [Header("Downed state (opcional)")]
    [Tooltip("Slider que representa el tiempo de bleed-out restante (de 1 a 0).")]
    [SerializeField] Slider bleedOutSlider;
    [Tooltip("Slider que representa el progreso de revive (de 0 a 1).")]
    [SerializeField] Slider reviveProgressSlider;

    void Awake()
    {
        transform.rotation = Quaternion.identity;
        if (barRoot != null) barRoot.SetActive(false);
        if (bleedOutSlider != null)       bleedOutSlider.gameObject.SetActive(false);
        if (reviveProgressSlider != null) reviveProgressSlider.gameObject.SetActive(false);
    }

    /// <param name="currentHits">HP actual</param>
    /// <param name="maxHits">HP maximo</param>
    public void UpdateHealth(int currentHits, int maxHits)
    {
        if (fillImage == null) return;

        float pct = 1f - Mathf.Clamp01(maxHits > 0 ? (float)currentHits / maxHits : 0f);
        fillImage.fillAmount = Mathf.Clamp01(1f - pct); // fillAmount = porcion de vida restante

        if (colorGradient != null)
            fillImage.color = colorGradient.Evaluate(1f - pct);

        if (barRoot != null)
            barRoot.SetActive(currentHits < maxHits && currentHits > 0);
    }

    /// <summary>Muestra la barra vacia y roja (estado muerto).</summary>
    public void ShowDead()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.color = Color.red;
        }
        if (barRoot != null) barRoot.SetActive(true);
        if (bleedOutSlider != null)       bleedOutSlider.gameObject.SetActive(false);
        if (reviveProgressSlider != null) reviveProgressSlider.gameObject.SetActive(false);
    }

    /// <summary>Entra al estado downed: oculta barra de HP, muestra sliders.</summary>
    public void ShowDowned()
    {
        if (fillImage != null) fillImage.enabled = false;

        if (bleedOutSlider != null)
        {
            bleedOutSlider.gameObject.SetActive(true);
            bleedOutSlider.value = 1f;
        }
        if (reviveProgressSlider != null)
        {
            reviveProgressSlider.gameObject.SetActive(true);
            reviveProgressSlider.value = 0f;
        }
        if (barRoot != null) barRoot.SetActive(true);
    }

    /// <summary>Actualiza los sliders de bleed-out y revive progress cada frame.</summary>
    /// <param name="bleedOutNorm">Fraccion restante de bleed-out [1..0]</param>
    /// <param name="reviveNorm">Progreso de revive [0..1]</param>
    public void ShowReviving(float bleedOutNorm, float reviveNorm)
    {
        if (bleedOutSlider != null)       bleedOutSlider.value       = bleedOutNorm;
        if (reviveProgressSlider != null) reviveProgressSlider.value = reviveNorm;
    }

    /// <summary>Sale del estado downed: restaura barra de HP, oculta sliders.</summary>
    public void HideDowned()
    {
        if (fillImage != null) fillImage.enabled = true;
        if (bleedOutSlider != null)       bleedOutSlider.gameObject.SetActive(false);
        if (reviveProgressSlider != null) reviveProgressSlider.gameObject.SetActive(false);
    }
}
