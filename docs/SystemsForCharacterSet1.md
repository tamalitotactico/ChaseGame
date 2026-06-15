# Sistemas / infra para el set 1 de personajes

Mecanicas transversales y piezas reutilizables que habilitan a [CharacterDesigns.md](CharacterDesigns.md).
Se priorizan bases COMPARTIDAS para no duplicar (ej. trampas/placeables con cupo, dash-a-target).
Todo data-driven y tuneable. Cada item lista: que hace, donde encaja en lo existente, y seam de red (Fusion).

Estado: `[ ]` sin implementar . `[~]` parcial . `[x]` listo.

---

## 1. StatusEffects nuevos
Base existente: [StatusEffect.cs](../Assets/_Game/Scripts/Gameplay/Characters/StatusEffect.cs)
(BlocksMovement, BlocksActions, SpeedModifier, ForceMoveInput, VisualTint/Priority).
Ya existen: Feared, Slowed, Hasted, Stunned.

| Efecto | Que hace | Hook / base | Usado por | Estado |
|---|---|---|---|---|
| `CharmedEffect` | Jale **progresivo** hacia un punto fijo (fear invertido): `ForceMoveInput` apunta del prey HACIA el punto, recalculado cada frame contra la pos actual del target (guarda el target en OnApply). pullStrength escala la magnitud; arriveRadius corta el jale al llegar. BlocksActions. | clon de `FearedEffect` invertido | Charmer/Enchant | `[x]` |
| `CCImmunityEffect` | Mientras activo, `StatusEffectController.Apply` rechaza nuevos efectos de control. No bloquea buffs. GrantsCCImmunity=true. | gate de inmunidad en Apply (ya existia) | Charmer/Repel | `[x]` |
| `InvisibleEffect` | Oculto para **enemigos** siempre (EnemyRevealRadius=0). Aliados lo ven. Rompe al actuar. | canal de visibilidad (item 4) | TrickyWizard/Tricky Lure | `[x]` |
| `CamouflageEffect` | Oculto para enemigos con **fade por distancia** (visible si enemigo < revealRadius). Aliados siempre lo ven. Bundle: + haste (SpeedModifier) + BreaksOnOwnerAction. | canal de visibilidad (item 4) | Drowned/Nadar | `[x]` |
| `BlindedEffect` | Reduce el FOV/vision del afectado (multiplicador) por una duracion. IsControlEffect (lo dispela Repel). | `VisionSource.SetRadiusMultiplier(this, m)` en OnApply / Clear en OnRemove (item 6) | Trapper/Smoke Trap, TrickyWizard/Disparo cegador | `[x]` |
| `TrueFormEffect` | Bundle temporizado: haste (SpeedModifier) + letalidad (GrantsLethalAttack -> CombatController estampa DamageInfo.Lethal, item 5) + VisualTint. NO es control (no lo dispela Repel, no lo bloquea CC immunity). Modo DownInOne; DamageMultiplier seria infra futura. | un solo efecto que expone SpeedModifier+GrantsLethalAttack | Werewolf/True Form | `[x]` |

`StunnedEffect` y `SlowedEffect` se reusan tal cual (Bear Trap, Disparo cegador, mordisco del lobo, etc.).
Ya marcados como `IsControlEffect=true` (junto con `FearedEffect`) para inmunidad/dispel.

**Cambio en `StatusEffectController`: HECHO `[x]`.** (a) gate de inmunidad en `Apply` (rechaza
`IsControlEffect` si `HasCCImmunity`; `Apply` ahora devuelve bool), (b) `DispelControlEffects()`
(remueve control, deja buffs; lo usara Repel), (c) helpers `HasLethalAttack` / `HasCCImmunity`.
Base `StatusEffect` extendida con `IsControlEffect` / `GrantsLethalAttack` / `GrantsCCImmunity`.

---

## 2. Ultimate por carga de hits (slot 2 de hunters) -- HECHO `[x]`
Reemplaza el cooldown del slot 2 por un contador de hits de ataque basico a preys.

- `CombatController` estampa `DamageInfo.FromBasicAttack=true`; `Character.TakeDamage` publica
  `BasicAttackLandedEvent { Attacker, Victim }` solo cuando un ataque basico aplica dano.
- `AbilityController` lleva `_hitCharge[slot]`: se suscribe a `BasicAttackLandedEvent` y suma 1 a los
  slots con `usesHitCharge` cuando el atacante es este personaje (cap en `hitsRequired`).
- `AbilityData` expone `usesHitCharge` (bool) + `hitsRequired` (default 2).
- `SlotReady` para estos slots depende del contador, no del cooldown. `OnCooldownChanged` emite el
  fill como progreso de carga (0..1) y "remaining" = hits que faltan (para HUD).
