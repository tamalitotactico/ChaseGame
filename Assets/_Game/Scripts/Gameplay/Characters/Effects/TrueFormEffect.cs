using UnityEngine;

/// <summary>
/// True Form del Werewolf (ult): bundle temporizado que mientras dura otorga (a) gran
/// velocidad (SpeedModifier) y (b) ataque basico LETAL (GrantsLethalAttack -> CombatController
/// estampa DamageInfo.Lethal -> el golpe derriba al prey en 1, sin importar su HP). Pinta un
/// tint de transformacion.
///
/// NO es un efecto de control (es un self-buff): la inmunidad a CC no lo bloquea y Repel no lo
/// dispela. La letalidad se modela "DownInOne" via el canal Lethal ya existente. Un modo
/// DamageMultiplier alternativo seria infra futura (canal de multiplicador de dano en combate).
///
/// Uso: owner.StatusEffects.Apply(new TrueFormEffect(4f, 1.6f, tint));
/// </summary>
public class TrueFormEffect : StatusEffect
{
    readonly float _haste;
    readonly Color _tint;

    public override bool  GrantsLethalAttack => true;
    public override float SpeedModifier      => _haste;
    public override Color VisualTint         => _tint;
    public override int   VisualPriority     => 30;
    public override string IconId            => "trueform";

    /// <param name="duration">Duracion de la transformacion en segundos.</param>
    /// <param name="hasteMultiplier">Multiplicador de velocidad (>= 1).</param>
    /// <param name="tint">Tint de overlay durante la transformacion.</param>
    public TrueFormEffect(float duration, float hasteMultiplier, Color tint)
    {
        Duration = Remaining = duration;
        _haste   = Mathf.Max(1f, hasteMultiplier);
        _tint    = tint;
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
