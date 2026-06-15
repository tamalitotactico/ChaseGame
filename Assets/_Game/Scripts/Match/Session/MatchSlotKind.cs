/// <summary>
/// Naturaleza de un slot de la partida segun la sesion. Desacopla "que compone la partida"
/// de "como se spawnea" (ya abstraido por ISpawnService): GameManager pregunta el kind de cada
/// slot y decide el brain, sin saber de red.
///
///   LocalPlayer  -> el humano de ESTE cliente (PlayerBrain + indicadores de aim).
///   RemoteHuman  -> otro humano por red (Fusion, futuro). Hoy nunca lo produce LocalMatchSession.
///   Bot          -> IA local (BotBrain).
/// </summary>
public enum MatchSlotKind
{
    LocalPlayer,
    RemoteHuman,
    Bot,
}