- Reset a 0 al ejecutar (consumo). El "al terminar el efecto" queda cubierto: se resetea al cast.
- Refund: `Ability.CanExecute(ctx, aim)` (default true). Si devuelve false (ult sin target valido),
  `FinishAndExecute` cancela sin SFX, sin cooldown y SIN consumir la carga. Lo usara Assault.
- `AbilityController.GetHitCharge(slot)` expuesto para el HUD.

NOTA: el slot R del Crowmaster (TeleportSmash) sigue en cooldown hasta que se ponga `usesHitCharge=true`
en su asset (default false -> sin regresion). Verificado en play: 0->2 con cap en el 3er hit.

Seam Fusion: el contador es estado replicable ([Networked] int) validado por autoridad.
Estado: `[x]`

---

## 3. Placeables con cupo limitado (base compartida)
Patron comun a Raise Wall (2), Smoke Trap (2), Bear Trap (3) y reusa la idea del decoy existente
([RemnantDecoy](../Assets/_Game/Scripts/Gameplay/Abilities/Runtime/RemnantDecoy.cs)).

- Base `PlaceableAbility` (Ability) + `PlaceableRegistry` (estatico, por owner+tipo): al exceder el cupo,
  despawnea el mas antiguo. `Placeable` (MonoBehaviour base) maneja owner + lifetime (0 = infinito) y
  `SetLifetime` (Raise Wall lo usa para fijar 5s al ser tocado).
- Cada placeable spawnea via `ISpawnService` -> Fusion-ready.
- HECHO `[x]` (Bloque 4): base + Raise Wall + Beacon.
  - **Raise Wall** (`RaiseWallPlaceable`): largo auto por raycast (de pared a pared, tope `maxLength`),
    zona de slow por OverlapBox filtrada por team (solo hunters), 5s al ser tocado. Visual por
    SpriteRenderer.size (drawMode Sliced). Verificado: cupo 2, largo 5, slow al hunter.
  - **Beacon** (`BeaconPlaceable`, IDamageable): aura de haste a aliados via IWorldQueryService
    (refresca exitBoostDuration -> persiste al salir), el hunter la rompe de 1 golpe (ignora dano aliado).
