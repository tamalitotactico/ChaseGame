# Prompt para siguiente instancia — Cambios pendientes en Unity Editor

## Contexto del proyecto

Juego Unity 6 top-down 2D asimetrico (cazador vs survivors). Escena principal: `02_Game.unity`.
Motor: Unity 6000.0.60f1, URP, A* Pathfinding Project instalado (`ASTAR_EXISTS` definido).

Se acaba de completar un refactor grande de scripts. Todos los `.cs` estan correctos y compilan.
Lo que falta es configurar prefabs y escena en el Unity Editor usando las herramientas MCP.

---

## Instrucciones para el agente

Usa las herramientas MCP de Unity disponibles para ejecutar cada paso.
Verifica la consola de Unity antes y despues de cada cambio para confirmar que no hay errores.
El orden importa: primero los prefabs, luego la escena.

---

## Paso 1 — Actualizar prefab del jugador Cazador

**Prefab actual:** busca en `Assets/_Game/Prefabs/` el prefab del jugador (probablemente llamado `Player`).

Cambios a realizar:
1. Eliminar el componente `PlayerController` del prefab.
2. Agregar el componente `HumanPursuer`.
3. Agregar el componente `SpeedBoostAbility` (cooldown: 8).
4. Agregar el componente `DashAbility` (cooldown: 5).
5. Agregar el componente `TeleportAbility` (cooldown: 12, teleportDistance: 6, wallMaxThickness: 3, exitOffset: 0.3).
6. En el componente `HumanPursuer`, asignar:
   - `Abilities[0]` → el componente `SpeedBoostAbility` del mismo GameObject
   - `Abilities[1]` → el componente `DashAbility` del mismo GameObject
   - `Abilities[2]` → el componente `TeleportAbility` del mismo GameObject
7. Guardar este prefab como `Player_Pursuer` (o renombrarlo si ya se llama `Player`).
8. El componente `PursuerAttack` debe permanecer en el prefab.

---

## Paso 2 — Crear prefab del jugador Survivor

1. Duplicar el prefab `Player_Pursuer` recien configurado.
2. En la copia:
   - Eliminar `HumanPursuer`.
   - Eliminar `SpeedBoostAbility`, `DashAbility`, `TeleportAbility`.
   - Eliminar `PursuerAttack`.
   - Agregar `HumanSurvivor`.
   - Agregar `SurvivorSkill1Ability`.
   - Agregar `SurvivorSkill2Ability`.
   - En `HumanSurvivor`, asignar:
     - `Abilities[0]` → `SurvivorSkill1Ability`
     - `Abilities[1]` → `SurvivorSkill2Ability`
3. Guardar como `Player_Survivor`.

---

## Paso 3 — Actualizar prefab PursuerBot

Prefab: `Assets/_Game/Prefabs/PursuerBot` (o nombre similar).

1. Agregar componente `BotLocomotion`.
2. Configurar en `BotLocomotion`:
   - `Move Speed`: 4
   - `Detection Radius`: 6
   - `Wall Layer`: capa "Wall"
3. Verificar que `AIPath` (A*) sigue presente.
4. Verificar que el componente `PursuerBot` esta presente y el campo `Attack Range` = 1.5, `Attack Cooldown` = 1.5.

---

## Paso 4 — Actualizar prefab SurvivorBot

Prefab: `Assets/_Game/Prefabs/SurvivorBot` (o nombre similar).

1. Agregar componente `BotLocomotion`.
2. Configurar en `BotLocomotion`:
   - `Move Speed`: 4
   - `Detection Radius`: 6
   - `Wall Layer`: capa "Wall"
3. Verificar que `AIPath` (A*) sigue presente.
4. En el componente `SurvivorBot`, si hay un campo `FloatingHealthBar`, asegurarse de que apunta a la barra del prefab.

---

## Paso 5 — FloatingHealthBar en prefabs

En cualquier prefab que tenga el componente `FloatingHealthBar`:
1. Asignar el campo `Bar Root` al GameObject raiz del canvas de la barra de vida (el contenedor padre del fill y el fondo).
2. Asegurarse de que ese `Bar Root` esta **desactivado** por defecto en el prefab.

---

## Paso 6 — Configurar escena 02_Game.unity

### 6a. GameManager

El componente `GameManager` en la escena tiene dos nuevos campos:
- `Player Pursuer Prefab` → asignar `Player_Pursuer`
- `Player Survivor Prefab` → asignar `Player_Survivor`

El campo antiguo `Player Prefab` ya no existe; si aparece como "Missing", ignorarlo.

### 6b. Main Camera — CameraFollow

1. Seleccionar `Main Camera` en la jerarquia.
2. Agregar componente `CameraFollow`.
3. Configurar:
   - `Smooth Speed`: 5
   - `Orthographic Size`: 4

### 6c. Canvas — Panel de habilidades (AbilityHUDController)

En el Canvas del HUD (el que ya tiene HUDPanel, TimerPanel, etc.), crear la siguiente jerarquia:

```
AbilityPanel  [nuevo GameObject]
  Slot_Q
    Icon        (Image)
    CooldownFill (Image — Image Type: Filled, Fill Method: Radial360, Fill Origin: Top)
    KeyLabel    (TextMeshProUGUI, texto: "Q")
  Slot_E
    Icon        (Image)
    CooldownFill (Image — Image Type: Filled, Fill Method: Radial360, Fill Origin: Top)
    KeyLabel    (TextMeshProUGUI, texto: "E")
  Slot_R
    Icon        (Image)
    CooldownFill (Image — Image Type: Filled, Fill Method: Radial360, Fill Origin: Top)
    KeyLabel    (TextMeshProUGUI, texto: "R")
```

En `AbilityPanel`:
1. Agregar componente `AbilityHUDController`.
2. Asignar el array `Slots` con 3 entradas:
   - Slot 0: Root=Slot_Q, CooldownFill=CooldownFill de Slot_Q, KeyLabel=KeyLabel de Slot_Q
   - Slot 1: Root=Slot_E, CooldownFill=CooldownFill de Slot_E, KeyLabel=KeyLabel de Slot_E
   - Slot 2: Root=Slot_R, CooldownFill=CooldownFill de Slot_R, KeyLabel=KeyLabel de Slot_R
3. En cada boton de los slots, configurar `OnClick`:
   - Slot_Q Button → `AbilityHUDController.OnSlotButtonPressed(0)`
   - Slot_E Button → `AbilityHUDController.OnSlotButtonPressed(1)`
   - Slot_R Button → `AbilityHUDController.OnSlotButtonPressed(2)`

---

## Paso 7 — Verificacion final

1. Abrir `02_Game.unity` y pulsar Play.
2. Seleccionar rol Cazador.
3. Verificar en consola que no hay errores de NullReference ni warnings de componentes faltantes.
4. Verificar que aparecen 3 botones de habilidad en pantalla (Q/E/R).
5. Pulsar Q → el jugador debe moverse mas rapido durante 1 segundo.
6. Pulsar E → debe ocurrir un dash corto.
7. Pulsar R frente a una pared → el jugador debe aparecer al otro lado.
8. Seleccionar rol Survivor → deben aparecer 2 botones (Q/E) y activarse con flash de color.
9. Verificar que los bots se mueven y atacan correctamente.
10. Verificar que la camara sigue al jugador.

---

## Archivos de referencia

- `ARCHITECTURE.md` — documentacion completa de todas las clases y sus campos
- `CONTEXT.md` — estado del proyecto y decisiones tecnicas
- `Assets/_Game/Scripts/` — todos los scripts C# del juego
