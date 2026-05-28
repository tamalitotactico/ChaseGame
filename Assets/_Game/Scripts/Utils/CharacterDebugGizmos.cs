using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Visualizadores de debug para un Character. Sibling, se cachean automaticamente
/// las refs en Awake (Combat, Health, Abilities, BotBrain, Revivable).
///
/// Suscribe eventos para mostrar flashes temporales:
///  - CombatController.OnAttackUsed → red flash del area de ataque
///  - CharacterHealth.OnDamaged → white X breve
///  - AbilityController.OnAbilityExecuted → orange ring breve
///
/// Persistentes (cada frame en OnDrawGizmos):
///  - Attack range (rojo, si Combat presente)
///  - Vision range (cyan, si BotBrain presente)
///  - Revive proximity radius (verde, si Revivable presente y downed)
///  - Cooldowns como barras verticales sobre el char
///  - Label con nombre del BotState actual
///
/// Toggles globales via DebugGizmoSettings; bool enableDebug por instancia
/// para silenciar uno solo.
/// </summary>
public class CharacterDebugGizmos : MonoBehaviour
{
    [Tooltip("Off oculta TODOS los gizmos de este personaje, independiente de los toggles globales.")]
    [SerializeField] bool enableDebug = true;

    [Header("Flash durations (s)")]
    [SerializeField] float attackFlashDuration  = 0.18f;
    [SerializeField] float damageFlashDuration  = 0.22f;
    [SerializeField] float abilityFlashDuration = 0.25f;

    Character          _char;
    CombatController   _combat;
    CharacterHealth    _health;
    AbilityController  _abilities;
    BotBrain           _bot;
    RevivableComponent _revivable;

    float _attackFlashT;
    float _damageFlashT;
    float _abilityFlashT;
    int   _lastAbilitySlot = -1;

    void Awake()
    {
        _char      = GetComponent<Character>();
        _combat    = GetComponent<CombatController>();
        _health    = GetComponent<CharacterHealth>();
        _abilities = GetComponent<AbilityController>();
        _bot       = GetComponent<BotBrain>();
        _revivable = GetComponent<RevivableComponent>();
    }

    void OnEnable()
    {
        if (_combat    != null) _combat.OnAttackUsed      += OnAttackUsed;
        if (_health    != null) _health.OnDamaged         += OnDamaged;
        if (_abilities != null) _abilities.OnAbilityExecuted += OnAbilityExecuted;
    }

    void OnDisable()
    {
        if (_combat    != null) _combat.OnAttackUsed      -= OnAttackUsed;
        if (_health    != null) _health.OnDamaged         -= OnDamaged;
        if (_abilities != null) _abilities.OnAbilityExecuted -= OnAbilityExecuted;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (_attackFlashT  > 0f) _attackFlashT  -= dt;
        if (_damageFlashT  > 0f) _damageFlashT  -= dt;
        if (_abilityFlashT > 0f) _abilityFlashT -= dt;
    }

    void OnAttackUsed(float _)        => _attackFlashT  = attackFlashDuration;
    void OnDamaged(int _, int __)     => _damageFlashT  = damageFlashDuration;
    void OnAbilityExecuted(int slot)
    {
        _abilityFlashT   = abilityFlashDuration;
        _lastAbilitySlot = slot;
    }

    void OnDrawGizmos()
    {
        if (!enableDebug || !DebugGizmoSettings.MasterEnabled) return;
        Vector3 pos = transform.position;

        // ----- Attack range (persistente)
        if (DebugGizmoSettings.ShowAttackRange && _combat != null)
        {
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.5f);
            Gizmos.DrawWireSphere(pos, _combat.AttackRange);
        }

        // ----- Vision range (persistente, solo bots)
        if (DebugGizmoSettings.ShowVisionRange && _bot != null && _bot.Tuning != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(pos, _bot.Tuning.visionRange);
        }

