namespace LeaderGame.Simulation.Information;

/// <summary>
/// Hidden historical truth used only by the simulation when reconstructing what
/// an adviser could have observed at an earlier date. The player never reads
/// these values directly.
/// </summary>
public sealed class InformationTruthSnapshot
{
    public required GameDate Date { get; init; }

    public Dictionary<InformationKey, double> Values { get; } = [];
}
