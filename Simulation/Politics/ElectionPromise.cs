using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Politics;

public enum ElectionPromiseType
{
    TaxRelief,
    AdministrativeInvestment,
    MilitaryInvestment,
    CoalitionPatronage
}

public enum ElectionPromiseStatus
{
    Campaigning,
    AwaitingFulfilment,
    Fulfilled,
    Broken,
    Lapsed
}

/// <summary>
/// A public electoral commitment made by a named candidate. The promise affects
/// campaign constituencies immediately and, after victory, becomes a governing
/// obligation with a real policy condition.
/// </summary>
public sealed class ElectionPromise
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Country Country { get; init; }

    public required Character Candidate { get; init; }

    public required ElectionPromiseType Type { get; init; }

    public decimal TargetValue { get; init; }

    public required GameDate MadeOn { get; init; }

    public ElectionPromiseStatus Status { get; set; } =
        ElectionPromiseStatus.Campaigning;

    public int MonthsSinceElection { get; set; }
}
