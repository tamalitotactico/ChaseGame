# Character Designs (set 1)

Fuente de verdad de DISENO/BALANCE de los personajes. La tabla de cada personaje describe
la intencion; la implementacion vive en ScriptableObjects (`CharacterData` + `AbilityData`) y
en clases C# (`Ability`, `StatusEffect`). Todo numero aqui es TUNEABLE via inspector: cooldowns,
duraciones, radios, multiplicadores, cupos, que efectos aplica. Cero hardcode en la logica.

Estado por habilidad: `[ ]` sin implementar . `[~]` parcial . `[x]` implementada y verificada.

Mecanicas transversales y sistemas nuevos: ver [SystemsForCharacterSet1.md](SystemsForCharacterSet1.md).

---

## Roster (4 hunters + 4 preys)

Data-driven: NO hay 8 prefabs. Se mantienen 2 prefabs base (Hunter / Prey) con los componentes
compartidos (Character, Motor, Health, AbilityController, Combat, brains). Cada personaje =
1 `CharacterData` (stats + sus ability assets) + 1 skin/visual + 1 entrada `MetaCharacter` en
`MetaCatalog` para la UI de seleccion. Agregar un personaje futuro = crear assets, sin tocar prefabs.

| Bando | Personajes | CharacterData existente |
|---|---|---|
| Hunter | Crowmaster, Charmer, Werewolf, Drowned | Crowmaster = `HunterData.asset` actual (Remnant/Fear/TeleportSmash ya implementadas) |
| Prey | Engineer, Trapper, TrickyWizard, Medic | (`PreyData.asset` actual es placeholder Dash+Projectile, se reemplaza) |

### Modos de juego y composicion
- Dos modos: **jugadores reales** vs **partida con bots**.
- En modo bots, a los bots se les asigna un personaje **al azar** de su bando.
- La cantidad de hunters y preys por partida la definira **el modo de juego** (a futuro). Hoy
  `GameManager.huntersTotal/preysTotal` sigue mandando.
- El jugador local elige su personaje en la UI de seleccion ya existente; se lee de `IProfileService`
  al spawnear. Cambio puntual en `GameManager.SpawnOne`: inyectar el `CharacterData` equipado sobre
  el prefab base en vez del data fijo del prefab.

### Mecanica comun a TODAS las ultimates de hunter (slot 2 / R)
"Availability Condition: Golpea 2 veces a preys". La ultimate NO usa cooldown: usa un **contador de
hits de ataque basico a preys** (cap configurable, default 2). Reglas:
- Solo el **ataque basico** suma al contador (no las habilidades 1/2).
- Al lanzar la ultimate (o al terminar su efecto) el contador vuelve a 0.
- Si la ultimate requiere target y al canalizar **no hay target valido, se cancela y DEVUELVE la carga**.
- Mientras el contador no este lleno, pulsar R no hace nada (UI: "no lista").

Detalle de sistema en [SystemsForCharacterSet1.md](SystemsForCharacterSet1.md) -> "Ultimate por carga de hits".

---

## HUNTERS

### Crowmaster  (= HunterData actual)
| Slot | Habilidad | Descripcion | Params tuneables | Estado |
|---|---|---|---|---|
| 1 | **Remnant** | Deja un senuelo que asusta y revela posicion al cazador (1s). | decoyDuration, activationRadius, effectRadius, fearDuration, slowDuration, slowMultiplier, activeDuration | `[x]` |
| 2 | **Fear** | Proyectil que causa miedo (1s) y ralentizacion. | speed, range, homing, homingTurnRateDeg, fearDuration, slowDuration, slowMultiplier | `[x]` |
| 3 | **Teleport Smash** | Teletransporte con AoE (fear+slow) y self-haste. Cast 1.5s. | castTime, teleportDistance, aoeRadius, aoeFearDuration, aoeSlowDuration, aoeSlowMultiplier, hasteDuration, hasteMultiplier, usesHitCharge, hitsRequired | `[x]` gating por carga de hits activado (`usesHitCharge=true`, hitsRequired=2) |

### Charmer
| Slot | Habilidad | Descripcion | Params tuneables | Estado |
|---|---|---|---|---|
| 1 | **Repel** | Dispel fuerte de efectos negativos de control (deja buffs intactos) + inmunidad a CC (2s) + haste (2s). Instant. | dispelControlEffects, immunityDuration, hasteDuration, hasteMultiplier | `[x]` |
| 2 | **Enchant** | Proyectil que **perfora** y aplica Charm (jale progresivo, fear invertido) hacia la posicion del Charmer al impacto. 1s. Sin maximo de objetivos (`maxTargets`, 0 = ilimitado). | speed, range, maxTargets, charmDuration, charmPullStrength | `[x]` |
| 3 | **Assault** (ult) | Cast 0.5s, luego salto rapido al **enemigo en vision mas cercano** y le aplica Fear (1s). Si no hay enemigo en vision: cancela y devuelve carga. | usesHitCharge, hitsRequired, castTime, visionRange, wallLayer, wallPadding, fearDuration | `[x]` |

