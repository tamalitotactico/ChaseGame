using UnityEngine;

/// <summary>
/// Feedback visual in-game sobre el sprite del personaje. Sibling de Character.
///
/// Maneja:
///  - Flash blanco breve al recibir daño
///  - Tint rojo suave cuando HP cae bajo injuredThreshold
///  - Tint azul pulsante cuando downed
///  - Gris cuando muerto
///  - Overlay de color cuando hay status effects (stun=amarillo, slow=azul frio)
///  - Outline cuando hay efecto de marcado
///  - Llama FloatingHealthBar.UpdateHealth si el componente esta presente
///  - Llama FloatingHealthBar.ShowDowned/ShowReviving/HideDowned para downed state
///
/// Requiere que el SpriteRenderer use el material "Game/CharacterEffect" para que
/// los efectos de shader (overlay, outline) funcionen. Si usa el material default
/// de Unity, solo funciona el color tinting via SpriteRenderer.color.
/// </summary>
public class CharacterVisuals : MonoBehaviour
{
    [Header("Damage flash")]
    [SerializeField] float damageFlashDuration = 0.15f;
    [SerializeField] Color damageFlashColor    = Color.white;

    [Header("Injured tint")]
    [SerializeField] Color injuredTint       = new Color(1f, 0.55f, 0.55f);
    [Range(0f, 1f)]
    [SerializeField] float injuredThreshold  = 0.5f;

    [Header("Downed tint")]
    [SerializeField] Color downedTint        = new Color(0.55f, 0.6f, 1f);
    [SerializeField] float downedAlphaMin    = 0.45f;
    [SerializeField] float downedPulseSpeed  = 2.5f;

    [Header("Dead tint")]
    [SerializeField] Color deadTint          = new Color(0.4f, 0.4f, 0.4f, 0.7f);

    // Los tints de status effect ya no viven aqui: cada StatusEffect expone su
    // VisualTint + VisualPriority y StatusEffectController.GetTopVisualTint resuelve
    // cual gana. Agregar un efecto nuevo con tint ya no requiere tocar este archivo.

    [Header("Status effect outline")]
    [SerializeField] Color markedOutlineColor = new Color(1f, 0.3f, 0f, 1f);

    [Header("Status effect VFX prefabs (opcional, asignar desde Inspector)")]
    [SerializeField] GameObject stunFXPrefab;
    [SerializeField] GameObject slowFXPrefab;

    SpriteRenderer    _sprite;
    Material          _mat;          // instancia del material de este renderer
    FloatingHealthBar _healthBar;
    Character         _char;
    CharacterHealth   _health;
    StatusEffectController _statusEffects;

    Color _originalColor;
    Color _targetBaseColor;
    float _flashTimer;
    bool  _isDowned;
    bool  _isDead;

    // Estado de shader de efectos
    Color _activeEffectTint = new Color(0, 0, 0, 0);
    float _outlineEnabled   = 0f;
    Color _outlineColor     = Color.clear;
    bool  _shaderDirty      = false;

    // Handles de VFX adjuntos
    VFXHandle _stunVFX;
    VFXHandle _slowVFX;

    // IDs de propiedad del shader cacheados para no hashear strings cada frame
    static readonly int ID_EffectTint     = Shader.PropertyToID("_EffectTint");
    static readonly int ID_OutlineEnabled = Shader.PropertyToID("_OutlineEnabled");
    static readonly int ID_OutlineColor   = Shader.PropertyToID("_OutlineColor");

    void Awake()
    {
        _char          = GetComponent<Character>();
        _health        = GetComponent<CharacterHealth>();
        _statusEffects = GetComponent<StatusEffectController>();
        _sprite        = GetComponentInChildren<SpriteRenderer>();
        _healthBar     = GetComponentInChildren<FloatingHealthBar>();

        if (_sprite != null)
        {
            _originalColor   = _sprite.color;
            _targetBaseColor = _originalColor;
            // Auto-instancia el material para poder modificarlo sin afectar el material compartido
            _mat = _sprite.material;
        }
    }

    void OnEnable()
    {
        if (_health != null)
        {
            _health.OnDamaged += OnDamaged;
            _health.OnDied    += OnDied;
            _health.OnHealed  += OnHealed;
        }
        EventBus.Subscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Subscribe<CharacterRevivedEvent>(OnRevived);
        EventBus.Subscribe<ReviveProgressChangedEvent>(OnReviveProgress);

        if (_statusEffects != null)
        {
            _statusEffects.OnEffectApplied  += HandleEffectApplied;
            _statusEffects.OnEffectRemoved  += HandleEffectRemoved;
        }
    }

