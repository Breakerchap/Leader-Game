using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Politics;

public enum PlotType
{
    Coup
}

public sealed class PoliticalPlot
{
    private double _progress;

    public Guid Id { get; } = Guid.NewGuid();

    public PlotType Type { get; init; } = PlotType.Coup;

    public required Country Country { get; init; }

    public required Character Instigator { get; init; }

    public double Progress
    {
        get => _progress;
        set => _progress = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// 0 = hidden, 1 = rumours, 2 = credible evidence.
    /// </summary>
    public int DiscoveryStage { get; internal set; }

    public bool IsResolved { get; internal set; }

    public bool Succeeded { get; internal set; }
}