### Werewolf
| Slot | Habilidad | Descripcion | Params tuneables | Estado |
|---|---|---|---|---|
| 1 | **Ghost Wolf** | Invoca un lobo autonomo (IA de navegacion A* "tonta"). Direccion inicial = donde apunto el hunter (avanza recto aimBiasSeconds, luego persigue con A*); puede desviarse ante muros. Vida max 15s. Al primer prey que alcanza: **muerde** (slow, SIN dano) y **desaparece** (1 mordisco). No controlable. Reveal del mordido pendiente del World Target Pointer (Bloque 5). | maxLifetime, moveSpeed, aimBiasSeconds, rePathInterval, biteRadius, biteSlowDuration, biteSlowMultiplier, revealDuration | `[x]` (reveal `[~]` pend. pointer) |
| 2 | **Smell** | Al lanzar, calcula con A* la **ruta optima a cada prey** y la dibuja como trail (revela el camino). Snapshot al cast: NO se recalcula. Sin grafo A* cae a linea recta. Dura 5s. | revealDuration, trailWidth, trailColor | `[x]` |
| 3 | **True Form** (ult) | Transformacion 4s: gran velocidad (haste) + ataque basico **letal** (derriba al prey en 1 golpe, via canal GrantsLethalAttack/DamageInfo.Lethal). | usesHitCharge, hitsRequired, duration, hasteMultiplier, tintColor (lethalMode=DownInOne; DamageMultiplier futuro) | `[x]` |

Nota True Form: el "instakill" NO es dano x2 literal (eso queda atado a que el prey tenga 2 HP).
Se modela como flag de letalidad en `DamageInfo` (el golpe derriba sin importar el HP). Ver
[SystemsForCharacterSet1.md](SystemsForCharacterSet1.md) -> "Letalidad / ejecucion".

### Drowned
| Slot | Habilidad | Descripcion | Params tuneables | Estado |
|---|---|---|---|---|
| 1 | **Gancho** | Atrae al enemigo al **punto medio** entre cazador y afectado (posiciones al momento de impacto; solo se mueve el prey, via Motor.ApplyImpulse) + Slow. | speed, range, pullDuration, slowDuration, slowMultiplier | `[x]` |
| 2 | **Nadar** | Camuflaje (fade por distancia, invisible para enemigos lejanos) + velocidad extra. **Atacar o usar habilidad lo rompe** (BreakActionSensitiveEffects). | duration, hasteMultiplier, revealRadius | `[x]` |
| 3 | **Ejecucion** (ult) | Cast 0.6s (AreaThenCastAimer), dash a la **posicion apuntada** (DashHelper, respeta muros) infligiendo 1 golpe de dano en aoeRadius. Si derriba a alguien: +haste. | usesHitCharge, hitsRequired, castTime, aimRange, aoeRadius, damage, targetLayers, wallLayer, downHasteDuration, downHasteMultiplier | `[x]` |

---

## PREYS

### Engineer
| Slot | Habilidad | Descripcion | Params tuneables | Estado |
|---|---|---|---|---|
| 1 | **Raise Wall** | Barra que **relentiza** al hunter que la cruza (no bloquea, sin collider; preys pasan). Largo auto-ajustado por raycast a muros (de pared a pared, tope maxLength). Cupo **2** (el mas antiguo se borra). Vida infinita hasta que un hunter la toca -> hitLifetime. | maxLength, slowAreaWidth, slowMultiplier, slowDuration, maxWalls (2), hitLifetime (5s), wallLayer, hunterLayer | `[x]` |
| 2 | **Beacon** | Baliza en su posicion (instant) con aura: aliados dentro ganan haste; al salir/expirar el boost dura exitBoostDuration mas. El hunter la rompe de 1 golpe (ignora dano aliado). Cupo 1. | duration (5s), hasteMultiplier (1.5), areaRadius, exitBoostDuration (1s), beaconHealth (1), maxBeacons (1) | `[x]` |

### Trapper
| Slot | Habilidad | Descripcion | Params tuneables | Estado |
|---|---|---|---|---|
| 1 | **Smoke Trap** | Trampa que solo dispara un hunter (visible). Al dispararse, durante areaDuration aplica BlindedEffect (reduce vision via VisionSource) a hunters en el area. Cupo 2. | triggerRadius, areaRadius, areaDuration (3s), fovMultiplier, blindRefresh, maxTraps (2) | `[x]` |
| 2 | **Bear Trap** | Trampa (cupo 3) que al primer hunter lo aturde 1s + revela su posicion a los preys 1s (World Target Pointer), y desaparece. | triggerRadius, stunDuration (1s), revealDuration (1s), pointer (sprite/size/color), maxTraps (3) | `[x]` |

### TrickyWizard
| Slot | Habilidad | Descripcion | Params tuneables | Estado |
|---|---|---|---|---|
| 1 | **Tricky Lure** | Clon recto (imita el sprite del prey, 1 HP, no bloquea -trigger-, bait). Muere de 1 golpe soltando risa. El prey se vuelve **invisible** (total) 1s. | cloneSpeed, cloneLifetime (3s), cloneHealth (1), laughSfx, invisDuration (1s) | `[x]` |
| 2 | **Disparo cegador** | Proyectil que al impactar aplica BlindedEffect (reduce FOV 2s) + SlowedEffect (1s). | speed, range, fovMultiplier, fovDuration (2s), slowDuration (1s), slowMultiplier | `[x]` |

### Medic
| Slot | Habilidad | Descripcion | Params tuneables | Estado |
|---|---|---|---|---|
| 1 | **Booster Pills** | Apuntable a un aliado (AllyTargetAimer): +haste y recorta cdReductionPct% del cooldown restante de TODAS sus habilidades (instantaneo). Sin target -> a si mismo. | hasteDuration (1s), hasteMultiplier, cdReductionPct (20), aimRange | `[x]` |
| 2 | **Sanacion** | Cast 2s (AimThenCast), AoE: cura healAmount al aliado NO-full mas cercano del area. Si no hay aliado curable -> a si mismo; si tambien esta full, se pierde. | castTime (2s), aoeRadius, healAmount (1) | `[x]` |

Nota vida: `CharacterHealth` ya tiene MaxHealth/CurrentHealth (enteros). "Medio corazon a entero" es
visual (HeartIndicator soporta medios); el heal es `Heal(1)` entero. No se necesita vida fraccionaria.
