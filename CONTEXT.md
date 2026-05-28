# Chase Game — CONTEXT.md

## Estado actual: Phase 2 avanzada

**Fecha:** 2026-05-17
**Motor:** Unity 6000.0.60f1
**Render:** Universal Render Pipeline (URP) 17.0.4 — 2D Renderer
**Input:** com.unity.inputsystem 1.14.2 (nuevo Input System)
**Pathfinding:** A* Pathfinding Project (define symbol `ASTAR_EXISTS`)
**MCP:** Unity MCP integrado (auditoría y herramientas desde Claude Code)

> [!NOTE]
> Este documento fue reescrito el 2026-05-17 para reflejar la arquitectura Phase 2.
> La versión anterior (Phase 1, fechada 2026-04-26) describía clases que ya no existen
> (`RoleBase`, `PursuerBase`, `SurvivorBase`, etc.). Para detalles técnicos de cada módulo
> consultar `ARCHITECTURE.md`.

---

## Concepto

Juego mobile top-down 2D de persecución asimétrica: **1 Hunter** persigue a **2–3 Preys**.
Los Preys ganan sobreviviendo el timer; el Hunter gana eliminándolos a todos.
Phase 1 (single-player vs bots) está funcional. Phase 3 (multiplayer) está preparada
arquitectónicamente (`IAuthorityContext`, `ISpawnService`) pero no implementada.

---

## Lo implementado

### Sistema de personajes
- `Character` (abstract MonoBehaviour) coordina sus subsistemas mediante `GetComponent` en `Awake()`.
- Subclases concretas: `HunterCharacter` (Team=Hunter) y `PreyCharacter` (Team=Prey).
- Subsistemas sibling: `CharacterMotor` (Rigidbody2D), `CharacterHealth`, `AbilityController`,
  `CombatController` (solo Hunter), `RevivableComponent` (solo Prey), `StatusEffectController`,
  `CharacterVisuals`, `CharacterStateMachine` (Idle/Move/Attack/Injured/Downed/Dead).

### Sistema de input (`IBrain`)
- `IBrain.CaptureIntent() → BrainIntent` desacopla input de gameplay.
- `PlayerBrain` lee Keyboard (Q/E/R, Space) + `HybridInputManager` (WASD + joystick virtual).
- `BotBrain` ejecuta un `BotStateMachine` con 7 estados.

### Sistema de habilidades (data-driven)
- `AbilityData` (ScriptableObject abstract) → `CreateRuntime()` → `Ability` (clase pura).
- `AbilityController` (componente) maneja lifecycle: activación, fase de Aim (`Aimer`),
  ejecución, cooldown.
- 3 tipos de Aimer: `NoAimer` (instant), `DirectionalAimer` (apunta), `AreaAimer` (área).
- **5 habilidades concretas implementadas**: `DashAbility`, `ProjectileAbility`,
  `FearProjectileAbility`, `TeleportSmashAbility`, `RemnantAbility`.

### Status effects
- `StatusEffect` (clase abstract) define `BlocksMovement`, `BlocksActions`,
  `SpeedModifier`, `ForceMoveInput`.
- `StatusEffectController` agrega efectos activos y aplica modificadores al Motor.
- **4 efectos**: `StunnedEffect`, `SlowedEffect`, `FearedEffect`, `HastedEffect`.

### Combat
- `IDamageable` interface + `DamageInfo` struct + `CombatController` con melee circular.

### Downed / Revive
- `RevivableComponent` en Preys: al morir van a estado downed con bleed-out timer.
- Aliados pueden revivir por proximidad (timer de progreso).
- `CharacterStateMachine.DownedState` maneja la lógica + UI feedback.

### IA (bots)
- `BotBrain` + `BotLocomotion` (wrapper de A* AIPath + Seeker) + `BotStateMachine`.
- **7 estados de bot**: Hunter (Patrol/Chase/Attack/Search), Prey (Wander/Flee/Revive).
- `BotChaseState` con interception (lead target via velocity), scoring de targets
  (downed > herido > aislado > distancia), y disparo de habilidades por reglas declarativas.
