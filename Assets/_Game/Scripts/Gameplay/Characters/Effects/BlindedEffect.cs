using UnityEngine;

/// <summary>
/// Cegado: reduce el radio de vision (FOV) del afectado por un multiplicador, mientras dura.
/// Aplica el multiplicador sobre el VisionSource del personaje (item 6); si no tiene VisionSource
/// (ej. un bot), no hace nada visible. Lo usan Smoke Trap (Trapper) y Disparo cegador (TrickyWizard).
///
/// Es un efecto negativo de control (lo dispela Repel / lo bloquea la inmunidad a CC).
///
/// Uso: target.StatusEffects.Apply(new BlindedEffect(2f, 0.4f)); // 2s a 40% de vision
/// </summary>
public class BlindedEffect : StatusEffect
{
    readonly float _fovMultiplier;

    public override bool  IsControlEffect => true;
    public override Color VisualTint      => new Color(0.1f, 0.1f, 0.25f, 0.5f);
    public override int   VisualPriority  => 12;
    public override string IconId         => "blind";

    /// <param name="duration">Duracion en segundos.</param>
    /// <param name="fovMultiplier">Fraccion del radio de vision [0..1]. 0.4 = 40% de la vision.</param>
    public BlindedEffect(float duration, float fovMultiplier)
    {
        Duration       = Remaining = duration;
        _fovMultiplier = Mathf.Clamp01(fovMultiplier);
    }

    public override void OnApply(Character target)
    {
        var vs = target != null ? target.GetComponentInChildren<VisionSource>() : null;
        if (vs != null) vs.SetRadiusMultiplier(this, _fovMultiplier);
    }

    public override void OnRemove(Character target)
    {
        var vs = target != null ? target.GetComponentInChildren<VisionSource>() : null;
        if (vs != null) vs.ClearRadiusMultiplier(this);
    }
}
