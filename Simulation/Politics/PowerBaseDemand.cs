using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Politics;

public enum PowerBaseDemandType
{
    LowerTaxes,
    RaiseArmyFunding,
    RaiseAdministrationFunding,
    RaiseCourtFunding,
    EndWar
}

public sealed class PowerBaseDemand
{
    public required Country Country { get; init; }

    public required PowerBaseType PowerBase { get; init; }

    public required PowerBaseDemandType Type { get; init; }

    /// <summary>
    /// Political figure currently carrying the group's demand into court.
    /// The spokesperson may change if they die, are imprisoned or lose relevance.
    /// </summary>
    public Character? Spokesperson { get; set; }

    /// <summary>
    /// Numeric target used by funding and tax demands. EndWar ignores this value.
    /// </summary>
    public decimal TargetValue { get; init; }

    public int MonthsOpen { get; set; }

    public int EscalationLevel { get; set; }

    public bool IsResolved { get; set; }
}
