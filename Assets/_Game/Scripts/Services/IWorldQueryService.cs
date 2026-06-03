using System.Collections.Generic;

/// <summary>
/// Abstraccion de consulta del estado del mundo: listas de personajes por bando.
/// Desacopla el gameplay del singleton concreto GameManager.Instance.
///
/// Phase 0/1/2 usa LocalWorldQuery (envuelve las listas de GameManager).
/// Phase 3 swappea a una implementacion que lee NetworkObjects de Fusion sin tocar
/// el codigo de gameplay que la consume.
/// </summary>
public interface IWorldQueryService
{
    IReadOnlyList<Character> Hunters { get; }
    IReadOnlyList<Character> Preys   { get; }

    /// <summary>Lista del bando indicado.</summary>
    IReadOnlyList<Character> GetTeam(CharacterTeam team);

    /// <summary>Lista del bando contrario al indicado.</summary>
    IReadOnlyList<Character> GetEnemiesOf(CharacterTeam team);

    /// <summary>Lista del mismo bando indicado (alias semantico de GetTeam para aliados).</summary>
    IReadOnlyList<Character> GetAlliesOf(CharacterTeam team);
}
