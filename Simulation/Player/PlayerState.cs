using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Player;

public class PlayerState
{
    public required Character CurrentCharacter { get; set; }

    public required Country Country { get; set; }

    public required PoliticalLineage Lineage { get; set; }

    public bool HasLost { get; set; }

    public string? LossReason { get; set; }

    public bool HasWon { get; set; }

    public string? WinReason { get; set; }

    public int ElectionsWon { get; set; }

    /// <summary>
    /// Number of consecutive simulated months the player's lineage has spent
    /// outside control of the national government.
    /// </summary>
    public int MonthsOutOfPower { get; set; }

    /// <summary>
    /// Consecutive months in which the lineage has had neither meaningful
    /// political strength nor a credible near-term route back to office.
    /// </summary>
    public int ConsecutiveLowViabilityMonths { get; set; }

    /// <summary>
    /// Hidden simulation measure used to decide whether an out-of-power lineage
    /// is still a meaningful political force. The UI should normally describe
    /// this qualitatively rather than exposing the exact number.
    /// </summary>
    public double PoliticalViability { get; set; } = 100;

    public bool IsInPower =>
        Lineage.Contains(Country.Ruler);
}
