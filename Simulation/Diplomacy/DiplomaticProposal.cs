using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Diplomacy;

public enum DiplomaticProposalType
{
    TradeAgreement,
    TributeUltimatum
}

public enum DiplomaticProposalStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired,
    Withdrawn,
    EscalatedToWar
}

public sealed class DiplomaticProposal
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DiplomaticProposalType Type { get; init; }

    public required Country SourceCountry { get; init; }

    public required Country TargetCountry { get; init; }

    public required GameDate CreatedOn { get; init; }

    /// <summary>
    /// Monetary demand attached to coercive proposals. Zero for proposals
    /// without a payment component.
    /// </summary>
    public decimal DemandedPayment { get; init; }

    public int MonthsOpen { get; internal set; }

    public DiplomaticProposalStatus Status { get; internal set; } =
        DiplomaticProposalStatus.Pending;
}
