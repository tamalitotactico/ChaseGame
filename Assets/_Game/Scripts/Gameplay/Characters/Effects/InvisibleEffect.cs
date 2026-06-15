using UnityEngine;

/// <summary>
/// Invisibilidad total para ENEMIGOS (Tricky Lure): a diferencia del camuflaje, NO se ve por
/// distancia (EnemyRevealRadius = 0). Los aliados siempre lo ven. Se rompe al atacar o usar otra
/// habilidad. Reusa el canal de visibilidad por estado (StateVisibility).
///
/// Uso: owner.StatusEffects.Apply(new InvisibleEffect(1f));
/// </summary>
public class InvisibleEffect : StatusEffect
{
    public override bool  HidesFromEnemies    => true;
    public override float EnemyRevealRadius    => 0f;   // invisible total para enemigos
    public override bool  BreaksOnOwnerAction  => true;
    // El look para quien lo ve (aliado/uno mismo) lo da el shader, no un tint plano.
    public override StealthStyle Stealth       => StealthStyle.Invisible;
    public override Color StealthColor         => new Color(0.55f, 0.7f, 1f, 1f);
    public override string IconId              => "invisible";

    public InvisibleEffect(float duration)
    {
        Duration = Remaining = duration;
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
