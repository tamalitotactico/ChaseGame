using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona los efectos de estado activos de un Character (stun, slow, mark, etc.).
///
/// Uso:
///   character.StatusEffects.Apply(new StunnedEffect(2f));
///   character.StatusEffects.Remove<SlowedEffect>();
///   bool stunned = character.StatusEffects.Has<StunnedEffect>();
///
/// Cada vez que cambia el conjunto de efectos activos, recalcula el SpeedMultiplier
/// del CharacterMotor automaticamente.
/// </summary>
public class StatusEffectController : MonoBehaviour
{
    public event Action<StatusEffect> OnEffectApplied;
    public event Action<StatusEffect> OnEffectRemoved;

    readonly List<StatusEffect> _active = new();
    Character _char;

    void Awake()
    {
        _char = GetComponent<Character>();
    }

    // --- Propiedades agregadas ---

    public bool CanMove
    {
        get
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].BlocksMovement) return false;
            return true;
        }
    }

    public bool CanAct
    {
        get
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].BlocksActions) return false;
            return true;
        }
    }

    /// <summary>True si algun efecto activo otorga inmunidad a control (rechaza nuevos CC).</summary>
    public bool HasCCImmunity
    {
        get
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].GrantsCCImmunity) return true;
            return false;
        }
    }

    /// <summary>True si algun efecto activo vuelve LETAL el ataque basico del owner (True Form).</summary>
    public bool HasLethalAttack
    {
        get
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].GrantsLethalAttack) return true;
            return false;
        }
    }

    /// <summary>Producto de todos los SpeedModifier activos. Resultado >= 0.</summary>
    public float AggregatedSpeedModifier
    {
        get
        {
            float m = 1f;
            for (int i = 0; i < _active.Count; i++)
                m *= _active[i].SpeedModifier;
            return Mathf.Max(0f, m);
        }
    }

    /// <summary>
    /// Tint de overlay del efecto activo con mayor VisualPriority (alpha > 0).
    /// Transparente si ningun efecto pinta. Usado por CharacterVisuals: reemplaza el
    /// switch por tipo, permitiendo agregar efectos nuevos sin tocar CharacterVisuals.
    /// </summary>
    public Color GetTopVisualTint()
    {
        StatusEffect top = null;
        for (int i = 0; i < _active.Count; i++)
        {
            var e = _active[i];
            if (e.VisualTint.a <= 0f) continue;
            if (top == null || e.VisualPriority > top.VisualPriority) top = e;
        }
        return top != null ? top.VisualTint : new Color(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Devuelve el primer ForceMoveInput no-null de los efectos activos, o null si ninguno
    /// sobreescribe el input. En la practica solo FearedEffect lo usa.
    /// </summary>
    public Vector2? GetForceMoveInput()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            var f = _active[i].ForceMoveInput;
            if (f.HasValue) return f;
        }
        return null;
    }

    // --- API ---

    /// <summary>
    /// Aplica un efecto. Si ya existe uno del mismo tipo, lo refresca (no apila).
    /// Si el owner tiene inmunidad a control y el efecto es de control, se rechaza
    /// (devuelve false sin aplicarlo). Devuelve true si quedo aplicado.
    /// </summary>
    public bool Apply(StatusEffect effect)
    {
        if (effect == null) return false;
        if (effect.IsControlEffect && HasCCImmunity) return false;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].GetType() == effect.GetType())
            {
                var old = _active[i];
                old.OnRemove(_char);
                _active.RemoveAt(i);
                OnEffectRemoved?.Invoke(old);
                EventBus.Publish(new StatusEffectRemovedEvent { Character = _char, Effect = old });
                break;
            }
        }

        _active.Add(effect);
        effect.OnApply(_char);
        OnEffectApplied?.Invoke(effect);
        EventBus.Publish(new StatusEffectAppliedEvent { Character = _char, Effect = effect });
        RefreshMotor();
        return true;
    }

    /// <summary>Efecto activo que oculta al owner de enemigos (camuflaje/invisible), o null. Lo consulta
    /// StateVisibility para el alpha del sprite. Si hay varios, devuelve el primero.</summary>
    public StatusEffect GetHidingEffect()
    {
        for (int i = 0; i < _active.Count; i++)
            if (_active[i].HidesFromEnemies) return _active[i];
        return null;
    }

    /// <summary>Remueve los efectos marcados BreaksOnOwnerAction (camuflaje/invisible). Lo llaman
    /// CombatController (al atacar) y AbilityController (al ejecutar una habilidad). Devuelve cuantos removio.</summary>
    public int BreakActionSensitiveEffects()
    {
        int removed = 0;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (!_active[i].BreaksOnOwnerAction) continue;
            var e = _active[i];
            e.OnRemove(_char);
            _active.RemoveAt(i);
            OnEffectRemoved?.Invoke(e);
            EventBus.Publish(new StatusEffectRemovedEvent { Character = _char, Effect = e });
            removed++;
        }
        if (removed > 0) RefreshMotor();
        return removed;
    }

    /// <summary>Remueve TODOS los efectos de control activos (fear/slow/stun/charm); deja los buffs.
    /// Devuelve cuantos removio. Lo usa Repel del Charmer (dispel fuerte).</summary>
    public int DispelControlEffects()
    {
        int removed = 0;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (!_active[i].IsControlEffect) continue;
            var e = _active[i];
            e.OnRemove(_char);
            _active.RemoveAt(i);
            OnEffectRemoved?.Invoke(e);
            EventBus.Publish(new StatusEffectRemovedEvent { Character = _char, Effect = e });
            removed++;
        }
        if (removed > 0) RefreshMotor();
        return removed;
    }

    /// <summary>Remueve el primer efecto del tipo T que este activo.</summary>
    public void Remove<T>() where T : StatusEffect
    {
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i] is T)
            {
                var e = _active[i];
                e.OnRemove(_char);
                _active.RemoveAt(i);
                OnEffectRemoved?.Invoke(e);
                EventBus.Publish(new StatusEffectRemovedEvent { Character = _char, Effect = e });
                RefreshMotor();
                return;
            }
        }
    }

    public bool Has<T>() where T : StatusEffect
    {
        for (int i = 0; i < _active.Count; i++)
            if (_active[i] is T) return true;
        return false;
    }

    public T Get<T>() where T : StatusEffect
    {
        for (int i = 0; i < _active.Count; i++)
            if (_active[i] is T t) return t;
        return null;
    }

    void Update()
    {
        bool changed = false;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            _active[i].Tick(Time.deltaTime);
            if (_active[i].IsExpired)
            {
                var e = _active[i];
                e.OnRemove(_char);
                _active.RemoveAt(i);
                OnEffectRemoved?.Invoke(e);
                EventBus.Publish(new StatusEffectRemovedEvent { Character = _char, Effect = e });
                changed = true;
            }
        }

        if (changed) RefreshMotor();
    }

    void RefreshMotor()
    {
        if (_char == null || _char.Motor == null) return;
        _char.Motor.SpeedMultiplier = CanMove ? AggregatedSpeedModifier : 0f;
    }

    // ===== Replicacion de red (Hito 4) =====
    // El host construye una MASCARA de bits con los tipos de efecto activos (GetActiveTypeMask) y la
    // replica via [Networked]. El cliente la consume con SyncFromMask, reconstruyendo efectos VISUALES
    // (duracion gigante; el host controla el alta/baja por la mascara). Esto reusa los eventos
    // OnEffectApplied/Removed -> StatusIconDisplay (iconos) y CharacterVisuals (tint/stealth/VFX)
    // funcionan en el cliente SIN cambios. Seguro: en el cliente el motor es network-driven, asi que
    // SpeedModifier/ForceMoveInput/Blocks* (que solo lee el host en FixedUpdateNetwork) no afectan.
    //
    // ORDEN ESTABLE = indice de bit. El mismo codigo corre en host y cliente -> mismo orden.
    static readonly System.Type[] _netTypes =
    {
        typeof(StunnedEffect), typeof(SlowedEffect), typeof(FearedEffect), typeof(HastedEffect),
        typeof(CharmedEffect), typeof(BlindedEffect), typeof(CamouflageEffect), typeof(InvisibleEffect),
        typeof(CCImmunityEffect), typeof(TrueFormEffect),
    };

    // Factories para reconstruir efecto VISUAL en el cliente (params no importan para iconos/tint:
    // esos son constantes por tipo, salvo TrueForm cuyo tint usamos aproximado).
    static readonly System.Func<StatusEffect>[] _netMake =
    {
        () => new StunnedEffect(9999f),
        () => new SlowedEffect(9999f),
        () => new FearedEffect(9999f, Vector2.right),
        () => new HastedEffect(9999f),
        () => new CharmedEffect(9999f, Vector2.zero),
        () => new BlindedEffect(9999f, 1f),
        () => new CamouflageEffect(9999f, 1f, 4f),
        () => new InvisibleEffect(9999f),
        () => new CCImmunityEffect(9999f),
        () => new TrueFormEffect(9999f, 1f, new Color(1f, 0.4f, 0.2f, 0.5f)),
    };

    readonly Dictionary<int, StatusEffect> _netApplied = new();
    int _lastNetMask;

    const int BlindBit = 5; // indice de BlindedEffect en _netTypes (mantener en sync con el array)

    /// <summary>HOST: multiplicador de FOV del blind activo (1 = sin blind). Para replicar la magnitud.</summary>
    public float GetBlindMultiplier()
    {
        for (int i = 0; i < _active.Count; i++)
            if (_active[i] is BlindedEffect b) return b.FovMultiplier;
        return 1f;
    }

    /// <summary>HOST: mascara de bits de los tipos de efecto activos, para replicar.</summary>
    public int GetActiveTypeMask()
    {
        int mask = 0;
        for (int i = 0; i < _active.Count; i++)
        {
            var t = _active[i].GetType();
            for (int b = 0; b < _netTypes.Length; b++)
                if (_netTypes[b] == t) { mask |= (1 << b); break; }
        }
        return mask;
    }

    /// <summary>CLIENTE: reconstruye/retira efectos visuales segun la mascara replicada del host.
    /// Idempotente: solo actua en los bits que cambiaron. blindMult = magnitud real del blind (para que
    /// el jugador local cegado reduzca su FOV; el resto de magnitudes no afectan la vista del cliente).</summary>
    public void SyncFromMask(int mask, float blindMult = 1f)
    {
        if (mask == _lastNetMask) return;

        for (int b = 0; b < _netTypes.Length; b++)
        {
            int flag = 1 << b;
            bool want = (mask & flag) != 0;
            bool have = _netApplied.ContainsKey(b);

            if (want && !have)
            {
                var e = (b == BlindBit) ? new BlindedEffect(9999f, blindMult) : _netMake[b]();
                _active.Add(e);
                e.OnApply(_char);
                OnEffectApplied?.Invoke(e);
                EventBus.Publish(new StatusEffectAppliedEvent { Character = _char, Effect = e });
                _netApplied[b] = e;
            }
            else if (!want && have)
            {
                var e = _netApplied[b];
                _netApplied.Remove(b);
                _active.Remove(e);
                e.OnRemove(_char);
                OnEffectRemoved?.Invoke(e);
                EventBus.Publish(new StatusEffectRemovedEvent { Character = _char, Effect = e });
            }
        }

        RefreshMotor();
        _lastNetMask = mask;
    }
}