- Pendiente (Bloque 5): **Smoke Trap** (BlindedEffect en area, #6) y **Bear Trap** (StunnedEffect + reveal #7).

Estado: `[~]` (base + Engineer hechos; trampas del Trapper pendientes)

---

## 4. Canal de visibilidad por estado (invisibilidad / camuflaje)
Analogo a [CharacterFogVisibility](../Assets/_Game/Scripts/FogOfWar/CharacterFogVisibility.cs) (que ya
decide visibilidad por niebla). Nuevo canal que decide visibilidad por **estado** (invis/camuflaje),
independiente de la niebla.

- Regla central en UN solo lugar: "un character con invis/camuflaje es visible para un viewer si
  (viewer es aliado) OR (viewer es enemigo y [invis: nunca] / [camuflaje: distancia < revealRadius, con
  fade])".
- Local (viewer = jugador local): se evalua contra el bando del jugador local.
- Aliados siempre lo ven, con shader de "invisible/camuflado" + icono de estado.
- Combina con el alpha de la niebla (toma el minimo de ambos canales).

HECHO `[x]`: la regla vive en `StateVisibility.AlphaFor(target, viewer)` (un solo lugar) y devuelve un
ALPHA [0..1]; `CharacterVisuals.Update` lo multiplica sobre el color del sprite. `StatusEffect` expone
`HidesFromEnemies` + `EnemyRevealRadius` (<=0 = invisible total, >0 = camuflaje con fade);
`StatusEffectController.GetHidingEffect()`. Viewer local = `PlayerBrain.Local`. Se combina con la niebla
porque CharacterFogVisibility togglea el `enabled` del SR por separado (efectivamente el minimo). El
"romper al actuar" usa `BreaksOnOwnerAction` + `StatusEffectController.BreakActionSensitiveEffects()`
(lo llaman CombatController.DoAttack y AbilityController antes de Execute). Reutilizable por InvisibleEffect
(item 1, Bloque 6: EnemyRevealRadius=0).

Seam Fusion: la fuente de "quien es el viewer / su bando" cambia a por-cliente; la regla queda intacta.
Estado: `[x]`

---

## 5. Letalidad / ejecucion (instakill correcto) -- HECHO `[x]`
Para True Form. Evita atar el "1 golpe" al HP actual del prey.

- [DamageInfo](../Assets/_Game/Scripts/Gameplay/Combat/DamageInfo.cs): campo `Lethal` (bool). HECHO.
- [CombatController.DoAttack](../Assets/_Game/Scripts/Gameplay/Combat/CombatController.cs) consulta
  `owner.StatusEffects.HasLethalAttack` y estampa `Lethal = true` en el golpe basico. HECHO.
- `CharacterHealth.TryDamage` con `Lethal` lleva el HP a 0 (-> downed) sin importar el valor actual;
  sigue respetando la invulnerabilidad salvo `IgnoreInvulnerability`. HECHO. Verificado en play (3HP->0).
- Pendiente (cuando se haga True Form): exponer `lethalMode` tuneable = `DownInOne` (default) o
  `DamageMultiplier(x)` en el efecto, si balance lo pide. El canal `GrantsLethalAttack` ya esta.

Estado: `[x]` (la parte de infra; el efecto True Form en si va con el Werewolf)

---

## 6. Hook de modificacion de vision (FOV) y reveal -- HECHO `[x]`
- `VisionSource.VisionRadius = visionRadius * producto(multiplicadores)`. API:
  `SetRadiusMultiplier(key, m)` / `ClearRadiusMultiplier(key)` (key = el efecto). No pisa el base.
- `BlindedEffect` aplica el multiplicador en OnApply y lo quita en OnRemove. Verificado: hunter base 8 -> 3.2 (x0.4).
- NOTA: los prefabs Hunter/Prey YA tienen un VisionSource (hunter base 8, prey 5); se referencia por GUID
  en el .prefab (un grep por "VisionSource" no lo encuentra). Buffs de vision (signo inverso) reusan la API.

Estado: `[x]`

---

## 7. World Target Pointer (puntero a objetivo en mundo) - generalizacion
Generaliza [GhostBodyPointer](../Assets/_Game/Scripts/UI/GhostBodyPointer.cs) (hoy especifico:
fantasma -> su cuerpo) a un puntero reutilizable: baliza sobre el target si esta en pantalla, flecha
clampeada al borde si esta fuera. Configurable (sprite, tamano, color, duracion).

- HECHO `[x]`: `WorldTargetPointer.Show(target, duration, sprite, size, color)` (UI/WorldTargetPointer.cs).
  Auto-construye Canvas overlay, baliza on-screen / flecha clampeada off-screen (logica de GhostBodyPointer),
  se autodestruye al expirar. Tunables vienen de la AbilityData del llamador (BearTrapAbilityData), respetando
  la politica runtime-UI (token RUNTIME-UI-REVIEW + entrada en docs/RuntimeUIReview.md pendiente de agregar).
- Usuario actual: Bear Trap (revela al hunter a los preys; gate: solo si el jugador LOCAL es del bando del owner).
- Pendiente: usarlo en reveals de Ghost Wolf (mordisco), Remnant, Smell.

Estado: `[x]`

---

## 8. Aimer "apuntar a aliado"
Los aimers actuales (DirectionalAimer, AreaAimer, TapAimer, AimThenCastAimer) apuntan a direccion,
posicion o enemigo. Falta apuntar a un **aliado** (Booster Pills; Sanacion usa AoE+nearest, no aimer).

- HECHO `[x]`: `AllyTargetAimer(aimRange)` resuelve el aliado vivo mas cercano al punto apuntado via
  `IWorldQueryService.GetAlliesOf` (excluye al owner). Si no hay, GetResult sin target y la habilidad cae
  a si misma. Lo usa Booster Pills (Medic). Verificado en el Preview Harness (con dummies aliados).

Estado: `[x]`

---

## 9. Dash/teleport a TARGET (generalizacion de TeleportSmash)
[TeleportSmashAbility](../Assets/_Game/Scripts/Gameplay/Abilities/Concrete/TeleportSmashAbility.cs) hoy
salta en una DIRECCION fija. Las nuevas saltan a un TARGET o a un PUNTO apuntado:
- **Assault** (Charmer): al enemigo en vision mas cercano (cancel+refund si no hay).
- **Ejecucion** (Drowned): a la posicion apuntada (AreaAimer) + AoE de dano.

Extraer la parte comun de "mover owner a destino respetando muros (raycast/padding) + Motor.Stop +
VFX/SFX de llegada" a un helper reutilizable.

HECHO `[~]`: `DashHelper.DashTo(owner, destination, wallLayer, wallPadding)` (Runtime/DashHelper.cs)
hace raycast a destino, aterriza antes del muro, mueve al owner y para el motor; devuelve la pos real.
Lo usan **Assault** (salta al enemigo mas cercano) y **Ejecucion** (Drowned, salto al punto apuntado +
AoE). El VFX/SFX de llegada lo maneja cada habilidad. Para "apuntar a un punto y luego canalizar" se
agrego `AreaThenCastAimer` (analogo a AimThenCastAimer pero devuelve TargetPosition). Opcional a futuro:
refactorizar TeleportSmash para que use el helper (hoy mantiene su copia, sin tocar para no arriesgarla).

Estado: `[x]`

---

## 10. A* para Smell y Ghost Wolf
El proyecto tiene **A\* Pathfinding Project** (define `ASTAR_EXISTS`,
[BotLocomotion](../Assets/_Game/Scripts/Gameplay/AI/BotLocomotion.cs) ya lo usa).
- **Smell**: `ABPath.Construct(hunterPos, preyPos)` por cada prey al cast; tomar `path.vectorPath`
  (waypoints) y dibujarlos como trail (LineRenderer/mesh). Snapshot: no recalcular.
- **Ghost Wolf**: HECHO con `ABPath` directo (snapshot + re-path cada rePathInterval) en
  `GhostWolfController`, NO AIPath/Seeker (evita el ciclo de vida de AIPath sin grafo y lo hace
  testeable). Movimiento por transform (como el ghost del jugador), paso en `Tick(dt)` publico.
  Destino = prey vivo mas cercano; sesgo inicial aimBiasSeconds en la direccion apuntada; fallback
  directo si no hay grafo/ruta; bite (slow) + despawn al primer prey.

HECHO `[x]`: Smell (`SmellAbility` + `SmellTrail` prefab LineRenderer) usa `ABPath.Construct` +
`BlockUntilCalculated` por cada prey (snapshot), fallback linea recta sin grafo. Ghost Wolf como arriba.
Verificado en el Preview Harness (Smell dibuja trail; lobo alcanza y muerde al dummy con slow).

Estado: `[x]`

---

## Orden de implementacion sugerido
De menor a mayor dependencia, para validar la infra antes de los personajes complejos:

1. **Infra base sin la cual nada nuevo funciona**: StatusEffectController (inmunidad/dispel/lethal helpers),
   DamageInfo.Lethal (#5), contador de hits para ults (#2).
2. **Charmer** HECHO `[x]`: Repel (#1 CCImmunity + dispel), Enchant (CharmedEffect + proyectil
   perforante), Assault (#9 dash-a-target + #2). Cierra el flujo "ultimate por carga". Crowmaster
   TeleportSmash flipeado a `usesHitCharge=true`. Verificado en el Preview Harness (4/4 pruebas PASS).
3. **Werewolf** HECHO `[x]`: True Form (#5 lethal + TrueFormEffect, ult por carga), Smell (#10 A*
   trail), Ghost Wolf (#10 minion ABPath). Verificado en el Preview Harness (4/4). Reveal del mordisco
   del lobo pendiente del World Target Pointer (#7, Bloque 5). Harness extendido con training dummies
   + IWorldQueryService minimo + boton "simular golpe" para cargar ults.
4. **Drowned** HECHO `[x]`: Gancho (pull a punto medio via ApplyImpulse + slow), Nadar (#4 camuflaje +
   haste, rompe al actuar), Ejecucion (#9 DashHelper + AreaThenCastAimer + AoE dano + haste si derriba).
   Verificado en el Preview Harness (3/3). El canal de visibilidad por estado (#4) quedo completo.
5. **Preys** HECHO `[x]`: Engineer (Bloque 4), Trapper (Bloque 5), TrickyWizard + Medic (Bloque 6).
   Toda la infra de preys lista: placeables (#3), FOV/Blinded (#6), World Target Pointer (#7),
   AllyTargetAimer (#8), InvisibleEffect (reusa #4). LOS 8 PERSONAJES IMPLEMENTADOS Y VERIFICADOS.
6. **Roster/Meta** (Bloque 7) HECHO `[x]` y verificado en 00_InGame: 8 `MetaCharacter` creados (envuelven
   su CharacterData + skin default del rol) y cargados en `MetaCatalog`; `ProfileService.EnsureSeed`
   desbloquea los 8 (dev). `GameManager.SpawnOne` ahora resuelve loadout: player = personaje EQUIPADO,
   bots = al azar de su bando (`ResolveLoadout`/`PickRandomCharacter`), inyecta via `Character.SetData` y
   spawnea por `ISpawnService`. Composicion 1 hunter / 4 preys (seteable, playerTeam=Prey). Ghost convertido
   a prefab (`PlayerGhost.prefab`) + spawn/despawn por ISpawnService (get-or-add, fallback runtime).
   Verificado: player=engineer (equipado), bots variados, comp 1/4, ghost desde prefab al derribar.
   PENDIENTE arte: skins/sprites/animators por personaje (hoy comparten la default del rol -> se ven iguales).

Cada habilidad nueva: clase `Ability` + `AbilityData` ([CreateAssetMenu]) + asset configurado +
(si aplica) prefab del proyectil/placeable/minion via `ISpawnService`. Verificacion por MCP en play.
