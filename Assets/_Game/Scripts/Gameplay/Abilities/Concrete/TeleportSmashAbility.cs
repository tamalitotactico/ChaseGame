using UnityEngine;

/// <summary>
/// Habilidad 3 (definitiva) del Hunter: canaliza 1 seg (Hunter inmovilizado),
/// luego teletransporta en la direccion que tenia al activar la habilidad,
/// aplica Fear + Slow a todos los Prey en el AoE del destino, y aplica self-haste.
///
/// VFX:
///   - aoeFXPrefab  : efecto PlayOnce que queda en el punto de llegada.
///   - auraFXPrefab : efecto Attach que sigue al Hunter por auraDuration segundos.
///
/// Si hay un muro entre la posicion actual y el destino, aterriza justo antes del muro.
/// </summary>
public class TeleportSmashAbility : Ability
{
    readonly TeleportSmashAbilityData _d;

    static readonly Collider2D[] _hitBuffer = new Collider2D[24];

    VFXHandle _auraHandle;
    float     _auraTimer;

    public TeleportSmashAbility(TeleportSmashAbilityData d) : base(d) { _d = d; }

    // AimThenCastAimer: el jugador apunta con drag, al soltar empieza la
    // canalizacion fija. El TP usa la direccion capturada al inicio del cast.
    public override Aimer BeginActivation(in AbilityContext ctx) =>
        new AimThenCastAimer(_d.castTime);

    // Se llama en el frame exacto en que el aimer pasa a fase de canalizacion.
    public override void OnCastingBegan(in AbilityContext ctx)
    {
        // Groar pre-TP: sigue al hunter (aunque esta inmovil durante el canalizado).
        if (ctx.Owner != null)
            ServiceLocator.Resolve<IAudioService>()?.PlayAttached(_d.sfxOnCastStart, ctx.Owner.transform);
    }

    public override void Tick(float dt)
    {
        base.Tick(dt);

        // Apagar aura cuando expira su timer
        if (_auraHandle != null && !_auraHandle.IsStopped)
        {
            _auraTimer -= dt;
            if (_auraTimer <= 0f)
                _auraHandle.Stop();
        }
    }

    public override void Execute(in AbilityContext ctx, in AimResult aim)
    {
        // La direccion fue capturada durante la fase de aim del AimThenCastAimer
        // y congelada al pasar a fase de cast (release).
        Vector2 dir = aim.HasDirection
            ? aim.Direction
            : (ctx.MoveDirection.sqrMagnitude > 0.01f
                ? ctx.MoveDirection.normalized
                : ctx.FacingDirection);
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

        Vector2 from      = ctx.OwnerPosition;
        Vector2 desiredTo = from + dir * _d.teleportDistance;

        var hit = Physics2D.Raycast(from, dir, _d.teleportDistance, _d.wallLayer);
        Vector2 landingPos = hit.collider != null
            ? hit.point - dir * _d.wallPadding
            : desiredTo;

        var owner = ctx.Owner;
        var t = owner.transform;
        Vector3 currentPos = t.position;
        t.position = new Vector3(landingPos.x, landingPos.y, currentPos.z);
        if (owner.Motor != null) owner.Motor.Stop();

        // Audio y VFX en el punto de llegada
        ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(_d.sfxOnLanding, landingPos);
        if (_d.aoeFXPrefab != null)
            VFXSpawner.PlayOnce(_d.aoeFXPrefab, new Vector3(landingPos.x, landingPos.y, currentPos.z));

        // Aura VFX adjunta al Hunter (reemplaza la anterior si aun estaba activa)
        if (_d.auraFXPrefab != null)
        {
            _auraHandle?.Stop();
            _auraHandle = VFXSpawner.Attach(_d.auraFXPrefab, owner.transform);
            _auraTimer  = _d.auraDuration;
        }

        // AoE: aplicar Fear + Slow a Prey en el radio
        var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        filter.SetLayerMask(_d.preyLayer);
        int count = Physics2D.OverlapCircle(landingPos, _d.aoeRadius, filter, _hitBuffer);
        for (int i = 0; i < count; i++)
        {
            var col = _hitBuffer[i];
            if (col == null) continue;

            var c = col.GetComponentInParent<Character>();
            if (c == null || c == owner) continue;
            if (owner != null && c.Team == owner.Team) continue;
            if (!c.IsAlive) continue;
            if (c.StatusEffects == null) continue;

            Vector2 flee = ((Vector2)c.transform.position - landingPos).normalized;
            c.StatusEffects.Apply(new FearedEffect(_d.aoeFearDuration, flee));
            c.StatusEffects.Apply(new SlowedEffect(_d.aoeSlowDuration, _d.aoeSlowMultiplier));
        }

        // Self-buff de velocidad al Hunter
        if (owner.StatusEffects != null)
            owner.StatusEffects.Apply(new HastedEffect(_d.hasteDuration, _d.hasteMultiplier));
    }
}
