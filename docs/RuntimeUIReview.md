# Runtime-built UI — review list

UI construida en runtime (via `new GameObject`/`AddComponent`, sin prefab). Por politica
(ver CLAUDE.md "UI construction policy") cada una es candidata a convertir a prefab/serializado
para que un diseñador/artista pueda tunearla en el inspector. Token greppable: `RUNTIME-UI-REVIEW`.

Estado: `[ ]` pendiente de revisar · `[~]` tunables ya expuestos via config serializada · `[x]` convertido a prefab.

## In-game
- `[~]` `GhostBodyPointer` ([UI/GhostBodyPointer.cs]) — puntero al cuerpo en modo fantasma. Sprite/tamaño/colores
  ya editables via `GhostModeController` (Main Camera). Pendiente: convertir el fantasma entero a prefab
  (tambien lo exige `Runner.Spawn` de Fusion).
- `[~]` `WorldTargetPointer` ([UI/WorldTargetPointer.cs]) — puntero reusable a un objetivo del mundo
  (generaliza GhostBodyPointer). Lo usa Bear Trap (revela hunter). Sprite/tamaño/color/duracion vienen de
  la AbilityData del llamador (BearTrapAbilityData), serializados. Pendiente: convertir a prefab.
- `[~]` `StatusIconDisplay` ([UI/StatusIconDisplay.cs]) — iconos de status effects sobre el personaje.
- `[~]` `MatchAnnouncementUI` ([UI/MatchAnnouncementUI.cs]) — banner global de anuncios (screen-space overlay,
  no lo afecta el post-proceso). Hoy cubre el evento downed ("X fue derribado") + cue global. Vive en el GO
  `AnnouncementSystem` de `00_InGame`; tunables (cue, formato, color, tiempos, fontSize, posicion, sortingOrder)
  serializados en el componente. Pendiente: convertir el banner a prefab si se quiere arte custom (fondo, slide-in).
- `[ ]` `RuntimeDiagnosticsPanel` ([Diagnostics/RuntimeDiagnosticsPanel.cs]) — panel de debug (no shippable; ok hardcode).
- `[x]` UI de emotes in-game — hecha SEGUN la politica: la rueda (`EmoteWheelHUD`) esta autorizada en
  `00_InGame` (boton + 3 slots serializados) y la burbuja es prefab (`EmoteBubble.prefab`, globo de
  dialogo editable). El `EmoteBubblePresenter` (escena) resuelve la matriz de visibilidad. Sin deuda runtime-UI.

> Nota: el HUD in-game (AbilityPanel, hearts) NO es runtime-built: el HUD esta autorizado en `00_InGame`
> y los corazones (`HeartIndicator`) viven en el prefab Prey -> ya editables en inspector.

## Meta (todo runtime-built; layout/colores/tamaños hardcodeados en C#)
- `[ ]` `CharacterCard` ([Meta/UI/CharacterCard.cs])
- `[ ]` `EmotesView` ([Meta/UI/EmotesView.cs])
- `[ ]` `GameModeScreen` (celdas de modo) ([Meta/Screens/GameModeScreen.cs])
- `[ ]` `CharacterDetailScreen` (slots de habilidad/skin) ([Meta/Screens/CharacterDetailScreen.cs])
- `[ ]` `HubScreen` (layout) ([Meta/Screens/HubScreen.cs])
- `[ ]` `SplashVideoView` ([Meta/UI/SplashVideoView.cs])
