# Chase Game — Roadmap

Roadmap por **hitos**, no por fechas. Cada fase se cierra cuando su *gate* se cumple,
no cuando pasa un tiempo. El orden es por dependencia: primero el build-out tecnico
(hasta tener producto jugable en red), luego el bloque comercial.

**Ultima actualizacion:** 2026-06-07 · Estado del codigo: Phase 2 (UI/meta-layer landscape) implementada

---

## Principios transversales (aplican a todas las fases)

- **Movil landscape como target** (decision 2026-06-06, ver memoria `phase2-ui-decisions`),
  jugable en el editor de Unity (PC) para test/coding. Orientacion bloqueada a landscape
  (gameplay + menus); todo HUD/joystick/botones se disena horizontal; el input hibrido
  (teclado + joystick virtual) se mantiene para testear sin device.
- **Arquitectura networking-ready, sin bakear decisiones**: el gameplay nunca toca
  `GameManager.Instance` ni `Object.Instantiate` directo. Pasa por las costuras ya
  sembradas: `ServiceLocator`, `IAuthorityContext`, `ISpawnService`, `IWorldQueryService`,
  `EventBus`. El stack de red (Fusion 2 u otro) y el modelo de autoridad estan **por decidir**;
  la fase de networking solo intercambia las implementaciones `Local*` por las de red.
- **El proyecto debe compilar y correr tras cada workstream.** Verificacion via Unity MCP
  (`Unity_GetConsoleLogs`, `Unity_RunCommand`) — ver memoria `unity-mcp-verification`.
- **Data-driven primero**: personajes, habilidades, tuning de bots, settings de match viven
  en ScriptableObjects. Agregar contenido no deberia requerir tocar codigo de sistemas.

---

## Fase 0 — Baseline (COMPLETADO)

Lo que ya existe y funciona. Documentado para honestidad del roadmap.

- **Core gameplay**: movimiento (motor RB2D), combate basico del hunter (hit/herido),
  framework de habilidades data-driven (aim/cast/cooldown), 5 habilidades concretas.
- **AI de bots**: FSM (patrol/chase/attack/search · wander/flee/revive) + pathfinding A*,
  reglas de habilidad data-driven, percepcion con LOS y scoring.
- **Downed + revive por proximidad** (sin muerte real), status effects (stun/slow/fear/haste).
- **Fog of War** (mesh procedural + RenderTexture), **match flow** (lobby/starting/playing/ending),
  condiciones de victoria, panel de resultado.
- **Infra**: EventBus, ServiceLocator, AudioManager + MusicManager (pool + crossfade),
  VFXSpawner, diagnostics panel.
- **Hardening (auditoria 2026-05)**: GC en hot paths, leaks corregidos, proyectiles a
  FixedUpdate, `IWorldQueryService` desacoplando 4 consumidores de `GameManager`, API visual
  de status effects (OCP). Ver memoria `audit-refactor-2026-05`.

**Pendiente tecnico arrastrado:** Object Pooling de proyectiles (pospuesto, tanda dedicada).

---

## Fase 1 — Consolidar el Game Loop

**Objetivo:** una partida completa que se pueda **rejugar sin reiniciar Unity ni la app**,
con el modelo fantasma del downed funcionando.

**Gate:** jugar N partidas seguidas (rematch y via lobby), alternando rol hunter/prey,
sin reinicios ni fugas de estado entre partidas.

### Workstream 1.1 — Modo Fantasma del downed
- Al caer downed: el cuerpo queda como **ancla revivible** en el suelo; el jugador pasa a
  controlar un **fantasma** (no es muerte definitiva).
- Fantasma: movimiento libre (roam) + utilidad ligera = **1 ping de posicion** a los vivos
  (señalar algo, un solo uso por downed). Sin combate. Interaccion con mapa = futuro (no ahora).
- **Puntero UI** desde el fantasma hacia la posicion del cuerpo (para no perderlo) que
  **cambia/notifica cuando un aliado te esta reviviendo**.
- Al completarse el revive sobre el cuerpo: el fantasma **regresa al cuerpo** y se retoma el
  gameplay normal de prey (reusar `RevivableComponent` + `CharacterRevivedEvent`).
- **Regla de fin canonica** (ya consistente con `PlayingState`): los hunters ganan cuando
  **todas las preys estan downed simultaneamente** (sin aliado vivo que revive). El fantasma
  importa solo mientras quede ≥1 aliado vivo que pueda revivir; bleed-out sigue desactivado.

