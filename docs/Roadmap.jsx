import { useState } from "react";

// Roadmap por HITOS (sin tiempos). status: "done" | "active" | "pending"
const phases = [
  {
    id: 0,
    label: "FASE 0",
    title: "Baseline",
    status: "done",
    color: "#00E09A",
    goal: "Lo que ya existe y funciona (estado actual del codigo, post-auditoria)",
    milestone: "✓ Phase 2 tecnica + hardening de auditoria completados",
    tasks: [
      {
        category: "Ya implementado",
        items: [
          "Movimiento RB2D + combate basico del hunter (hit/herido)",
          "Framework de habilidades data-driven + 5 habilidades concretas",
          "AI de bots (FSM + pathfinding A*) con percepcion y reglas",
          "Downed + revive por proximidad, status effects (stun/slow/fear/haste)",
          "Fog of War (mesh + RenderTexture), match flow, win conditions",
          "Infra: EventBus, ServiceLocator, Audio/Music managers, VFXSpawner",
          "Hardening auditoria: GC hot paths, leaks, IWorldQueryService, API visual OCP",
        ],
      },
      {
        category: "Pendiente arrastrado",
        items: [
          "Object Pooling de proyectiles (pospuesto, tanda dedicada)",
        ],
      },
    ],
  },
  {
    id: 1,
    label: "FASE 1",
    title: "Consolidar el Game Loop",
    status: "active",
    color: "#FF4D00",
    goal: "Partida completa rejugable sin reiniciar Unity ni la app, con modo fantasma",
    milestone: "✓ N partidas seguidas (rematch y via lobby) sin reinicios ni fugas de estado",
    tasks: [
      {
        category: "Modo Fantasma (downed)",
        items: [
          "Cuerpo = ancla revivible; jugador downed controla un fantasma (no muerte real)",
          "Fantasma: roam libre + utilidad ligera = 1 ping de posicion a los vivos",
          "Puntero UI hacia el cuerpo que notifica cuando un aliado te esta reviviendo",
          "Revive del cuerpo -> el fantasma regresa al cuerpo, gameplay normal de prey",
          "Regla de fin: hunters ganan si TODAS las preys estan downed a la vez",
          "Interaccion del fantasma con el mapa = futuro (no en este alcance)",
        ],
      },
      {
        category: "Loop de rejugar",
        items: [
          "EndingState: boton de rematch (reset in-place) + volver al lobby",
          "Reset limpio: despawn, EventBus.Clear selectivo, reinicio de MatchStateMachine",
          "Sin estado residual (timers, suscripciones, objetos pooled)",
          "LobbyState deja de estar vacio: punto de retorno real del loop",
        ],
      },
      {
        category: "Seleccion de rol/personaje (local)",
        items: [
          "Elegir hunter o prey antes de la partida (hoy es campo del inspector)",
          "Seleccion de personaje (lee CharacterData) wired al spawn del player",
          "Composicion de bots configurable desde el flujo, no solo inspector",
        ],
      },
    ],
  },
  {
    id: 2,
    label: "FASE 2",
    title: "UI / UX / Meta-layer (landscape)",
    status: "done",
    color: "#FFAA00",
    goal: "Meta-juego front-end en landscape (Hub/Customize/Detalle/Gamemode) + loadout persistente, economia display-only",
    milestone: "✓ Meta -> equipar (persiste) -> JUGAR (rol aleatorio) -> resultado -> Revancha/Salir, en landscape. Verificado en Play (MCP)",
    tasks: [
      {
        category: "Arquitectura meta (DONE)",
        items: [
          "AppRoot (DontDestroyOnLoad) registra IProfileService; loadout sobrevive Meta<->Gameplay",
          "Datos SO: MetaCharacter/Skin/EmoteData/GameModeData/MetaCatalog; ProfileState JSON en persistentDataPath",
          "Screen system desacoplado: IScreenService/ScreenService (back-stack) + ScreenController",
          "UI habla solo via EventBus + ServiceLocator, nunca screen-a-screen",
          "2 escenas: 00_Meta (boot, idx0) + Gameplay (idx1)",
        ],
      },
      {
        category: "Pantallas meta (DONE)",
        items: [
          "Hub: avatar equipado + toggle Hunter/Prey, monedas display, SHOP/CUSTOMIZE/CHEST, modo, JUGAR",
          "Customize: sidebar Hunters/Preys/Emotes, grid owned/locked (silueta), sort rareza + favoritos, rueda de 3 emotes",
          "Detalle: splash, rareza, habilidades (CharacterData.abilities), skin + preview, carrusel, READY=equipar",
          "Select Gamemode: 'Modo del dia' rotativo diario + secundarios; solo Supervivencia funcional",
          "Shop / Chest: stubs (entrar/salir), contenido real en Phase 7",
        ],
      },
      {
        category: "Cableado de partida (DONE)",
        items: [
          "Rol AL AZAR al entrar desde el Meta; GameManager lee IProfileService y aplica la Skin equipada",
          "Swap de RuntimeAnimatorController en 'Visual' + CharacterAnimator.RebuildStateTable",
          "Resultados: Revancha (reset in-place) / Salir (LoadScene 00_Meta)",
          "Fix: removido SceneController DontDestroyOnLoad que filtraba GameManager al volver al Meta",
        ],
      },
      {
        category: "Pendiente Phase 2 (pulido)",
        items: [
          "HUD de partida en landscape (joystick/habilidades/timer/corazones) - hoy placeholder",
          "UI del fantasma: puntero al cuerpo, ping, prompt y barra de revive",
          "Settings/pausa, sensibilidad de joystick, transiciones/juice",
          "Arte real de tarjetas/iconos/splashes = Phase 6 (hoy placeholders; splash de prueba via SplashartTest.mp4)",
        ],
      },
    ],
  },
  {
    id: 3,
    label: "FASE 3",
    title: "Audio & Game Feel",
    status: "pending",
    color: "#FFD400",
    goal: "La partida se siente y suena completa",
    milestone: "✓ Ninguna accion clave queda sin audio ni feedback",
    tasks: [
      {
        category: "Audio",
        items: [
          "SFX: ataques, hits, habilidades (cast/impacto), revive, fantasma/ping, UI",
          "Estados de musica: menu / partida / ending (crossfade ya existe)",
          "Crear/asignar AudioCue faltantes; mixdown por buses",
        ],
      },
      {
        category: "Game feel",
        items: [
          "Hit-stop, screen shake, cobertura de VFX (VFXSpawner)",
          "Haptics en movil",
          "Camara: follow pulido, framing portrait, zoom contextual",
        ],
      },
    ],
  },
  {
    id: 4,
    label: "FASE 4",
    title: "Contenido & Balance",
    status: "pending",
    color: "#00C2FF",
    goal: "Roster y mapas suficientes para rejugabilidad",
    milestone: "✓ 2 hunters + 4 preys, 2 mapas, 2 modos, balance validado vs bots",
    tasks: [
      {
        category: "Personajes",
        items: [
          "2 hunters con filosofia de caza diferenciada",
          "4 preys (evasion, area, soporte, otro perfil)",
          "Stats/cooldowns por CharacterData; reglas de bot por BotTuningData",
        ],
      },
      {
        category: "Mapas",
        items: [
          "2 layouts con elementos interactivos/destructibles",
          "Sorting por eje Y, zonas de riesgo y atajos",
        ],
      },
      {
        category: "Modos de juego",
        items: [
          "Supervivencia (sobrevivir el tiempo)",
          "Recoleccion (objetivo de items) - formalizar abstraccion de modo",
          "Aqui entra la interaccion del fantasma con el mapa (mejora futura)",
        ],
      },
      {
        category: "Balance",
        items: [
          "Velocidades hunter vs prey, cooldowns, frecuencia de revival, duracion",
        ],
      },
    ],
  },
  {
    id: 5,
    label: "FASE 5",
    title: "Networking MVP",
    status: "pending",
    color: "#5B8CFF",
    goal: "Multijugador real sobre la base local estable (stack/autoridad por decidir)",
    milestone: "✓ 4 personas en partida estable con paridad respecto al modo local",
    tasks: [
      {
        category: "Capa de red sobre servicios sembrados",
        items: [
          "Implementaciones de red de ISpawnService / IAuthorityContext / IWorldQueryService",
          "Sincronizar posicion, habilidades+efectos, golpes/estados",
          "Sincronizar fantasma+revive, timer de partida, items de modo",
          "DECISION ABIERTA: Photon Fusion 2 vs alternativa; host vs shared",
        ],
      },
      {
        category: "Matchmaking",
        items: [
          "Sala privada con codigo + sala publica (matchmaking)",
          "Seleccion de personaje en lobby, asignacion de rol (1 hunter, resto preys)",
          "Reconexion basica; caida del hunter (cancelar o reemplazar por bot)",
        ],
      },
      {
        category: "UI de red",
        items: [
          "Pantalla de busqueda, indicador de ping/conexion, salas (reusa lobby Fase 2)",
        ],
      },
    ],
  },
  {
    id: 6,
    label: "FASE 6",
    title: "Arte definitivo",
    status: "pending",
    color: "#00E09A",
    goal: "Arte final integrado, coherente, en landscape (inicio bloque comercial)",
    milestone: "✓ Build sin placeholders en pantallas ni personajes principales",
    tasks: [
      {
        category: "Arte",
        items: [
          "Definir estilo visual final (pixel art 3/4 candidato)",
          "Sprites con animaciones (idle/run/hit/defeat/ghost)",
          "Tilesets de mapas, UI final, particulas",
          "Icono de la app (prioritario para stores)",
        ],
      },
    ],
  },
  {
    id: 7,
    label: "FASE 7",
    title: "Monetizacion & Analytics",
    status: "pending",
    color: "#C84FFF",
    goal: "Economia y medicion listas",
    milestone: "✓ Moneda + tienda + ads opcionales + analytics funcionando en build",
    tasks: [
      {
        category: "Economia",
        items: [
          "Moneda comun (recompensa por partida) + premium (Unity IAP)",
          "Tienda de personajes (ambas monedas) y skins (premium)",
          "Balance earn-rate vs costo",
        ],
      },
      {
        category: "Ads & Analytics",
        items: [
          "Rewarded ads opcionales (AdMob/ironSource), sin interstitials obligatorios",
          "Firebase Analytics: eventos, retencion D1/D7/D30, conversion, funnel",
        ],
      },
    ],
  },
  {
    id: 8,
    label: "FASE 8",
    title: "Launch",
    status: "pending",
    color: "#FF4DA6",
    goal: "Publicacion en stores con comunidad activa",
    milestone: "✓ Soft launch validado + publicacion en App Store y Google Play",
    tasks: [
      {
        category: "Pre / Stores / Post",
        items: [
          "Soft launch regional -> fix de criticos -> ASO",
          "Apple Developer + Google Play Console; builds iOS (TestFlight) y Android",
          "Cumplimiento de politicas de monetizacion; comunidad + devlog",
          "Post-launch: monitor de crashes, reviews, cadencia de contenido, CCU",
        ],
      },
    ],
  },
];

