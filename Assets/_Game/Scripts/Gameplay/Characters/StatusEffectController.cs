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
    /// </summary>
    public void Apply(StatusEffect effect)
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].GetType() == effect.GetType())
            {
                var old = _active[i];
                old.OnRemove(_char);
                _active.RemoveAt(i);
                OnEffectRemoved?.Invoke(old);
                break;
            }
        }

        _active.Add(effect);
        effect.OnApply(_char);
        OnEffectApplied?.Invoke(effect);
        RefreshMotor();
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
}