### Workstream 1.2 — Loop de rejugar (consolidar)
- `EndingState`: agregar **rematch** (misma config, reset in-place) y **volver al lobby**
  (cambiar rol/personaje), sin recargar la app. Hoy `EndingState.Tick` esta vacio.
- **Reset limpio de partida**: despawn de entidades, `EventBus.Clear` selectivo, reinicio de
  `MatchStateMachine`, re-spawn. Evitar estado residual (timers, suscripciones, pooled).
- `LobbyState` deja de estar vacio: punto de retorno real del loop.

### Workstream 1.3 — Seleccion de rol/personaje (local)
- Elegir hunter o prey antes de la partida (hoy es `playerTeam` en el inspector del GameManager).
- Seleccion de personaje (lee `CharacterData`), wired al spawn del player.
- Composicion de la partida (cuantos bots por bando) configurable desde el flujo, no solo inspector.

---

## Fase 2 — UI / UX / Meta-layer (landscape)  *(IMPLEMENTADA 2026-06)*

**Objetivo:** el meta-juego front-end completo en landscape (Hub, Customize, Detalle de
Personaje, Select Gamemode, stubs Shop/Chest) sobre un **loadout persistente** (1 Hunter +
1 Prey + 3 emotes), con rol asignado al azar en partida. Economia display-only (sin earn/spend).

**Gate:** la app abre en Meta → equipar hunter/prey/skin/emotes (persiste en disco) → elegir
modo → JUGAR → partida con rol aleatorio usando lo equipado → resultado → Revancha (in-place)
o Salir (vuelve al Hub), todo en landscape, sin reinicios. **Verificado en Play via MCP.**

Decisiones cerradas (ver memoria `phase2-ui-decisions`): landscape total · 2 escenas (Meta +
Gameplay) · `MetaCharacter` envuelve `CharacterData` · loadout persistente (JSON en
`persistentDataPath`), economia display-only.

### Workstream 2.1 — Arquitectura meta (DONE)
- **Capa persistente** `AppRoot` (DontDestroyOnLoad) registra `IProfileService` para que el
  loadout sobreviva el cambio Meta↔Gameplay. Modelo de datos SO: `MetaCharacter`/`Skin`/
  `EmoteData`/`GameModeData`/`MetaCatalog`; estado serializable `ProfileState` (loadout +
  ownership + currencies).
- **Screen system** desacoplado: `IScreenService`/`ScreenService` con back-stack;
  `ScreenController` base; la UI habla solo via `EventBus` (LoadoutChanged/Currency/GameMode/
  ScreenChanged) y `ServiceLocator`, nunca screen-a-screen.
- **2 escenas**: `00_Meta` (boot, idx0) + Gameplay (idx1). JUGAR fija `MatchConfig` y carga
  Gameplay; "Salir" desde resultados vuelve a `00_Meta`.

### Workstream 2.2 — Pantallas meta (DONE)
- **Hub**: avatar equipado con toggle Hunter/Prey, perfil, monedas (display), accesos
  SHOP/CUSTOMIZE/CHEST, selector de modo, JUGAR.
- **Customize**: sidebar (Hunters/Preys/Emotes), grid reusable con owned/locked (silueta),
  sort por rareza + favoritos persistidos, rueda de 3 emotes.
- **Detalle de Personaje**: splash, rareza, habilidades (de `CharacterData.abilities`),
  seleccion de skin con preview, carrusel, READY = equipar.
- **Select Gamemode**: modo destacado "Modo del dia" (rotacion diaria determinista) +
  secundarios; solo Supervivencia funcional, resto `isComingSoon`.
- **Shop / Chest**: stubs (entrar/salir), contenido real en Phase 7.

### Workstream 2.3 — Cableado de partida (DONE)
- Rol **al azar** al entrar desde el Meta; `GameManager.SpawnOne` lee `IProfileService` y
  aplica la `Skin` equipada al hijo "Visual" (swap de `RuntimeAnimatorController` +
  `CharacterAnimator.RebuildStateTable`). Las skins no afectan gameplay.
- Resultados: "Revancha" = reset in-place; "Salir" = `LoadScene(00_Meta)`.
- `GameManager` vuelve a ser estrictamente scene-scoped (se removio el `SceneController`
  DontDestroyOnLoad que lo arrastraba a persistir y filtraba la instancia al volver al Meta).

