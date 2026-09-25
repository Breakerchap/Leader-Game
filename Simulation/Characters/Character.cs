namespace LeaderGame.Simulation.Characters;

public class Character
{
    public required int Id { get; init; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public int Age { get; set; }

    public int Competence { get; set; }
    public int Ambition { get; set; }

    /// <summary>
    /// Current personal loyalty to the ruler. This will eventually be replaced by
    /// richer relationships and loyalties to people, institutions and factions.
    /// </summary>
    public int Loyalty { get; set; }

    /// <summary>
    /// Personal claim or perceived right to rule. It matters most while this
    /// character is ruler or a succession candidate, but remains attached to the person.
    /// </summary>
    public int Legitimacy { get; set; }

    /// <summary>
    /// Advisory office currently held. Null means the character holds no advisory office.
    /// Rulership is tracked by Country.Ruler rather than as an advisory position.
    /// </summary>
    public Position? Position { get; set; }

    public bool IsAlive { get; set; } = true;

    public bool IsInOffice => Position.HasValue;

    public string FullName => $"{FirstName} {LastName}";
}
