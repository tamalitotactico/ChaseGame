using System;
using UnityEngine;

/// <summary>
/// Habilita el flujo Downed/Revive en un Character (solo Prey en Phase 1).
///
/// Muerte real desactivada: el downed dura indefinidamente. Solo el revive
/// por aliado saca al personaje del downed.
///
/// Lifecycle:
///  - Character.HandleDied llama BeginDown() (siempre, sin chequeo de settings).
///  - El DownedState tickea Revivable.Tick(dt) cada frame.
///  - Tick escanea GameManager.Preys: si hay >=1 prey ALIVE dentro de
///    reviveProximityRadius, ReviveProgress sube en 1/reviveDuration por segundo.
///  - Si no hay reviver, ReviveProgress decae a reviveDecaySpeed/segundo.
///  - Al llegar ReviveProgress a 1 → Revive(): restablece HP al revivedHealthPct,
///    publica CharacterRevivedEvent, dispara OnRevived.
/// </summary>
public class RevivableComponent : MonoBehaviour
{
    [Header("Tuning")]
    [Tooltip("Segundos cerca de un aliado para completar revive.")]
    [SerializeField] float reviveDuration         = 3f;

    [Tooltip("Proporcion del progreso (0..1) perdida por segundo cuando no hay reviver.")]
    [SerializeField] float reviveDecaySpeed       = 0.5f;

    [Tooltip("Distancia maxima para que un aliado vivo cuente como reviver.")]
    [SerializeField] float reviveProximityRadius  = 1.2f;

    [Tooltip("Segundos que aguanta el downed antes de morir por bleed-out.")]
    [SerializeField] float bleedOutDuration       = 30f;

    [Tooltip("Porcentaje (0..100) de HP con el que vuelve a estar vivo.")]
    [Range(1, 100)]
    [SerializeField] int revivedHealthPct         = 50;

    public bool   IsDowned              { get; private set; }
    public float  ReviveProgress        { get; private set; } // 0..1
    public float  BleedOutRemaining     { get; private set; }
    public int    RevivesUsed           { get; private set; }
    public float  ReviveDuration        => reviveDuration;
    public float  ReviveProximityRadius => reviveProximityRadius;
    public float  BleedOutDuration      => bleedOutDuration;

    public event Action OnRevived;

    Character _character;

    void Awake()
    {
        _character = GetComponent<Character>();
    }

    public void BeginDown()
    {
        IsDowned = true;
        ReviveProgress = 0f;
        // Bleed-out desactivado: el downed dura indefinidamente. Se conserva el
        // campo para UI/tuning pero no decrementa.
        BleedOutRemaining = bleedOutDuration;
    }

    /// <summary>Tickea desde DownedState cada frame.</summary>
    public void Tick(float dt)
    {
        if (!IsDowned) return;

        // Scan reviver: cualquier ally Prey vivo dentro de proximityRadius
        bool hasReviver = HasAnyReviverInRange();
        if (hasReviver)
        {
            ReviveProgress += dt / Mathf.Max(0.01f, reviveDuration);
            if (ReviveProgress >= 1f)
            {
                ReviveProgress = 1f;
                Revive();
                return;
            }
        }
        else if (ReviveProgress > 0f)
        {
            ReviveProgress = Mathf.Max(0f, ReviveProgress - dt * reviveDecaySpeed);
        }

        EventBus.Publish(new ReviveProgressChangedEvent
        {
            Character         = _character,
            Progress          = ReviveProgress,
            BleedOutRemaining = BleedOutRemaining,
            HasReviver        = hasReviver
        });
    }

    bool HasAnyReviverInRange()
    {
        var gm = GameManager.Instance;
        if (gm == null || _character == null) return false;
        float r2 = reviveProximityRadius * reviveProximityRadius;
        Vector2 my = _character.transform.position;

        // El reviver debe ser otro Prey, vivo (no downed, no dead).
        foreach (var c in gm.Preys)
        {
            if (c == null || c == _character) continue;
            if (!c.IsAlive || c.IsDowned) continue;
            if (((Vector2)c.transform.position - my).sqrMagnitude <= r2)
                return true;
        }
        return false;
    }

    void Revive()
    {
        IsDowned = false;
        RevivesUsed++;
        ReviveProgress = 0f;
        BleedOutRemaining = 0f;

        if (_character != null && _character.Health != null)
        {
            int target = Mathf.Max(1, _character.Health.MaxHealth * revivedHealthPct / 100);
            _character.Health.SetCurrent(target);
        }

        EventBus.Publish(new CharacterRevivedEvent { Character = _character });
        OnRevived?.Invoke();
    }

    /// <summary>Llamado externamente (Hunter finish). Actualmente no-op: la
    /// muerte real esta desactivada y los downed son invulnerables.</summary>
    public void KillFromFinish() { }
}