### Workstream 2.4 — Pendiente de Phase 2 (pulido)
- HUD de partida en landscape: re-layout de joystick/habilidades/timer/corazones (placeholder
  actual). UI del fantasma: **puntero al cuerpo**, boton de **ping**, prompt + barra de revive.
- Settings/pausa (buses ya en `GameAudioMixer`), sensibilidad de joystick, transiciones/juice.
- Arte real de tarjetas/iconos/splashes llega en Phase 6 (hoy placeholders; splash de prueba
  via `SplashartTest.mp4`).
- **PENDIENTE (FoW oscurece al personaje):** los sprites de personaje (sortingOrder 2) quedan
  por DEBAJO del `FogOverlay` (order 20), asi que la niebla los oscurece. Pegado a una pared, la
  vision (raycast desde los pies) no llega a la cabeza y el overlay corta/oscurece la parte alta
  del sprite. Diseno buscado: la niebla solo oscurece TERRENO; los personajes son binarios
  (mostrar/ocultar via `CharacterFogVisibility`). Fix recomendado (Opcion A): subir el
  sortingOrder de los sprites de personaje por encima del overlay cuando estan visibles
  (`CharacterFogVisibility.Apply` ya lo promete en su comentario pero el codigo no lo hace).
  Decidido posponer 2026-06-08.

---

## Fase 3 — Audio & Game Feel

**Objetivo:** la partida se siente y suena completa.

**Gate:** ninguna accion clave (hit, habilidad, revive, ping fantasma, UI, fin de partida)
queda sin audio ni feedback.

### Workstream 3.1 — Audio
- Cobertura de SFX: ataques, hits, habilidades (cast/impacto), revive, fantasma/ping, UI.
- Estados de musica: menu / partida / ending (infra de crossfade ya existe en `MusicManager`).
- Crear/asignar `AudioCue` faltantes; mixdown por buses.

### Workstream 3.2 — Game feel
- Hit-stop, screen shake, cobertura de VFX (usar `VFXSpawner`), **haptics** en movil.
- Camara: follow pulido, framing portrait, zoom segun contexto.

---

## Fase 4 — Contenido & Balance

**Objetivo:** roster y mapas suficientes para que el juego tenga rejugabilidad.

**Gate:** 2 hunters + 4 preys jugables, 2 mapas, 2 modos (Supervivencia + Recoleccion),
balance validado en playtest local vs bots.

### Workstream 4.1 — Personajes
- 2 hunters con filosofia de caza diferenciada; 4 preys (evasion, area, soporte, otro perfil).
- Stats y cooldowns diferenciados por `CharacterData`; reglas de bot por `BotTuningData`.

### Workstream 4.2 — Mapas
- 2 layouts con elementos interactivos/destructibles; sorting por eje Y; zonas de riesgo y atajos.

### Workstream 4.3 — Modos de juego
- **Supervivencia** (sobrevivir el tiempo). **Recoleccion** (objetivo de items).
  `MatchSettings` ya prevé modos; formalizar la abstraccion de modo.
- Aqui es donde la **interaccion del fantasma con el mapa** entra como mejora futura.

### Workstream 4.4 — Balance
- Iterar velocidades hunter vs prey, cooldowns, frecuencia de revival, duracion de partida.

---

## Fase 5 — Networking MVP

**Objetivo:** partidas multijugador reales sobre la base local ya estable.

**Gate:** 4 personas en partida estable, sin desconexiones criticas, con paridad de
comportamiento respecto al modo local.

> **Decision abierta:** stack (Photon Fusion 2 vs alternativa) y modelo de autoridad
> (host-authoritative vs shared) **por decidir**. La arquitectura ya esta preparada para
> cualquiera de los dos: solo se intercambian las implementaciones `Local*` de los servicios.

### Workstream 5.1 — Capa de red sobre los servicios sembrados
- Implementaciones de red de `ISpawnService` (spawn replicado), `IAuthorityContext`
  (autoridad real), `IWorldQueryService` (listas desde objetos de red).
- Sincronizar: posicion, habilidades+efectos, sistema de golpes/estados, **fantasma+revive**,
  timer de partida, items de modo.

### Workstream 5.2 — Matchmaking
- Sala privada con codigo (jugar con amigos) + sala publica (matchmaking).
- Seleccion de personaje en lobby, asignacion de rol (1 hunter, resto preys).
- Reconexion basica; manejo de caida del hunter (cancelar partida o reemplazar por bot).

