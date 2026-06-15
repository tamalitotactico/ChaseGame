using UnityEngine;

/// <summary>
/// Habilidad 1 del TrickyWizard: Tricky Lure. Crea un clon-senuelo que avanza recto (imita el sprite del
/// prey) y vuelve al prey INVISIBLE por invisDuration. El clon muere de 1 golpe soltando una risa.
///
/// Aim: DirectionalAimer (tap = facing; drag = direccion del clon).
/// </summary>
public class TrickyLureAbility : Ability
{
    readonly TrickyLureAbilityData _d;

    public TrickyLureAbility(TrickyLureAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => new DirectionalAimer();

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        var owner = ctx.Owner;
        if (owner == null) return;

        if (_d.clonePrefab != null && ctx.SpawnService != null)
        {
            Vector2 dir = aim.HasDirection && aim.Direction.sqrMagnitude > 0.01f
                ? aim.Direction.normalized
                : ctx.FacingDirection;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

            // Sin rotacion del GO: el clon imita la animacion 8-direccional del personaje, no se
            // orienta por transform (eso lo hacia verse "rotado"). Spawn con rotacion identidad.
            var go = ctx.SpawnService.Spawn(
                _d.clonePrefab,
                new Vector3(ctx.OwnerPosition.x, ctx.OwnerPosition.y, 0f),
                Quaternion.identity);

            if (go != null && go.TryGetComponent<TrickyLureClone>(out var clone))
                clone.Init(owner, dir, _d.cloneSpeed, _d.cloneLifetime, _d.cloneHealth,
                           _d.laughSfx, _d.wallPadding, _d.allyOutlineColor);
        }

        if (owner.StatusEffects != null && _d.invisDuration > 0f)
        {
            owner.StatusEffects.Apply(new InvisibleEffect(_d.invisDuration));
            if (_d.hasteMultiplier > 1f)
                owner.StatusEffects.Apply(new HastedEffect(_d.invisDuration, _d.hasteMultiplier));
        }
    }
}