    void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDamaged -= OnDamaged;
            _health.OnDied    -= OnDied;
            _health.OnHealed  -= OnHealed;
        }
        EventBus.Unsubscribe<CharacterDownedEvent>(OnDowned);
        EventBus.Unsubscribe<CharacterRevivedEvent>(OnRevived);
        EventBus.Unsubscribe<ReviveProgressChangedEvent>(OnReviveProgress);

        if (_statusEffects != null)
        {
            _statusEffects.OnEffectApplied  -= HandleEffectApplied;
            _statusEffects.OnEffectRemoved  -= HandleEffectRemoved;
        }
    }

    // --- Status effect visuals ---

    void HandleEffectApplied(StatusEffect e)
    {
        // Tint: generico (no depende del tipo concreto). Cada efecto expone su VisualTint.
        RefreshEffectTint();

        // VFX prefabs: siguen mapeados por tipo aqui porque son refs asignadas en el
        // inspector (un StatusEffect es objeto C# puro y no puede serializar prefabs).
        switch (e)
        {
            case StunnedEffect _:
                _stunVFX?.Stop();
                if (stunFXPrefab != null)
                    _stunVFX = VFXSpawner.Attach(stunFXPrefab, _sprite != null ? _sprite.transform : transform);
                break;

            case SlowedEffect _:
                _slowVFX?.Stop();
                if (slowFXPrefab != null)
                    _slowVFX = VFXSpawner.Attach(slowFXPrefab, _sprite != null ? _sprite.transform : transform);
                break;
        }
    }

    void HandleEffectRemoved(StatusEffect e)
    {
        RefreshEffectTint();

        switch (e)
        {
            case StunnedEffect _:
                _stunVFX?.Stop();
                _stunVFX = null;
                break;

            case SlowedEffect _:
                _slowVFX?.Stop();
                _slowVFX = null;
                break;
        }
    }

    // Toma el tint del efecto activo de mayor prioridad (resuelto por el controller).
    void RefreshEffectTint()
    {
        _activeEffectTint = _statusEffects != null
            ? _statusEffects.GetTopVisualTint()
            : new Color(0, 0, 0, 0);
        _shaderDirty = true;
    }

    // --- Eventos de Character ---

    void OnDamaged(int current, int max)
    {
        _flashTimer = damageFlashDuration;
        RecalculateBaseColor(current, max);
        if (_healthBar != null) _healthBar.UpdateHealth(current, max);
    }

    void OnHealed(int current, int max)
    {
        RecalculateBaseColor(current, max);
        if (_healthBar != null) _healthBar.UpdateHealth(current, max);
    }

    void OnDied()
    {
        _isDead = true;
        _isDowned = false;
        if (_healthBar != null) _healthBar.ShowDead();
    }

    void OnDowned(CharacterDownedEvent e)
    {
        if (e.Character != _char) return;
        _isDowned = true;
        _isDead   = false;
        if (_healthBar != null) _healthBar.ShowDowned();
    }

    void OnRevived(CharacterRevivedEvent e)
    {
        if (e.Character != _char) return;
        _isDowned = false;
        _isDead   = false;
        if (_health != null) RecalculateBaseColor(_health.CurrentHealth, _health.MaxHealth);
        if (_healthBar != null)
        {
            _healthBar.HideDowned();
            if (_health != null) _healthBar.UpdateHealth(_health.CurrentHealth, _health.MaxHealth);
        }
    }

    void OnReviveProgress(ReviveProgressChangedEvent e)
    {
        if (e.Character != _char) return;
        if (_healthBar != null)
        {
            var rev = _char.Revivable;
            float bleedNorm = rev != null && rev.BleedOutDuration > 0f
                ? Mathf.Clamp01(e.BleedOutRemaining / rev.BleedOutDuration)
                : 1f;
            _healthBar.ShowReviving(bleedNorm, e.Progress);
        }
    }

    void RecalculateBaseColor(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 1f;
        _targetBaseColor = ratio <= injuredThreshold
            ? Color.Lerp(injuredTint, _originalColor, ratio / Mathf.Max(0.01f, injuredThreshold))
            : _originalColor;
    }

    // --- Update ---

    void Update()
    {
        if (_sprite == null) return;

        _flashTimer = Mathf.Max(0f, _flashTimer - Time.deltaTime);
        _sprite.color = ComputeColor();

        if (_shaderDirty) FlushShaderProperties();
    }

    Color ComputeColor()
    {
        if (_flashTimer > 0f)
            return Color.Lerp(_targetBaseColor, damageFlashColor, _flashTimer / damageFlashDuration);
        if (_isDead)
            return deadTint;
        if (_isDowned)
        {
            float pulse = (Mathf.Sin(Time.time * downedPulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(downedAlphaMin, 1f, pulse);
            return new Color(downedTint.r, downedTint.g, downedTint.b, alpha);
        }
        return _targetBaseColor;
    }

    void FlushShaderProperties()
    {
        if (_mat == null) { _shaderDirty = false; return; }
        _mat.SetColor(ID_EffectTint,     _activeEffectTint);
        _mat.SetFloat(ID_OutlineEnabled, _outlineEnabled);
        if (_outlineEnabled > 0.5f)
            _mat.SetColor(ID_OutlineColor, _outlineColor);
        _shaderDirty = false;
    }
}