### Workstream 5.3 — UI de red
- Pantalla de busqueda, indicador de ping/conexion, salas. (Reusa el lobby de Fase 2.)

---

## Fase 6 — Arte definitivo  *(inicio del bloque comercial)*

**Objetivo:** arte final integrado, coherente, en landscape.

**Gate:** build con arte definitivo, sin placeholders en pantallas o personajes principales.

- Definir estilo visual final (pixel art 3/4 es candidato). Sprites de los personajes con
  animaciones (idle/run/hit/defeat/ghost). Tilesets de los mapas. UI final (botones, barras,
  iconos de habilidad). Particulas. **Icono de la app** (prioritario para stores).

---

## Fase 7 — Monetizacion & Analytics

**Objetivo:** sistemas de economia y medicion listos.

**Gate:** moneda + tienda + ads opcionales + analytics de retencion/conversion funcionando en build.

- Moneda comun (recompensa por partida) + premium (IAP via Unity IAP).
- Tienda de personajes (ambas monedas) y skins (premium). Balance earn-rate vs costo.
- Rewarded ads opcionales (AdMob/ironSource), **sin interstitials obligatorios**.
- Firebase Analytics: eventos de partida, retencion D1/D7/D30, conversion, funnel de onboarding.

---

## Fase 8 — Launch

**Objetivo:** publicacion en stores con comunidad activa.

**Gate:** soft launch validado y publicacion en App Store + Google Play.

- Soft launch regional → fix de criticos → ASO (titulo, keywords, screenshots, video).
- Apple Developer + Google Play Console; builds iOS (TestFlight) y Android (Internal→Production).
- Cumplimiento de politicas de monetizacion. Comunidad (Discord/Reddit) + devlog.
- Post-launch: monitor de crashes (Unity Cloud Diagnostics), respuesta a reviews, cadencia de
  contenido, evaluar Photon Dedicated/servidor segun CCU.

---

## Notas de diseño

### Modelo de muerte (canonico)
Downed NO es muerte. Cuerpo = ancla revivible; jugador controla un fantasma (roam + 1 ping).
Revive del cuerpo por aliado → fantasma vuelve al cuerpo → gameplay normal de prey.
Hunters ganan si TODAS las preys estan downed a la vez (sin aliado vivo para revivir).

### Costuras de networking ya disponibles
`ServiceLocator` (registro swappable) · `IAuthorityContext` (`CanSimulate`/autoridad) ·
`ISpawnService` (spawn/despawn) · `IWorldQueryService` (consultas de mundo) · `EventBus`
(pub/sub). Mantenerlas como unica via: no introducir acoplamientos nuevos a `GameManager`.

---

## Riesgos

| Riesgo | Severidad | Mitigacion |
|---|---|---|
| Networking mas complejo de lo esperado | Alta | Arquitectura ya desacoplada (servicios). Prototipo de red en paralelo a Fase 4, no esperar a Fase 5. |
| El modo fantasma rompe el balance o la claridad | Media | Empezar minimo (roam + 1 ping, sin interaccion de mapa). Validar en playtest antes de ampliar. |
| Loop de rejugar deja estado residual (leaks/suscripciones) | Media | Reset explicito y verificable; checklist de despawn/Clear; test de N partidas seguidas. |
| Arte tarda o no cuadra con el estilo | Alta | Definir estilo y contactar artista durante Fase 4, no esperar a Fase 6. |
| Landscape y framing del mapa top-down | Media | Validar framing/zoom de camara al hacer el HUD landscape (WS 2.4); ajustar vision/FoW si hace falta. |
| Cold start: nadie en matchmaking | Media | Sala privada por codigo desde el dia 1 + soft launch regional concentrado. |
| Apple rechaza por monetizacion | Baja | Revisar guidelines antes de implementar IAP/ads, no despues. |

---

## Decisiones abiertas (pendientes de definir)

- **Stack de networking y modelo de autoridad** (Fusion 2 host vs shared vs alternativa).
- **Capacidades futuras del fantasma** (interaccion con mapa: puertas, recoleccion) — fuera de alcance inicial.
- **Estilo de arte final** y si el artista es freelance o socio.
- **Set exacto de modos** mas alla de Supervivencia + Recoleccion.
