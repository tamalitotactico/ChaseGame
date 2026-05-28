# Chase Game — Architecture Reference

**Motor:** Unity 6000.0.60f1 | **Render:** URP 17.0.4 (2D Renderer) | **Input:** Input System 1.14.2
**Pathfinding:** A* Pathfinding Project (`ASTAR_EXISTS` define symbol)
**Ultima revision:** 2026-05-17 (Phase 2 — reescritura completa)

---

## Tabla de contenidos

1. [Jerarquia de herencia](#1-jerarquia-de-herencia)
2. [Diagrama de dependencias](#2-diagrama-de-dependencias)
3. [Modulo: Characters & Components](#3-modulo-characters--components)
4. [Modulo: Brains & Input](#4-modulo-brains--input)
5. [Modulo: Abilities](#5-modulo-abilities)
6. [Modulo: Combat & Damage](#6-modulo-combat--damage)
7. [Modulo: Status Effects](#7-modulo-status-effects)
8. [Modulo: AI (Bots)](#8-modulo-ai-bots)
9. [Modulo: Match Flow](#9-modulo-match-flow)
10. [Modulo: Fog of War](#10-modulo-fog-of-war)
11. [Modulo: Eventos (EventBus)](#11-modulo-eventos-eventbus)
12. [Modulo: UI](#12-modulo-ui)
13. [Modulo: Infrastructure](#13-modulo-infrastructure)
14. [Modulo: ScriptableObjects](#14-modulo-scriptableobjects)
15. [Flujo de una partida](#15-flujo-de-una-partida)
16. [Guias de extension](#16-guias-de-extension)
17. [Problemas conocidos / quirks](#17-problemas-conocidos--quirks)

---

## 1. Jerarquia de herencia

```
MonoBehaviour
├── Character (abstract)
│   ├── HunterCharacter         Team = Hunter, hasBasicAttack = true
│   └── PreyCharacter           Team = Prey, no basic attack
├── CharacterMotor              Movimiento via Rigidbody2D
├── CharacterHealth             HP + invulnerability
├── CharacterVisuals            Flash daño, tints, status overlays
├── AbilityController           Lifecycle de habilidades
├── CombatController            Ataque basico (solo Hunter)
├── RevivableComponent          Downed/Revive (solo Prey)
├── StatusEffectController      Efectos de estado activos
├── PlayerBrain  ─┐
├── BotBrain     ─┴── implementan IBrain
└── BotLocomotion               Wrapper de A* AIPath/Seeker

Ability (clase pura, NO MonoBehaviour)
├── DashAbility                 instant: impulse en direccion
├── ProjectileAbility           directional aim: proyectil generico
├── FearProjectileAbility       instant: proyectil homing con Fear
├── TeleportSmashAbility        instant: teleport + AoE Fear/Slow + self-Haste
└── RemnantAbility              instant: spawnea decoy trampa

AbilityData : ScriptableObject (abstract)
├── DashAbilityData
├── ProjectileAbilityData
├── FearProjectileAbilityData
├── TeleportSmashAbilityData
└── RemnantAbilityData

Aimer (abstract)
├── NoAimer                     instant cast (no aim phase)
├── DirectionalAimer            hold-aim-release con direccion
└── AreaAimer                   hold-aim-release con punto en mundo

StatusEffect (abstract)
├── StunnedEffect               BlocksMovement + BlocksActions
├── SlowedEffect                SpeedModifier < 1
├── FearedEffect                ForceMoveInput + BlocksActions
└── HastedEffect                SpeedModifier > 1

IBotState
├── BotPatrolState              Hunter: waypoints aleatorios
├── BotChaseState               Hunter: persigue con lead + dispara abilities
├── BotAttackState              Hunter: melee con lock
├── BotSearchState              Hunter: ultima posicion conocida
├── BotWanderState              Prey: exploracion pasiva
├── BotFleeState                Prey: huye con escape-fan raycast
└── BotReviveState              Prey: navega hacia aliado downed

ICharacterState
├── IdleState
├── MoveState
├── AttackState
├── InjuredState
├── DownedState
└── DeadState

IMatchState
├── LobbyState
├── StartingState
├── PlayingState
└── EndingState
```

---

## 2. Diagrama de dependencias

```
                  ┌─────────────────┐
                  │   GameManager   │
                  │  (scene anchor) │
                  └────────┬────────┘
                           │ spawns + manages
                  ┌────────▼────────┐
                  │   MatchState    │ ── publishes ──► EventBus ◄── subscribed by UI/Camera/FoW
                  │     Machine     │
                  └────────┬────────┘
                           │
                  ┌────────▼────────┐
                  │    Character    │── reads ──► CharacterData (SO)
                  └────────┬────────┘
                           │ owns
        ┌─────────┬────────┼────────┬─────────┬──────────┐
        ▼         ▼        ▼        ▼         ▼          ▼
      Motor    Health  Combat  Abilities  Status    Revivable
                                    │
                                    │ reads from
                                    ▼
                               IBrain ──► BrainIntent
                                    │
                       ┌────────────┴────────────┐
                       │                         │
                  PlayerBrain                BotBrain ──► BotStateMachine
                       │                         │
                  Keyboard +              ┌──────┴──────┐
                  HybridInput             ▼             ▼
                                    BotLocomotion  BotTuningData (SO) ──► AbilityUseRule[]
                                    (A* wrapper)
```

---

## 3. Modulo: Characters & Components

### `Character` (abstract MonoBehaviour)
**Path:** `Assets/_Game/Scripts/Gameplay/Characters/Character.cs`

Orquesta los subsistemas. En `Awake()` hace `GetComponent` de todos los componentes
sibling y los expone como propiedades. En `Initialize()` lee `CharacterData` y configura
`Motor`, `Health`, `Abilities`, `Combat`.

**Subclases:** `HunterCharacter` y `PreyCharacter`. Solo override `Team`.
El resto del comportamiento se diferencia por componentes presentes en el prefab
(Hunter tiene `CombatController`, Prey tiene `RevivableComponent`).

**Key API:**
- `Team` (abstract) — `CharacterTeam.Hunter` o `Prey`
- `Data`, `Motor`, `Health`, `Abilities`, `Combat`, `Revivable`, `StatusEffects`, `States`
- `Authority` (`IAuthorityContext`)
- `FacingDirection` (Vector2)
- `Initialize(CharacterData)`, `SetBrain(IBrain)`, `SetAuthority(IAuthorityContext)`
- `IsAlive`, `IsDowned`, `IsTargetable`

### `CharacterMotor`
`SetMoveInput(Vector2)`, `ApplyImpulse(Vector2 vel, float duration)`, `Stop()`.
Lee `SpeedMultiplier` del `StatusEffectController` cada `FixedUpdate`.

### `CharacterHealth`
`MaxHealth`, `CurrentHealth`, `TryDamage(DamageInfo)`, `Heal(int)`.
Eventos: `OnDamaged(int current, int max)`, `OnDied()`, `OnHealed(int, int)`.

### `RevivableComponent` (solo Prey)
Bleed-out timer + revive progress por proximidad. Eventos: `OnRevived`, `OnBleedOut`.
Configurado por `MatchSettings.reviveAllowed`, `bleedOutDuration`, `maxRevivesPerMatch`.

### `StatusEffectController`
Lista de `StatusEffect`. Métodos: `Apply(StatusEffect)`, `Remove<T>()`, `Has<T>()`,
`Tick(dt)`. Propiedades agregadas: `CanMove`, `CanAct`, `AggregatedSpeedModifier`,
`GetForceMoveInput()`.

### `CharacterVisuals`
Sibling de `Character`. Suscribe a `OnDamaged`, eventos de estado downed, `OnEffectApplied`.
Aplica flash blanco al daño, tint rojo si herido, tint azul pulsante si downed,
gris si muerto, y overlays de color/outline en el shader `Game/CharacterEffect`.

### `CharacterStateMachine` + 6 estados
FSM genérica gobernada desde `Character.Update()`. Estados `IdleState`, `MoveState`,
`AttackState`, `InjuredState`, `DownedState`, `DeadState` (todos `ICharacterState`).
Sirven para gates de animación/audio futuros y como filtro de input.

---

## 4. Modulo: Brains & Input

### `IBrain`
```csharp
public interface IBrain { BrainIntent CaptureIntent(); }
```

### `BrainIntent` (struct)
Snapshot por frame de la intención del personaje:
```csharp
public Vector2 MoveInput;
public Vector2 AimInput;
public bool AttackPressed;
public AbilityInputState Slot0, Slot1, Slot2; // None | Pressed | Held | Released
public BrainIntent WithActionsCleared(); // anula attack + slots
```

### `PlayerBrain`
Lee Keyboard (`Q`/`E`/`R` para slots, `Space` para attack) y movimiento desde
`HybridInputManager` (WASD + joystick virtual). Genera transiciones None→Pressed→Held→Released
detectando el flanco de tecla. `AimInput` por defecto = `MoveInput` (extensible para mouse).

### `BotBrain`
Ejecuta `BotStateMachine`. Lee `BotTuningData` (SO) para todos los parámetros.
Provee `FindNearestVisibleEnemy()` (scoring por downed/herido/aislado/distancia)
y `FindNearestDownedAlly()`.

---

## 5. Modulo: Abilities

### Arquitectura data-driven

```
AbilityData (SO) ──CreateRuntime()──► Ability (clase)
                                          │
                                          ▼
                                  BeginActivation(ctx)
                                  retorna Aimer? o null
                                          │
                                ┌─────────┴─────────┐
                                ▼                   ▼
                              null               Aimer
                            (instant)         (hold-aim-release)
                                │                   │
                                ▼                   ▼
                          Execute(ctx, default)  Execute(ctx, aim.GetResult())
                                          │
                                          ▼
                                  StartCooldown()
                                  OnAbilityExecuted event
```

### `AbilityData` (SO abstract)
**Path:** `Assets/_Game/Scripts/Data/AbilityData.cs`

Base de todos los SOs de habilidad. Campos comunes: `id`, `displayName`, `icon`,
`description`, `cooldown`, `duration`. Método abstract:
```csharp
public abstract Ability CreateRuntime();
```

### `Ability` (clase pura)
Recibe el SO en constructor. Ciclo de vida:
- `BeginActivation(ctx)` — retorna `Aimer` (con phase) o `null` (instant).
- `Execute(ctx, aim)` — efecto de la habilidad.
- `Tick(dt)` — decrementa cooldown.
- `IsReady` — `CooldownRemaining <= 0`.

### `AbilityController` (componente)
**Path:** `Assets/_Game/Scripts/Gameplay/Abilities/AbilityController.cs`

Drive principal desde `Character.Update()`:
```csharp
public void Tick(in BrainIntent intent, float dt)
{
    // 1. Cooldowns
    // 2. Si _activeAimer == null: detectar Pressed → BeginActivation → Aimer? → cast o aim
    // 3. Si _activeAimer != null: DriveAim() hasta Released
}
```
Eventos: `OnCooldownChanged(slot, normalized)`, `OnAbilityExecuted(slot)`.

### `Aimer` (abstract) + 3 concretos
- **NoAimer**: instant cast, no usado directamente (se retorna `null` desde `BeginActivation`).
- **DirectionalAimer**: lee `intent.AimInput` o `intent.MoveInput`; almacena dirección
  hasta `Released`. Output: `AimResult.Direction`.
- **AreaAimer**: lee `intent.AimInput` y clamp a `maxRange`. Output: `AimResult.TargetPosition`.

### Habilidades concretas

| Habilidad | Aimer | Acción |
|---|---|---|
| `DashAbility` | NoAimer (instant) | `Motor.ApplyImpulse(dir * force, duration)` |
| `ProjectileAbility` | DirectionalAimer | Spawn proyectil con `ISpawnService.Spawn()` |
| `FearProjectileAbility` | NoAimer (auto-target) | Busca Prey más cercano, spawnea homing |
| `TeleportSmashAbility` | NoAimer (instant) | Raycast wall-aware → teleport → AoE Fear+Slow → self-Haste |
| `RemnantAbility` | NoAimer (instant) | Spawnea `RemnantDecoy` que activa AoE al detectar Prey |

### `AbilityContext` (struct, passed a Aimer y Execute)
```csharp
Character Owner;
Vector2 OwnerPosition;
Vector2 MoveDirection;
Vector2 FacingDirection;
IAuthorityContext Authority;
ISpawnService SpawnService;
```

### `AimResult` (struct, output del Aimer)
```csharp
Vector2 Direction;
Vector2 TargetPosition;
Transform TargetEntity;
bool HasDirection, HasPosition, HasTarget;
```

---

## 6. Modulo: Combat & Damage

### `IDamageable`
```csharp
bool IsAlive { get; }
bool IsTargetable { get; }
void TakeDamage(in DamageInfo info);
```
Implementado por `Character`.

### `DamageInfo` (struct)
```csharp
Character Source;
int Amount;
Vector2 Origin;
Vector2 Direction;
float KnockbackForce;
bool IgnoreInvulnerability;
DamageInfo.Simple(source, amount); // helper
```

### `CombatController` (solo Hunter)
Ataque básico melee circular. Setup via `CharacterData.hasBasicAttack`,
`attackRange`, `attackCooldown`, `attackDamage`. Cooldown propio independiente del
sistema de habilidades. `Tick(intent, dt)` ejecuta si `intent.AttackPressed` + cooldown ready.

---

## 7. Modulo: Status Effects

### `StatusEffect` (abstract)
**Path:** `Assets/_Game/Scripts/Gameplay/Characters/StatusEffect.cs`

```csharp
public float Duration { get; protected set; }
public float Remaining { get; protected set; }
public bool IsExpired => Remaining <= 0f;

public virtual bool BlocksMovement => false;
public virtual bool BlocksActions => false;
public virtual float SpeedModifier => 1f;
public virtual Vector2? ForceMoveInput => null;

public abstract void OnApply(Character target);
public abstract void OnRemove(Character target);
public virtual void Tick(float dt);
```

### `StatusEffectController`
Lista de `StatusEffect` activos. Métodos:
- `Apply(effect)` — si ya existe del mismo tipo, lo reemplaza (refresh, no stack).
- `Remove<T>()`, `Has<T>()`, `Get<T>()`.
- `Tick(dt)` — decrementa duraciones y remueve expirados.

Agregadores:
- `CanMove` — false si cualquier efecto activo tiene `BlocksMovement = true`.
- `CanAct` — false si cualquier efecto tiene `BlocksActions = true`.
- `AggregatedSpeedModifier` — producto de todos los `SpeedModifier`.
- `GetForceMoveInput()` — primer efecto que provea un override de input gana.

Eventos: `OnEffectApplied(StatusEffect)`, `OnEffectRemoved(StatusEffect)`.

### 4 efectos concretos

| Efecto | Constructor | BlocksMov | BlocksAct | SpeedMod | ForceMove |
|---|---|---|---|---|---|
| `StunnedEffect(duration)` | duration | ✓ | ✓ | 1 | null |
| `SlowedEffect(duration, mult)` | duration, mult | ✗ | ✗ | mult (clamp [0,1]) | null |
| `FearedEffect(duration, fleeDir)` | duration, dir | ✗ | ✓ | 1 | fleeDir.normalized |
| `HastedEffect(duration, mult)` | duration, mult | ✗ | ✗ | mult (clamp ≥1) | null |

`Character.Update()` consume el `StatusEffectController`:
```csharp
if (StatusEffects != null && !StatusEffects.CanAct)
    intent = intent.WithActionsCleared();
Vector2 moveInput = intent.MoveInput;
if (StatusEffects != null) {
    var forced = StatusEffects.GetForceMoveInput();
    if (forced.HasValue) moveInput = forced.Value;
}
Motor.SetMoveInput(moveInput);
```

---

## 8. Modulo: AI (Bots)

### `BotBrain` + `BotLocomotion` + `BotStateMachine`

- `BotLocomotion`: wrapper de A* `AIPath`/`Seeker`. Métodos `SetDestination(Vector3)`,
  `GetSteeringDirection() → Vector2`, `HasLineOfSight(from, to)`, `CheckStuck(dt)`.
- `BotStateMachine`: FSM. `ChangeState(IBotState)`, `Tick(dt) → BrainIntent`.
- `BotBrain.Start()` elige estado inicial según `self.Team`:
  - Hunter → `BotPatrolState`
  - Prey → `BotWanderState`

### Perception (en `BotBrain`)
```csharp
Character FindNearestVisibleEnemy();   // scoring: downed +1000, herido +100, aislado +50, -distSq
Character FindNearestDownedAlly();     // sin LOS, solo distancia (señal "psíquica")
bool CanSee(Transform);                // visionRange + LOS
```

### 7 estados de bot

**Hunter:**
- `BotPatrolState`: waypoints aleatorios dentro de `patrolRadius`; transición a `Chase`
  si ve un Prey.
- `BotChaseState`:
  - **Interception**: predice posición del target con `target.Motor.Velocity * leadTime`
    (cap `huntLeadTimeMax`).
  - **Habilidades**: itera `Tuning.abilityRules` y dispara la primera regla cumplida
    (cooldown por regla + global cooldown).
  - Transiciona a `Attack` si entra a `attackRange`, a `Search` si pierde LOS por
    `chaseLosTimeout`.
- `BotAttackState`: ejecuta `AttackPressed` durante `attackLockDuration`.
- `BotSearchState`: navega a `LastKnownTargetPosition` y escanea durante `searchDuration`.

**Prey:**
- `BotWanderState`: waypoints aleatorios dentro de `wanderRadius`; transición a `Flee`
  si ve Hunter o a `Revive` si detecta aliado downed.
- `BotFleeState`: **escape-fan**: evalúa N direcciones candidatas con raycast contra
  muros, elige `score = clearance * (0.6 + 0.4 * dotConAwayDir)`. Si `CheckStuck`
  retorna true, fuerza destino perpendicular durante `fleeLateralBurstDuration` para
  despegarse de paredes. Soluciona el bug clásico de "atascado en esquina".
- `BotReviveState`: navega al aliado downed; revive por proximidad.

### `AbilityUseRule` + `TargetCondition`
**Path:** `Assets/_Game/Scripts/Data/AbilityUseRule.cs`

```csharp
[Serializable]
public class AbilityUseRule {
    public int slot;                    // 0/1/2
    public float minDistance, maxDistance;
    public float internalCooldown;
    public bool requiresLineOfSight;
    public TargetCondition condition;
    public string note;
}

public enum TargetCondition {
    None,
    TargetMoving,             // velocity > umbral
    TargetWounded,            // HP < max
    TargetIsolated,           // sin aliados cerca
    TargetFleeingStraight     // velocity casi paralela a (target - hunter)
}
```

`BotChaseState` itera reglas en orden, primer match dispara. Anti-spam vía
`globalAbilityCooldown` además del cooldown por regla.

---

## 9. Modulo: Match Flow

### `GameManager` (singleton de escena)
**Path:** `Assets/_Game/Scripts/Match/GameManager.cs`

- Registra servicios en `ServiceLocator` al iniciar (`LocalAuthority`, `LocalSpawnService`).
- Spawnea entidades según `MatchSettings`:
  - `Hunters`/`Preys` listas mantenidas con `[Hunter|Prey]Spawned/Despawned` callbacks.
  - Publica `CharacterSpawnedEvent` por cada uno (suscriben `CameraFollow`, `FogOfWarManager`, UI).
- Ejecuta `MatchStateMachine.Tick()` cada frame.
- API útil: `Instance`, `Settings`, `Hunters`, `Preys`, `TimeRemaining`, `PlayerTeam`,
  `AliveHuntersCount`, `AlivePreysCount`, `ActivePreysCount`.

### `MatchStateMachine` + 4 estados
- **LobbyState**: espera (no usado si `autoStart=true`).
- **StartingState**: spawn de entidades + countdown (publica `CountdownTickEvent`).
- **PlayingState**: timer + chequeo de victoria (`ActivePreysCount == 0` o `TimeRemaining <= 0`).
- **EndingState**: publica `MatchEndedEvent` con equipo ganador y razón.

---

## 10. Modulo: Fog of War

### `FogOfWarManager` (multi-source, estilo Dota 2)
**Path:** `Assets/_Game/Scripts/FogOfWar/FogOfWarManager.cs`

Suscribe a `CharacterSpawnedEvent` y auto-registra el `VisionSource` del jugador local
(detectado por `PlayerBrain`). Soporta múltiples fuentes (uno por aliado en multiplayer).

Por cada fuente:
1. Crea/asigna un `SpriteMask` (custom range 0-19 en sorting layer Default).
2. Construye textura 256×256 con polígono de visibilidad: 72 raycasts en 360° +
   corner rays para cerrar sombras en esquinas.
3. Rasteriza triángulos en CPU (edge-function) y aplica al sprite del mask.
4. Pasa la textura como `_VisionTex0` al material `FogOverlay.mat` para el fade radial.

### `VisionSource`
**Path:** `Assets/_Game/Scripts/FogOfWar/VisionSource.cs`

Componente marker en el personaje. Solo expone `visionRadius` (Hunter: 8, Prey: 5).

### Shader `Game/FogOverlay`
**Path:** `Assets/_Game/Shaders/FogOverlay.shader`

Render del overlay de niebla. Combina:
1. Sample de `_VisionTex0` (polígono con oclusión de muros).
2. `smoothstep(radius - fadeWidth, radius, distanciaAlPlayer)` para fade radial suave.

```hlsl
float visibility = inPolygon * (1 - smoothstep(radius - fadeWidth, radius, dist));
col.a = _FogColor.a * (1 - visibility);
```

### Shader `Game/CharacterEffect`
**Path:** `Assets/_Game/Shaders/CharacterEffect.shader`

Shader URP Unlit para sprites de personajes con:
- `_EffectTint`: overlay de color (stun amarillo, slow azul, fear morado).
- `_OutlineEnabled` + `_OutlineColor`: contorno (target marcado).
- **Bloque `Stencil`**: `Ref [_StencilRef] Comp [_StencilComp]` — Unity setea estos valores
  automáticamente según `SpriteRenderer.maskInteraction`. **CRÍTICO**: sin este bloque,
  el shader no respeta `SpriteMask` → personajes siempre visibles ignorando FoW.
  Por eso `Sprite-Lit-Default` no funciona (no tiene el bloque).

---

## 11. Modulo: Eventos (EventBus)

**Path:** `Assets/_Game/Scripts/Events/GameEvents.cs`

Todos los eventos son `struct` (zero GC). Suscripción genérica:
```csharp
EventBus.Subscribe<CharacterSpawnedEvent>(handler);
EventBus.Publish(new CharacterSpawnedEvent { Character = c });
EventBus.Unsubscribe<CharacterSpawnedEvent>(handler);
```

### Catálogo completo

| Evento | Payload | Publisher | Subscribers principales |
|---|---|---|---|
| `CharacterSpawnedEvent` | Character | GameManager | CameraFollow, FogOfWarManager, AbilityHUD |
| `CharacterDamagedEvent` | Character, CurrentHealth, MaxHealth | CharacterHealth | HUDController, CharacterVisuals |
| `CharacterDiedEvent` | Character | CharacterHealth | GameManager, HUD |
| `CharacterDownedEvent` | Character | RevivableComponent | DownedIndicatorPanel, CharacterVisuals |
| `CharacterRevivedEvent` | Character, Reviver | RevivableComponent | UI, audio |
| `ReviveProgressChangedEvent` | Character, BleedOutRemaining, Progress | RevivableComponent | FloatingHealthBar |
| `AbilityActivatedEvent` | Character, Slot | AbilityController | UI feedback |
| `InteractionStartedEvent` | Character, Target | (placeholder) | future |
| `InteractionCompletedEvent` | Character, Target | (placeholder) | future |
| `MatchStateChangedEvent` | StateName | MatchStateMachine | UI |
| `MatchStartedEvent` | — | StartingState→PlayingState | HUD, audio |
| `MatchEndedEvent` | WinningTeam, Reason | EndingState | UI result panel |
| `CountdownTickEvent` | Remaining | StartingState | HUD countdown |
| `MatchTimerTickEvent` | Remaining | PlayingState | HUD timer |

---

## 12. Modulo: UI

| Componente | Responsabilidad | Eventos suscritos |
|---|---|---|
| `HUDController` | Corazones del jugador, timer, countdown, panel de resultado | CharacterDamaged, MatchTimerTick, CountdownTick, MatchEnded |
| `AbilityHUD` | Slots con cooldown radial; reacciona a clicks táctiles | CharacterSpawned (bind a player), AbilityController.OnCooldownChanged |
| `FloatingHealthBar` | World-space sobre cada personaje, estados normal/herido/downed/dead | CharacterDamaged, ReviveProgressChanged |
| `DownedIndicatorPanel` | Panel con estado de Preys + prompt de revive | CharacterDowned, CharacterRevived |
| `StatusIconDisplay` | Sprites de efectos activos sobre el personaje | OnEffectApplied/Removed |
| `CameraFollow` | Cámara sigue al `PlayerBrain` | CharacterSpawned |
| `DebugOverlayToggle` | Toggle de gizmos | — |

---

## 13. Modulo: Infrastructure

### `EventBus`
**Path:** `Assets/_Game/Scripts/Core/EventBus.cs`
Pub/sub genérico, `Subscribe<T>` / `Unsubscribe<T>` / `Publish<T>(in T)` / `Clear()`.
Usa `Dictionary<Type, Delegate>`. Llamar `Clear()` en transición de escena para evitar leaks.

### `ServiceLocator`
**Path:** `Assets/_Game/Scripts/Core/ServiceLocator.cs`
Registro estático: `Register<T>(impl)`, `Resolve<T>()`, `Unregister<T>()`.
Usado para `IAuthorityContext` y `ISpawnService`. Permite swap a Fusion/Netcode en Phase 3
sin tocar gameplay.

### `IAuthorityContext`
```csharp
bool IsLocal { get; }
bool IsAuthority { get; }
bool CanSimulate { get; }
```
Implementación `LocalAuthority`: todo = true (Phase 0/1/2).

### `ISpawnService`
```csharp
GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot);
GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent);
void Despawn(GameObject instance);
```
Implementación `LocalSpawnService`: `Object.Instantiate` + `Object.Destroy`.

---

## 14. Modulo: ScriptableObjects

| SO | Path | Función |
|---|---|---|
| `CharacterData` | `Data/CharacterData.cs` | Stats + abilities por personaje |
| `AbilityData` (abstract) | `Data/AbilityData.cs` | Base de SOs de habilidades |
| `DashAbilityData`, etc. | `Gameplay/Abilities/Concrete/*Data.cs` | Datos específicos por habilidad |
| `MatchSettings` | `Data/MatchSettings.cs` | Reglas de partida (duración, composición, revive) |
| `BotTuningData` | `Data/BotTuningData.cs` | ~40 parámetros de IA por rol |
| `AbilityUseRule` | `Data/AbilityUseRule.cs` | Regla de uso de habilidad para bots (clase serializable, no SO) |
| `DifficultySettings` | `Data/DifficultySettings.cs` | Escalado de dificultad |

### Assets creados

```
Assets/_Game/ScriptableObjects/
├── Abilities/
│   ├── DashAbility.asset
│   ├── ProjectileAbility.asset
│   └── Hunter1/
│       ├── Hunter_FearProjectile.asset
│       ├── Hunter_TeleportSmash.asset
│       └── Hunter_Remnant.asset
├── BotTuning/
│   ├── HunterBotTuning.asset
│   └── PreyBotTuning.asset
├── Characters/
│   ├── HunterData.asset
│   └── PreyData.asset
├── Match/
│   └── SandboxMatch.asset
└── Data/
    └── DifficultySettings.asset
```

---

## 15. Flujo de una partida

```
00_Bootstrap.unity  (opcional)
       │
       ▼
00_Sandbox.unity (escena principal)
       │
       │ Awake/Start
       ▼
┌─────────────────────────────────────┐
│  GameManager                        │
│  1. Registra ServiceLocator         │
│     - LocalAuthority                │
│     - LocalSpawnService             │
│  2. Crea MatchStateMachine          │
│  3. Inicia con LobbyState           │
└──────────┬──────────────────────────┘
           │
           │ autoStart=true
           ▼
   ┌───────────────┐
   │ StartingState │  countdown + spawn entities
   │               │   - SpawnOne(hunterPrefab) × N
   │               │   - SpawnOne(preyPrefab) × M
   │               │   - publish CharacterSpawnedEvent
   └───────┬───────┘
           │ countdown → 0
           ▼
   ┌───────────────┐
   │ PlayingState  │  cada frame:
   │               │   - tick MatchStateMachine
   │               │   - decrement TimeRemaining
   │               │   - check ActivePreysCount == 0 → Hunters win
   │               │   - check TimeRemaining <= 0 → Preys win
   └───────┬───────┘
           │ win condition
           ▼
   ┌───────────────┐
   │ EndingState   │  publish MatchEndedEvent
   │               │  UI muestra resultado
   └───────────────┘
```

Por personaje (cada frame):
```
Character.Update()
  ├── intent = brain.CaptureIntent()
  ├── if !StatusEffects.CanAct: intent.WithActionsCleared()
  ├── moveInput = StatusEffects.GetForceMoveInput() ?? intent.MoveInput
  ├── Motor.SetMoveInput(moveInput)
  ├── Abilities.Tick(intent, dt)
  ├── Combat.Tick(intent, dt)   // solo Hunter
  ├── StatusEffects.Tick(dt)
  └── States.Tick(dt)
```

---

## 16. Guias de extension

### 16.1 Crear un nuevo Hunter desde cero (ej. "Acechador")

**Step 1 — Script de subclase**

`Assets/_Game/Scripts/Gameplay/Characters/AcechadorCharacter.cs`:
```csharp
public class AcechadorCharacter : Character
{
    public override CharacterTeam Team => CharacterTeam.Hunter;
}
```

**Step 2 — CharacterData SO**

Crear `Assets/_Game/ScriptableObjects/Characters/AcechadorData.asset`:
Menú: `Create > ChaseGame/Data/Character Data`.
Campos:
- `id = "acechador"`, `displayName = "Acechador"`
- `team = Hunter`
- `baseSpeed = 4.8`, `maxHealth = 3`
- `hasBasicAttack = true`, `attackRange = 1.5`, `attackCooldown = 0.8`, `attackDamage = 1`
- `abilities[]` = [SOs de las 3 habilidades creadas en 16.3]

**Step 3 — Prefab**

Duplicar `Hunter.prefab` → renombrar `Acechador.prefab`. Cambios:
1. Cambiar componente `HunterCharacter` → `AcechadorCharacter` (drag drop script).
2. Asignar `AcechadorData.asset` al campo `data` del `Character`.
3. Cambiar sprite del `SpriteRenderer` al de Acechador.
4. **Material**: dejar `CharacterEffect.mat` para que respete FoW.
5. **MaskInteraction**: `VisibleInsideMask` (ya viene del Hunter.prefab).

Para versión bot, duplicar a `Acechador Bot.prefab`. Agregar:
- `Seeker`, `AIPath` (de A* Pathfinding)
- `BotLocomotion`
- `BotBrain` con `BotTuningData` asignado (ver 16.5)

**Step 4 — Registrar en `GameManager`**

En la escena (`00_Sandbox.unity`):
- `GameManager.hunterPrefab` → asignar `Acechador.prefab` (o `Acechador Bot.prefab`).

Si quieres soportar múltiples Hunters, modificar `GameManager` para usar un pool de prefabs.

---

### 16.2 Crear un nuevo Prey desde cero

Idéntico a 16.1 cambiando:
- Subclase: `MiPreyCharacter : Character` con `Team => CharacterTeam.Prey`.
- `hasBasicAttack = false` en el SO.
- En el prefab: `RevivableComponent` en vez de `CombatController`.
- Registrar en `GameManager.preyPrefab` (no `hunterPrefab`).

---

### 16.3 Crear una habilidad nueva

Ejemplo: **HuntMarkAbility** — AoE que aplica el efecto `MarkedEffect` (debuff de daño).

**Step 1 — Data SO**

`Assets/_Game/Scripts/Gameplay/Abilities/Concrete/HuntMarkAbilityData.cs`:
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "HuntMarkAbility", menuName = "ChaseGame/Abilities/Hunt Mark")]
public class HuntMarkAbilityData : AbilityData
{
    public float markRadius = 4f;
    public float markDuration = 3f;
    public float damageMultiplier = 1.25f;
    public LayerMask preyLayer;

    public override Ability CreateRuntime() => new HuntMarkAbility(this);
}
```

**Step 2 — Ability runtime**

`Assets/_Game/Scripts/Gameplay/Abilities/Concrete/HuntMarkAbility.cs`:
```csharp
using UnityEngine;

public class HuntMarkAbility : Ability
{
    readonly HuntMarkAbilityData _d;
    static readonly Collider2D[] _hitBuffer = new Collider2D[16];

    public HuntMarkAbility(HuntMarkAbilityData d) : base(d) { _d = d; }

    public override Aimer BeginActivation(in AbilityContext ctx) => null; // instant

    public override void Execute(in AbilityContext ctx, in AimResult _)
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            ctx.OwnerPosition, _d.markRadius, _hitBuffer, _d.preyLayer);
        for (int i = 0; i < count; i++)
        {
            var c = _hitBuffer[i].GetComponentInParent<Character>();
            if (c == null || c == ctx.Owner || c.Team == ctx.Owner.Team) continue;
            if (!c.IsAlive || c.StatusEffects == null) continue;
            c.StatusEffects.Apply(new MarkedEffect(_d.markDuration, _d.damageMultiplier));
        }
    }
}
```

**Step 3 — Crear el asset SO**

Menú: `Create > ChaseGame/Abilities/Hunt Mark`. Configurar valores en Inspector
(radio, duración, multiplier, layer Prey).

**Step 4 — Asignar al personaje**

En `AcechadorData.asset` (o el `CharacterData` del personaje), agregar el SO al
array `abilities[]` en el slot deseado (0/1/2).

**Step 5 — Reglas de IA (si es para un bot)**

Ver guía 16.5.

**Notas importantes:**
- Para spawnear prefabs (como proyectiles, decoys), usar `ctx.SpawnService.Spawn()` —
  NUNCA `Object.Instantiate` directo. La indirección permite swap a networking en Phase 3.
- Si la habilidad requiere aiming, retornar un `Aimer` en `BeginActivation` en vez de `null`.

---

### 16.4 Crear un nuevo status effect

Ejemplo: **MarkedEffect** (usado por HuntMark arriba).

`Assets/_Game/Scripts/Gameplay/Characters/Effects/MarkedEffect.cs`:
```csharp
using UnityEngine;

public class MarkedEffect : StatusEffect
{
    readonly float _damageMultiplier;
    public float DamageMultiplier => _damageMultiplier;

    public MarkedEffect(float duration, float damageMultiplier)
    {
        Duration = Remaining = duration;
        _damageMultiplier = Mathf.Max(1f, damageMultiplier);
    }

    public override void OnApply(Character target)  { }
    public override void OnRemove(Character target) { }
}
```

**Para consumir el modificador:** modificar `CharacterHealth.TryDamage()` para leer
`StatusEffects.Get<MarkedEffect>()?.DamageMultiplier ?? 1f` y multiplicar el daño entrante.

**Para feedback visual:**
1. Editar `CharacterVisuals.HandleEffectApplied/Removed`: agregar un `case MarkedEffect _:`
   que setee `_activeEffectTint = markedTint`.
2. Agregar un `Color markedTint` `[SerializeField]` en `CharacterVisuals`.
3. (Icono) En `StatusIconDisplay`, agregar el sprite del icono asociado al efecto.

---

### 16.5 Configurar comportamiento de IA para una nueva habilidad

Editar el `BotTuningData.asset` del personaje (ej. `AcechadorBotTuning.asset`).
En el campo `abilityRules`, agregar elementos:

```yaml
abilityRules:
  - slot: 0                          # HuntMark en slot 0
    minDistance: 0
    maxDistance: 4                   # AoE de 4u, dispara cerca
    internalCooldown: 6              # cada 6s
    requiresLineOfSight: false       # AoE no necesita LOS
    condition: TargetMoving          # solo si el target se está moviendo
    note: "Marca al Prey cuando está cerca y huyendo"

  - slot: 1
    minDistance: 5
    maxDistance: 10
    internalCooldown: 5
    requiresLineOfSight: true
    condition: None
    note: "Otro skill a distancia media"
```

**Cómo funciona el evaluador (`BotChaseState.TryFireAbility`):**
1. Por cada regla en orden:
   - Si `globalAbilityCooldown` activo → skip todas.
   - Si `Ability.IsReady == false` o cooldown interno activo → skip.
   - Si `distancia` fuera del rango → skip.
   - Si `requiresLineOfSight && !CanSee(target)` → skip.
   - Si `condition` no se cumple → skip.
2. Primera regla que pasa → `intent.SlotN = Pressed`, resetea cooldowns.

**Condiciones disponibles:**
- `None`: siempre cumple.
- `TargetMoving`: `target.Motor.Velocity > targetMovingVelocityThreshold`.
- `TargetWounded`: `target.Health.CurrentHealth < MaxHealth`.
- `TargetIsolated`: ningún aliado del target dentro de `targetIsolationRadius`.
- `TargetFleeingStraight`: velocidad del target ≈ paralela a `(targetPos - hunterPos)`,
  dot ≥ `targetFleeingStraightDot`.

---

## 17. Problemas conocidos / quirks

### `Sprite-Lit-Default` no respeta `SpriteMask` en URP 2D
**Síntoma:** Personaje con `SpriteRenderer.MaskInteraction = VisibleInsideMask` se ve
siempre, ignorando el FoW. Causa: el shader URP `Sprite-Lit-Default` no tiene el
bloque `Stencil` que activa el masking.

**Solución:** Usar `Game/CharacterEffect.shader` que sí tiene el bloque:
```hlsl
Stencil {
    Ref [_StencilRef]
    Comp [_StencilComp]
}
```
Unity setea estas propiedades automáticamente según `MaskInteraction`. El material
`CharacterEffect.mat` está listo para asignar a SpriteRenderers de personajes.

### A* Pathfinding requiere `ASTAR_EXISTS` define
Sin el symbol, `BotLocomotion` se compila pero no funciona. Agregar en
Project Settings → Player → Other Settings → Scripting Define Symbols.

### Tag legacy `SpawnPoint_Pursuer`
Conservado por compatibilidad con escenas existentes. Semánticamente representa el
Hunter spawn. `MapLoader.GetHunterSpawn()` lo busca por este tag.

### `Sprite.Create()` cada vez que cambia `visionRadius`
`FogOfWarManager` recrea el `Sprite` del SpriteMask si detecta cambio de `VisionRadius`
runtime (para mantener PPU correcto). No es un costo significativo a 60 FPS.

### Bots usan material default por error
Los prefabs `Hunter Bot.prefab` y `Prey Bot.prefab` usan `Sprite-Lit-Default` en vez de
`CharacterEffect.mat`. Resultado: los bots no se ocultan correctamente fuera de visión
del jugador. **Fix manual:** asignar `Assets/_Game/Materials/CharacterEffect.mat` al
SpriteRenderer de ambos prefabs.