- `BotFleeState` con escape-fan: raycast en N direcciones contra muros + burst lateral
  post-stuck. Soluciona el bug clásico de "Prey atascado en esquina".
- **Habilidades por data-rules**: `AbilityUseRule[]` en `BotTuningData` define cuándo
  el bot dispara cada slot (banda de distancia, cooldown interno, condición sobre target).
  Cooldown global previene spam.

### Match flow
- `GameManager` (singleton de escena) orquesta una `MatchStateMachine` con 4 estados:
  `LobbyState` → `StartingState` (countdown + spawn) → `PlayingState` (timer + win check)
  → `EndingState` (publica resultado).

### Fog of War (estilo Dota 2)
- **Terreno siempre visible pero oscurecido**, personajes ocultos fuera de visión,
  fade suave radial en el borde del radio.
- `FogOfWarManager` se auto-conecta al jugador local vía `CharacterSpawnedEvent`.
  Soporta múltiples `VisionSource` (uno por aliado en multiplayer futuro).
- Shader `Game/FogOverlay`: combina textura de polígono de visibilidad (raycast 360°)
  con smoothstep distance-based fade.
- Shader `Game/CharacterEffect`: incluye bloque `Stencil` para que personajes con
  `MaskInteraction = VisibleInsideMask` respeten el SpriteMask de visión.

### UI
- `HUDController`, `AbilityHUD` (slots con cooldown radial), `FloatingHealthBar`
  (world-space sobre personajes), `DownedIndicatorPanel` (estado de Preys),
  `StatusIconDisplay` (iconos de efectos sobre el personaje), `CameraFollow`
  (sigue al jugador local).

### Infraestructura (Phase 3-ready)
- `EventBus` (pub/sub genérico, struct-only para evitar GC).
- `ServiceLocator` (registry de servicios).
- `IAuthorityContext` / `LocalAuthority` (abstracción de autoridad de simulación).
- `ISpawnService` / `LocalSpawnService` (abstracción de instanciación).

### VFX
- `VFXSpawner` estático (PlayOnce + Attach) + `VFXHandle` para detener efectos atados.

---

## Estructura de carpetas

