using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Politics;

public enum LegislativeProposalType
{
    TaxRate,
    Budget
}

public enum LegislativeProposalStatus
{
    Pending,
    Passed,
    Rejected
}

/// <summary>
/// A fiscal proposal that has left the executive and is awaiting a formal
/// institution's consent. The exact support calculation remains simulation
/// truth; the player sees the public process and result.
/// </summary>
public sealed class LegislativeProposal
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Country Country { get; init; }

    public required Character Sponsor { get; init; }

    public required Character Drafter { get; init; }

    public LegislativeProposalType Type { get; init; }

    public required GameDate CreatedOn { get; init; }

    public int MonthsOpen { get; set; }

    public LegislativeProposalStatus Status { get; set; } =
        LegislativeProposalStatus.Pending;

    public decimal? TargetTaxRate { get; init; }

    public decimal? TargetArmyFunding { get; init; }

    public decimal? TargetAdministrationFunding { get; init; }

    public decimal? TargetCourtFunding { get; init; }
}
