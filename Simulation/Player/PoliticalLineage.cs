using LeaderGame.Simulation.Characters;

namespace LeaderGame.Simulation.Player;

public enum PoliticalLineageType
{
    Dynasty,
    Party,
    Faction
}

/// <summary>
/// The political continuity controlled by the player. This is deliberately
/// separate from the country: the state may survive even when the player's
/// dynasty, party or faction loses power.
/// </summary>
public class PoliticalLineage
{
    public required string Id { get; init; }

    public required string Name { get; set; }

    public PoliticalLineageType Type { get; set; }

    public List<Character> Members { get; } = [];

    public bool Contains(Character character) => Members.Contains(character);
}
