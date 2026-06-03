using System.Collections.Generic;

/// <summary>
/// Implementacion Phase 0/1/2 de IWorldQueryService. Envuelve las listas que
/// mantiene GameManager. GameManager sigue siendo el dueno de los datos; los
/// consumidores (bots, revive, abilities) dependen solo de esta abstraccion.
///
/// Registro: hecho por GameManager en Awake via ServiceLocator.Register.
/// </summary>
public sealed class LocalWorldQuery : IWorldQueryService
{
    readonly GameManager _gm;

    public LocalWorldQuery(GameManager gm) { _gm = gm; }

    public IReadOnlyList<Character> Hunters => _gm.Hunters;
    public IReadOnlyList<Character> Preys   => _gm.Preys;

    public IReadOnlyList<Character> GetTeam(CharacterTeam team) =>
        team == CharacterTeam.Hunter ? _gm.Hunters : _gm.Preys;

    public IReadOnlyList<Character> GetEnemiesOf(CharacterTeam team) =>
        team == CharacterTeam.Hunter ? _gm.Preys : _gm.Hunters;

    public IReadOnlyList<Character> GetAlliesOf(CharacterTeam team) =>
        team == CharacterTeam.Hunter ? _gm.Hunters : _gm.Preys;
}
