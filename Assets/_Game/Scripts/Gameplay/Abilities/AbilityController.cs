using System;
using UnityEngine;

/// <summary>
/// Componente sibling del Character. Maneja el lifecycle de las abilities:
/// activacion, aim phase, cast phase, cooldowns. Lee BrainIntent inyectado por
/// el Character.
///
/// Modelo de input (estilo Brawl Stars):
///   - Press: empieza la fase de aim (siempre, incluso para tap-only via TapAimer).
///     NUNCA se ejecuta nada en este frame.
///   - Drag (en abilities apuntables): actualiza la direccion en AimInput.
///   - Released: el Aimer decide via HandleRelease:
///       Fire → ejecutar y poner en cooldown.
///       Cancel → descartar (no ejecuta, no entra en cooldown).
///       Continue → seguir activo (ej. AimThenCast pasa a fase de cast).
///   - IsComplete (timer de canalizacion expirado) → ejecutar automaticamente.
///
/// Phase 3: las llamadas a Execute() se vuelven RPC server-authoritative.
/// La fase de Aim sigue siendo 100% local.
///
/// Eventos para UI:
///   - OnCooldownChanged: cualquier ability, cada frame con su fill 0..1.
///   - OnAimingStarted/Stopped: fase de aim activa (drag) — sirve para mostrar
///     preview de aim. No es canalizacion.
///   - OnCastingStarted/Tick/Stopped: solo durante canalizacion REAL (aimer.IsCasting).
///     La CastingBarUI usa estos.
///   - OnAbilityExecuted: confirma fire.
///
/// Nota: NO usamos [RequireComponent(Character)] porque crearia un ciclo con
/// Character.[RequireComponent(AbilityController)]. La presencia de Character
/// se valida en Awake.
/// </summary>
public class AbilityController : MonoBehaviour
{
    public event Action<int, float, float> OnCooldownChanged;  // (slot, fillAmount 0..1, remainingSeconds)
    public event Action<int>          OnAimingStarted;         // (slot) — fase de aim comienza
    public event Action               OnAimingStopped;         // fase de aim termina (por fire/cancel/transicion a cast)
    public event Action<int>          OnCastingStarted;        // (slot) — canalizacion real comienza
    public event Action<float, float> OnCastingTick;           // (progress 0..1, remainingSeconds)
    public event Action               OnCastingStopped;        // canalizacion termina o se cancela
    public event Action<int>          OnAbilityExecuted;

    Character     _character;
    Ability[]     _runtime;
    Aimer         _activeAimer;
    int           _activeSlot   = -1;
    bool          _wasCasting;       // edge detection para OnCastingStarted/Stopped

    public Ability[] Abilities => _runtime;
    public bool      IsAiming   => _activeAimer != null;
    public int       AimingSlot => _activeSlot;
    /// <summary>Aimer activo (null si no esta apuntando). Read-only para UI/indicadores.</summary>
    public Aimer     ActiveAimer => _activeAimer;

    public AbilityData GetAbilityData(int slot) =>
        (_runtime != null && slot >= 0 && slot < _runtime.Length && _runtime[slot] != null)
        ? _runtime[slot].Data : null;

    void Awake()
    {
        _character = GetComponent<Character>();
    }

    /// <summary>Llamado por Character.Initialize() tras leer CharacterData.</summary>
    public void Bind(AbilityData[] datas)
    {
        if (datas == null) { _runtime = Array.Empty<Ability>(); return; }

        _runtime = new Ability[datas.Length];
        for (int i = 0; i < datas.Length; i++)
            _runtime[i] = datas[i] != null ? datas[i].CreateRuntime() : null;
    }

    /// <summary>Drive principal. Llamar desde Character cada frame.</summary>
    public void Tick(in BrainIntent intent, float dt)
    {
        // 1. Cooldowns
        if (_runtime != null)
        {
            for (int i = 0; i < _runtime.Length; i++)
            {
                if (_runtime[i] == null) continue;
                _runtime[i].Tick(dt);
                float cd        = _runtime[i].Data.cooldown;
                float remaining = Mathf.Max(0f, _runtime[i].CooldownRemaining);
                float norm      = cd > 0 ? 1f - (remaining / cd) : 1f;
                OnCooldownChanged?.Invoke(i, Mathf.Clamp01(norm), remaining);
            }
        }

        // 2. Lifecycle
        if (_activeAimer == null)
            TryStartActivation(in intent);
        else
            DriveAim(in intent);
    }

