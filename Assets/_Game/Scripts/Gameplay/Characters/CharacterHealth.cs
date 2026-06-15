using System;
using UnityEngine;

/// <summary>
/// Vida + invulnerabilidad post-hit del Character. Phase 3: campos seran [Networked].
/// </summary>
public class CharacterHealth : MonoBehaviour
{
    [SerializeField] int   maxHealth = 2;
    [SerializeField] float invulnerabilityDuration = 1.5f;

    int   _current;
    float _invulnTimer;

    public int   MaxHealth        => maxHealth;
    public int   CurrentHealth    => _current;
    public bool  IsAlive          => _current > 0;
    public bool  IsInvulnerable   => _invulnTimer > 0f;

    public event Action<int, int>     OnDamaged; // (current, max)
    public event Action               OnDied;
    public event Action<int, int>     OnHealed;

    void Awake()
    {
        _current = maxHealth;
    }

    public void Setup(int max, float invuln)
    {
        maxHealth = max;
        invulnerabilityDuration = invuln;
        _current = maxHealth;
        _invulnTimer = 0f;
    }

    public bool TryDamage(in DamageInfo info)
    {
        if (!IsAlive) return false;
        if (IsInvulnerable && !info.IgnoreInvulnerability) return false;

        int dmg = info.Lethal ? _current : Mathf.Max(0, info.Amount);
        _current = Mathf.Max(0, _current - dmg);
        _invulnTimer = invulnerabilityDuration;
        OnDamaged?.Invoke(_current, maxHealth);

        if (_current == 0) OnDied?.Invoke();
        return true;
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;
        _current = Mathf.Min(maxHealth, _current + amount);
        OnHealed?.Invoke(_current, maxHealth);
    }

    public void ResetFull()
    {
        _current = maxHealth;
        _invulnTimer = 0f;
    }

    /// <summary>Setea HP directamente a un valor [0..max]. Usado por revive para HP parcial.</summary>
    public void SetCurrent(int value)
    {
        _current = Mathf.Clamp(value, 0, maxHealth);
        _invulnTimer = 0f;
    }

    void Update()
    {
        if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
    }
}