```
Assets/
├── _Game/
│   ├── Scripts/
│   │   ├── Core/                        EventBus, ServiceLocator
│   │   ├── Events/                      GameEvents (todos los structs)
│   │   ├── Data/                        CharacterData, AbilityData, BotTuningData,
│   │   │                                MatchSettings, AbilityUseRule, CharacterTeam
│   │   ├── Networking/                  IAuthorityContext, LocalAuthority
│   │   ├── Services/                    ISpawnService, LocalSpawnService
│   │   ├── Gameplay/
│   │   │   ├── Characters/              Character, motor, health, visuals, revivable,
│   │   │   │   └── Effects/             status effects + controller, status effect concretos
│   │   │   ├── States/                  CharacterStateMachine + Idle/Move/Attack/Injured/Downed/Dead
│   │   │   ├── Brains/                  IBrain, BrainIntent, PlayerBrain, BotBrain
│   │   │   ├── Abilities/               Ability, AbilityController, Context, AimResult,
│   │   │   │   ├── Aimers/              NoAimer, DirectionalAimer, AreaAimer
│   │   │   │   ├── Concrete/            DashAbility, ProjectileAbility, FearProjectile,
│   │   │   │   │                        TeleportSmash, Remnant + Data SOs
│   │   │   │   └── Runtime/             Projectile, FearProjectile, RemnantDecoy
│   │   │   ├── Combat/                  IDamageable, DamageInfo, CombatController
│   │   │   ├── AI/                      BotLocomotion, BotStateMachine
│   │   │   │   └── States/              7 estados de bot
│   │   │   └── Interactions/            IInteractable (placeholder)
│   │   ├── Match/                       GameManager, MatchStateMachine
│   │   │   └── States/                  Lobby/Starting/Playing/Ending
│   │   ├── FogOfWar/                    FogOfWarManager, VisionSource
│   │   ├── FX/                          VFXSpawner, VFXHandle
│   │   ├── UI/                          HUDController, AbilityHUD, FloatingHealthBar, etc.
│   │   ├── Input/                       IInputProvider, KeyboardInputProvider,
│   │   │                                HybridInputManager, VirtualJoystick
│   │   ├── Map/                         MapLoader, SpawnPoint
│   │   ├── Utils/                       CameraFollow, DebugDrawer, DebugGizmoSettings
│   │   └── Editor/                      SandboxBuilder
│   ├── ScriptableObjects/
│   │   ├── Abilities/                   DashAbility, ProjectileAbility, Hunter1/*
│   │   ├── BotTuning/                   HunterBotTuning, PreyBotTuning
│   │   ├── Characters/                  HunterData, PreyData
│   │   ├── Match/                       SandboxMatch
│   │   └── Data/                        DifficultySettings
│   ├── Prefabs/                         Hunter, Prey, Hunter Bot, Prey Bot, Floor, Wall,
│   │   │                                SpawnPoint_Player, SpawnPoint_Pursuer, HealthBar
│   │   ├── Habilities/                  Projectile, FearProjectile, HunterRemnantDecoy
│   │   └── UI/                          StatusIcons
│   ├── Shaders/                         CharacterEffect.shader, FogOverlay.shader
│   ├── Materials/                       CharacterEffect.mat, FogOverlay.mat, LitParticle.mat
│   └── Scenes/                          00_Bootstrap, 00_Sandbox (principal), 01_Map_Test, 02_Game
└── AstarPathfindingProject/             A* (third party)
```

---

## Cómo retomar

1. Abrir Unity con la escena **`00_Sandbox.unity`** (escena principal de desarrollo).
2. Pulsar Play. El `GameManager` arranca la `MatchStateMachine` automáticamente.
3. Punto de entrada del flujo: `GameManager.SpawnMatchEntities()`.
4. Para debug, los `CharacterDebugGizmos` y `DebugOverlayToggle` permiten visualizar
   estados, vision, pathfinding.
5. Para auditoría rápida del estado del proyecto, ver herramientas MCP
   (`Unity_RunCommand` con scripts editor).

---

## Roadmap restante

### Pendiente crítico
- **Material de bot prefabs**: `Hunter Bot.prefab` y `Prey Bot.prefab` usan
  `Sprite-Lit-Default` en vez de `CharacterEffect.mat` → no respetan SpriteMask del FoW
  correctamente (los bots pueden verse a través de la niebla). Asignar manualmente.
- **XML doc summaries**: ~80+ miembros públicos sin documentación XML
  (impacta intellisense, no funcionalidad).

### Funcionalidad faltante
- **UI de selección de rol**: el `playerTeam` se configura desde Inspector del GameManager.
  Falta pantalla de lobby.
- **Restart / Replay**: `EndingState` publica `MatchEndedEvent` pero no hay UI para reiniciar.
- **Habilidades defensivas de Prey**: solo hay `DashAbility`. Faltan: escudo, invisibilidad
  corta, dash con i-frames, etc.
- **Audio**: no existe ningún sistema de audio.
- **Animaciones**: sprites estáticos (sin animaciones de idle/walk/attack).

### Phase 3 (multiplayer)
La arquitectura está preparada: `IAuthorityContext`, `ISpawnService`, `ServiceLocator`,
todos los efectos son `[Serializable]`, comunicación vía `EventBus` con structs.
Migración a Photon Fusion o Netcode for GameObjects sin reescribir gameplay.

### Mejoras futuras
- Generación procedural de mapas
- Arte final (todo es placeholder)
- Menú principal
- Más modos de juego (capturas, objetivos)
- Leaderboards / progresión
- Tutoriales
- Más personajes con kits únicos (ver guías en `ARCHITECTURE.md`)