        // ----- Revive radius + bar (cuando downed)
        if (_revivable != null && _revivable.IsDowned)
        {
            if (DebugGizmoSettings.ShowReviveRadius)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
                Gizmos.DrawWireSphere(pos, _revivable.ReviveProximityRadius);
            }
            if (DebugGizmoSettings.ShowReviveBar)
            {
                DrawProgressBar(pos + Vector3.up * 1.2f, _revivable.ReviveProgress, new Color(0.2f, 1f, 0.4f, 1f));
            }
        }

        // ----- Attack flash
        if (DebugGizmoSettings.ShowAttackFlash && _attackFlashT > 0f && _combat != null)
        {
            float t = Mathf.Clamp01(_attackFlashT / attackFlashDuration);
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.5f * t);
            Gizmos.DrawSphere(pos, _combat.AttackRange);
        }

        // ----- Damage flash (X arriba del char)
        if (DebugGizmoSettings.ShowDamageFlash && _damageFlashT > 0f)
        {
            float t = Mathf.Clamp01(_damageFlashT / damageFlashDuration);
            Gizmos.color = new Color(1f, 1f, 1f, t);
            Vector3 up = pos + Vector3.up * 0.7f;
            Gizmos.DrawLine(up + new Vector3(-0.2f, -0.2f, 0), up + new Vector3(0.2f, 0.2f, 0));
            Gizmos.DrawLine(up + new Vector3(-0.2f, 0.2f, 0), up + new Vector3(0.2f, -0.2f, 0));
        }

        // ----- Ability flash (orange ring)
        if (_abilityFlashT > 0f)
        {
            float t = Mathf.Clamp01(_abilityFlashT / abilityFlashDuration);
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.7f * t);
            Gizmos.DrawWireSphere(pos, 0.6f + (1f - t) * 0.4f);
        }

        // ----- Cooldowns como barras verticales
        if (DebugGizmoSettings.ShowAbilityCooldowns && _abilities != null && _abilities.Abilities != null)
        {
            float baseX = -0.25f;
            for (int i = 0; i < _abilities.Abilities.Length && i < 3; i++)
            {
                var ab = _abilities.Abilities[i];
                if (ab == null) continue;
                float full = ab.Data != null && ab.Data.cooldown > 0f ? ab.Data.cooldown : 1f;
                float ratio = Mathf.Clamp01(1f - ab.CooldownRemaining / full);
                Vector3 origin = pos + new Vector3(baseX + i * 0.25f, -0.7f, 0);
                Gizmos.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
                Gizmos.DrawCube(origin + Vector3.up * 0.15f, new Vector3(0.18f, 0.3f, 0.01f));
                Gizmos.color = ratio >= 1f ? new Color(1f, 0.85f, 0.1f, 0.9f) : new Color(0.6f, 0.6f, 0.6f, 0.9f);
                Gizmos.DrawCube(origin + Vector3.up * (0.15f * ratio), new Vector3(0.16f, 0.28f * ratio, 0.01f));
            }
        }

        // ----- Label del estado del bot
#if UNITY_EDITOR
        if (DebugGizmoSettings.ShowBotStateLabel && _bot != null && _bot.FSM != null && _bot.FSM.Current != null)
        {
            Handles.color = Color.white;
            Handles.Label(pos + Vector3.up * 0.9f, _bot.FSM.Current.GetType().Name);
        }
#endif
    }

    static void DrawProgressBar(Vector3 center, float progress, Color color)
    {
        float w = 0.8f;
        float h = 0.12f;
        Gizmos.color = new Color(0, 0, 0, 0.6f);
        Gizmos.DrawCube(center, new Vector3(w, h, 0.01f));
        float p = Mathf.Clamp01(progress);
        Gizmos.color = color;
        Vector3 size = new Vector3(w * p, h * 0.85f, 0.02f);
        Vector3 origin = center - new Vector3(w * 0.5f, 0, 0) + new Vector3(size.x * 0.5f, 0, 0);
        Gizmos.DrawCube(origin, size);
    }
}