const risks = [
  { label: "Networking mas complejo de lo esperado", mitigation: "Arquitectura ya desacoplada (servicios). Prototipo de red en paralelo a Fase 4.", severity: "alta" },
  { label: "El modo fantasma rompe balance o claridad", mitigation: "Empezar minimo (roam + 1 ping). Validar en playtest antes de ampliar.", severity: "media" },
  { label: "Loop de rejugar deja estado residual", mitigation: "Reset explicito y verificable; test de N partidas seguidas.", severity: "media" },
  { label: "Arte tarda o no cuadra con el estilo", mitigation: "Definir estilo y contactar artista durante Fase 4, no esperar a Fase 6.", severity: "alta" },
  { label: "Landscape y framing del mapa top-down", mitigation: "Validar framing/zoom al hacer el HUD landscape (pendiente WS 2.4).", severity: "media" },
  { label: "Cold start: nadie en matchmaking", mitigation: "Sala privada por codigo desde dia 1 + soft launch regional concentrado.", severity: "media" },
  { label: "Apple rechaza por monetizacion", mitigation: "Revisar guidelines antes de implementar IAP/ads.", severity: "baja" },
];

const STATUS_LABEL = { done: "COMPLETADO", active: "EN CURSO", pending: "PENDIENTE" };

export default function Roadmap() {
  const [activePhase, setActivePhase] = useState(1);
  const [expandedTask, setExpandedTask] = useState(null);
  const showRisks = activePhase === -1;
  const phase = phases[activePhase] ?? phases[0];

  const doneCount = phases.filter((p) => p.status === "done").length;

  return (
    <div style={{
      minHeight: "100vh",
      background: "#0A0A0F",
      fontFamily: "'DM Mono', 'Courier New', monospace",
      color: "#E8E8E8",
    }}>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Mono:wght@300;400;500&family=Syne:wght@700;800&display=swap');
        * { box-sizing: border-box; margin: 0; padding: 0; }
        .phase-btn { transition: all 0.2s ease; cursor: pointer; border: none; background: none; }
        .phase-btn:hover { opacity: 0.85; transform: translateY(-2px); }
        .task-card { transition: all 0.2s ease; cursor: pointer; }
        .task-card:hover { transform: translateX(4px); }
        .risk-row { transition: all 0.15s ease; }
        .risk-row:hover { background: rgba(255,255,255,0.03); }
        ::-webkit-scrollbar { width: 4px; height: 4px; }
        ::-webkit-scrollbar-track { background: #0A0A0F; }
        ::-webkit-scrollbar-thumb { background: #333; border-radius: 2px; }
        @keyframes fadeIn { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
        .fade-in { animation: fadeIn 0.3s ease forwards; }
      `}</style>

      {/* Header */}
      <div style={{
        borderBottom: "1px solid #1E1E2E", padding: "28px 32px 24px",
        display: "flex", alignItems: "flex-end", justifyContent: "space-between",
        flexWrap: "wrap", gap: "12px",
      }}>
        <div>
          <div style={{ fontSize: "10px", letterSpacing: "4px", color: "#555", marginBottom: "6px", textTransform: "uppercase" }}>
            GAME DEV ROADMAP · POR HITOS
          </div>
          <h1 style={{ fontFamily: "'Syne', sans-serif", fontSize: "clamp(22px, 5vw, 36px)", fontWeight: 800, color: "#FFF", letterSpacing: "-1px", lineHeight: 1 }}>
            CHASE GAME <span style={{ color: "#FF4D00" }}>_</span>
          </h1>
        </div>
        <div style={{ textAlign: "right" }}>
          <div style={{ fontSize: "11px", color: "#444", marginBottom: "2px" }}>PROGRESO</div>
          <div style={{ fontSize: "13px", color: "#888" }}>{doneCount} de {phases.length} fases · sin fechas</div>
        </div>
      </div>

      {/* Phase selector */}
      <div style={{ display: "flex", gap: "8px", padding: "20px 32px", overflowX: "auto", borderBottom: "1px solid #1E1E2E" }}>
        {phases.map((p, i) => (
          <button key={p.id} className="phase-btn"
            onClick={() => { setActivePhase(i); setExpandedTask(null); }}
            style={{
              padding: "10px 16px", borderRadius: "4px", fontSize: "11px", letterSpacing: "2px",
              fontFamily: "'DM Mono', monospace", fontWeight: activePhase === i ? "500" : "300",
              background: activePhase === i ? p.color : "transparent",
              color: activePhase === i ? "#000" : (p.status === "done" ? "#00E09A" : "#555"),
              border: `1px solid ${activePhase === i ? p.color : (p.status === "done" ? "#00E09A44" : "#222")}`,
              whiteSpace: "nowrap",
            }}>
            {p.label}{p.status === "done" ? " ✓" : ""}
          </button>
        ))}
        <button className="phase-btn" onClick={() => setActivePhase(-1)}
          style={{
            padding: "10px 16px", borderRadius: "4px", fontSize: "11px", letterSpacing: "2px",
            fontFamily: "'DM Mono', monospace", fontWeight: showRisks ? "500" : "300",
            background: showRisks ? "#333" : "transparent", color: showRisks ? "#FFF" : "#555",
            border: `1px solid ${showRisks ? "#555" : "#222"}`, whiteSpace: "nowrap",
          }}>
          RIESGOS
        </button>
      </div>

      {/* Content */}
      <div style={{ padding: "28px 32px", maxWidth: "900px" }}>
        {!showRisks ? (
          <div className="fade-in" key={activePhase}>
            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: "8px", flexWrap: "wrap", gap: "8px" }}>
              <div>
                <div style={{ fontSize: "10px", letterSpacing: "3px", color: phase.color, marginBottom: "4px" }}>
                  {STATUS_LABEL[phase.status]}
                </div>
                <h2 style={{ fontFamily: "'Syne', sans-serif", fontSize: "clamp(20px, 4vw, 28px)", fontWeight: 800, color: "#FFF", letterSpacing: "-0.5px" }}>
                  {phase.title}
                </h2>
              </div>
              <div style={{ background: "#111", border: `1px solid ${phase.color}33`, borderRadius: "4px", padding: "8px 14px", maxWidth: "300px" }}>
                <div style={{ fontSize: "9px", color: "#444", letterSpacing: "2px", marginBottom: "3px" }}>OBJETIVO</div>
                <div style={{ fontSize: "11px", color: "#AAA", lineHeight: 1.5 }}>{phase.goal}</div>
              </div>
            </div>

            <div style={{
              display: "inline-flex", alignItems: "center", gap: "8px",
              background: `${phase.color}11`, border: `1px solid ${phase.color}44`, borderRadius: "3px",
              padding: "6px 12px", marginBottom: "28px", fontSize: "11px", color: phase.color,
            }}>
              {phase.milestone}
            </div>

            <div style={{ display: "flex", flexDirection: "column", gap: "4px" }}>
              {phase.tasks.map((cat, ci) => (
                <div key={ci}>
                  <div className="task-card" onClick={() => setExpandedTask(expandedTask === ci ? null : ci)}
                    style={{
                      display: "flex", alignItems: "center", justifyContent: "space-between", padding: "14px 16px",
                      background: expandedTask === ci ? "#111" : "transparent",
                      border: `1px solid ${expandedTask === ci ? phase.color + "44" : "#1A1A1A"}`,
                      borderRadius: "4px", marginBottom: "2px",
                    }}>
                    <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
                      <div style={{ width: "6px", height: "6px", borderRadius: "50%", background: phase.color, flexShrink: 0 }} />
                      <span style={{ fontSize: "12px", letterSpacing: "1px", color: expandedTask === ci ? "#FFF" : "#888", fontWeight: expandedTask === ci ? "500" : "300" }}>
                        {cat.category.toUpperCase()}
                      </span>
                    </div>
                    <div style={{ display: "flex", alignItems: "center", gap: "10px" }}>
                      <span style={{ fontSize: "10px", color: "#444" }}>{cat.items.length} tareas</span>
                      <span style={{ color: "#444", fontSize: "14px" }}>{expandedTask === ci ? "−" : "+"}</span>
                    </div>
                  </div>
                  {expandedTask === ci && (
                    <div style={{ background: "#0D0D12", border: `1px solid ${phase.color}22`, borderTop: "none", borderRadius: "0 0 4px 4px", padding: "4px 0 8px", marginBottom: "2px" }}>
                      {cat.items.map((item, ii) => (
                        <div key={ii} style={{ display: "flex", alignItems: "flex-start", gap: "12px", padding: "8px 16px 8px 32px", borderBottom: ii < cat.items.length - 1 ? "1px solid #111" : "none" }}>
                          <div style={{ width: "3px", height: "3px", borderRadius: "50%", background: "#444", marginTop: "6px", flexShrink: 0 }} />
                          <span style={{ fontSize: "11px", color: "#666", lineHeight: 1.6 }}>{item}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>

            <div style={{ marginTop: "24px", padding: "12px 16px", background: "#0D0D12", border: "1px solid #1A1A1A", borderRadius: "4px", display: "flex", justifyContent: "space-between", flexWrap: "wrap", gap: "8px" }}>
              <span style={{ fontSize: "10px", color: "#444", letterSpacing: "2px" }}>TOTAL TAREAS ESTA FASE</span>
              <span style={{ fontSize: "13px", color: phase.color }}>
                {phase.tasks.reduce((acc, cat) => acc + cat.items.length, 0)} items
              </span>
            </div>
          </div>
        ) : (
          <div className="fade-in">
            <div style={{ marginBottom: "28px" }}>
              <div style={{ fontSize: "10px", letterSpacing: "3px", color: "#555", marginBottom: "4px" }}>GESTION DE RIESGO</div>
              <h2 style={{ fontFamily: "'Syne', sans-serif", fontSize: "clamp(20px, 4vw, 28px)", fontWeight: 800, color: "#FFF", letterSpacing: "-0.5px" }}>
                Riesgos criticos
              </h2>
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: "3px" }}>
              {risks.map((r, i) => (
                <div key={i} className="risk-row" style={{ display: "grid", gridTemplateColumns: "auto 1fr auto", gap: "16px", alignItems: "start", padding: "16px", borderRadius: "4px", border: "1px solid #1A1A1A" }}>
                  <div style={{ width: "8px", height: "8px", borderRadius: "50%", marginTop: "4px", background: r.severity === "alta" ? "#FF4D00" : r.severity === "media" ? "#FFAA00" : "#00E09A", flexShrink: 0 }} />
                  <div>
                    <div style={{ fontSize: "12px", color: "#CCC", marginBottom: "6px", fontWeight: "500" }}>{r.label}</div>
                    <div style={{ fontSize: "11px", color: "#555", lineHeight: 1.6 }}>→ {r.mitigation}</div>
                  </div>
                  <div style={{ fontSize: "9px", letterSpacing: "1px", color: r.severity === "alta" ? "#FF4D00" : r.severity === "media" ? "#FFAA00" : "#00E09A", textTransform: "uppercase", whiteSpace: "nowrap", marginTop: "2px" }}>
                    {r.severity}
                  </div>
                </div>
              ))}
            </div>
            <div style={{ marginTop: "32px", padding: "20px", background: "#0D0D12", border: "1px solid #1A1A1A", borderRadius: "4px" }}>
              <div style={{ fontSize: "10px", letterSpacing: "3px", color: "#444", marginBottom: "16px" }}>RESUMEN POR FASE</div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(120px, 1fr))", gap: "16px" }}>
                {phases.map((p) => (
                  <div key={p.id} style={{ borderLeft: `2px solid ${p.color}`, paddingLeft: "12px" }}>
                    <div style={{ fontSize: "9px", color: "#444", letterSpacing: "1px", marginBottom: "4px" }}>{p.label}</div>
                    <div style={{ fontSize: "11px", color: "#888" }}>{STATUS_LABEL[p.status]}</div>
                    <div style={{ fontSize: "13px", color: p.color, marginTop: "4px" }}>
                      {p.tasks.reduce((acc, cat) => acc + cat.items.length, 0)} tareas
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
