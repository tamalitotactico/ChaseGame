using UnityEngine;

/// <summary>
/// Camuflaje del Drowned (Nadar): oculto para los ENEMIGOS con fade por distancia (visible si un
/// enemigo esta a &lt; revealRadius; cuanto mas cerca, mas visible). Los aliados siempre lo ven.
/// Incluye un buff de velocidad (haste). Se ROMPE al atacar o usar otra habilidad.
///
/// No es un efecto de control (es self-stealth): la inmunidad a CC no lo bloquea ni Repel lo dispela.
/// La regla de visibilidad vive en StateVisibility; aca solo se declara el canal + el radio.
///
/// Uso: owner.StatusEffects.Apply(new CamouflageEffect(2f, 1.4f, 4f));
/// </summary>
public class CamouflageEffect : StatusEffect
{
    readonly float _haste;
    readonly float _revealRadius;

    public override float SpeedModifier       => _haste;
    public override bool  HidesFromEnemies     => true;
    public override float EnemyRevealRadius    => _revealRadius;
    public override bool  BreaksOnOwnerAction  => true;
    public override StealthStyle Stealth       => StealthStyle.Camouflage;
    public override Color StealthColor         => new Color(0.45f, 0.85f, 0.6f, 1f); // verdoso
    public override string IconId              => "camo";

    /// <param name="duration">Duracion del camuflaje en segundos.</param>
    /// <param name="hasteMultiplier">Multiplicador de velocidad (>= 1).</param>
    /// <param name="revealRadius">Distancia a la que un enemigo empieza a verlo (fade dentro del radio).</param>
    public CamouflageEffect(float duration, float hasteMultiplier, float revealRadius)
    {
        Duration      = Remaining = duration;
        _haste        = Mathf.Max(1f, hasteMultiplier);
        _revealRadius = Mathf.Max(0.01f, revealRadius);
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
