using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;

namespace LeaderGame.Simulation.Politics;

public enum CabinetProposalType
{
    LowerTaxes,
    RaiseTaxes,
    RaiseArmyFunding,
    RaiseAdministrationFunding,
    RaiseCourtFunding,
    ImproveRelations,
    NegotiateTradeAgreement,
    DefensiveWarStance,
    AggressiveWarStance,
    OfferWhitePeace
}

public enum CabinetProposalStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired,
    Withdrawn
}

public sealed class CabinetProposal
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Country Country { get; init; }

    public required Character Advisor { get; init; }

    public required CabinetProposalType Type { get; init; }

    public Country? TargetCountry { get; init; }

    public War? War { get; init; }

    public decimal TargetValue { get; init; }

    public string? Rationale { get; init; }

    public required GameDate CreatedOn { get; init; }

    public int MonthsOpen { get; set; }

    public CabinetProposalStatus Status { get; set; } =
        CabinetProposalStatus.Pending;
}
