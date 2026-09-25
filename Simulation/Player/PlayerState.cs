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
}