    void TryStartActivation(in BrainIntent intent)
    {
        if (_runtime == null || _runtime.Length == 0) return;

        AbilityInputState s0 = intent.Slot0;
        AbilityInputState s1 = intent.Slot1;
        AbilityInputState s2 = intent.Slot2;

        int slot = -1;
        if (s0 == AbilityInputState.Pressed && SlotReady(0)) slot = 0;
        else if (s1 == AbilityInputState.Pressed && SlotReady(1)) slot = 1;
        else if (s2 == AbilityInputState.Pressed && SlotReady(2)) slot = 2;

        if (slot < 0) return;

        var ctx   = BuildContext(in intent);
        var aimer = _runtime[slot].BeginActivation(in ctx);

        // Si la ability no necesita aim, usamos TapAimer para uniformar el
        // gesto: la habilidad se dispara al Released, no al Pressed.
        if (aimer == null) aimer = new TapAimer();

        aimer.Begin(in ctx);
        _activeAimer = aimer;
        _activeSlot  = slot;
        _wasCasting  = false;
        OnAimingStarted?.Invoke(slot);

        var aimStartCue = _runtime[slot].Data.sfxOnAimStart;
        if (aimStartCue != null)
            ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(aimStartCue, _character.transform.position);
    }

    void DriveAim(in BrainIntent intent)
    {
        _activeAimer.Tick(in intent);

        // Detectar transicion Aim → Cast para disparar OnCastingStarted/Tick/Stopped.
        bool nowCasting = _activeAimer.IsCasting;
        if (nowCasting && !_wasCasting)
        {
            _wasCasting = true;
            OnCastingStarted?.Invoke(_activeSlot);
            var castCtx = BuildContext(in intent);
            _runtime[_activeSlot].OnCastingBegan(in castCtx);
        }
        else if (!nowCasting && _wasCasting)
        {
            _wasCasting = false;
            OnCastingStopped?.Invoke();
        }
        if (_wasCasting)
            OnCastingTick?.Invoke(_activeAimer.Progress, _activeAimer.RemainingSeconds);

        // Auto-ejecutar al completarse la canalizacion (timer expirado).
        if (_activeAimer.IsComplete)
        {
            FinishAndExecute(in intent);
            return;
        }

        // Procesar Released del slot activo.
        AbilityInputState slotState = _activeSlot switch
        {
            0 => intent.Slot0,
            1 => intent.Slot1,
            2 => intent.Slot2,
            _ => AbilityInputState.None
        };

        if (slotState == AbilityInputState.Released)
        {
            var decision = _activeAimer.HandleRelease(in intent);
            switch (decision)
            {
                case ReleaseDecision.Fire:
                    FinishAndExecute(in intent);
                    return;
                case ReleaseDecision.Cancel:
                    CleanupAimer(cancelled: true);
                    return;
                case ReleaseDecision.Continue:
                    // El aimer sigue activo (ej. transicion a cast phase).
                    break;
            }
        }
        else if (slotState == AbilityInputState.None && _activeAimer.IsCancellable)
        {
            // Perdida de input sin Released formal (slot desconectado, switch de
            // brain, etc). Solo cancelar si el aimer lo permite.
            CleanupAimer(cancelled: true);
        }
    }

    void FinishAndExecute(in BrainIntent intent)
    {
        int slot = _activeSlot;
        var ctx  = BuildContext(in intent);
        var aim  = _activeAimer.GetResult();
        _activeAimer.End();

        bool wasCasting = _wasCasting;
        _activeAimer = null;
        _activeSlot  = -1;
        _wasCasting  = false;

        if (wasCasting) OnCastingStopped?.Invoke();
        OnAimingStopped?.Invoke();

        var castCue = _runtime[slot].Data.sfxOnCast;
        if (castCue != null)
            ServiceLocator.Resolve<IAudioService>()?.PlayAtPoint(castCue, _character.transform.position);

        _runtime[slot].Execute(in ctx, in aim);
        _runtime[slot].StartCooldown();
        OnAbilityExecuted?.Invoke(slot);
    }

    void CleanupAimer(bool cancelled)
    {
        _activeAimer?.Cancel();
        bool wasCasting = _wasCasting;
        _activeAimer = null;
        _activeSlot  = -1;
        _wasCasting  = false;

        if (wasCasting) OnCastingStopped?.Invoke();
        OnAimingStopped?.Invoke();
    }

    bool SlotReady(int slot)
    {
        return slot < _runtime.Length && _runtime[slot] != null && _runtime[slot].IsReady;
    }

    AbilityContext BuildContext(in BrainIntent intent)
    {
        return new AbilityContext
        {
            Owner            = _character,
            OwnerPosition    = _character.transform.position,
            MoveDirection    = intent.MoveInput,
            FacingDirection  = _character.FacingDirection,
            Authority        = _character.Authority,
            SpawnService     = ServiceLocator.Resolve<ISpawnService>()
        };
    }
}
