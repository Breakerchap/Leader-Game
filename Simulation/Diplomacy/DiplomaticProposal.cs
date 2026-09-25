using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Diplomacy;

public enum DiplomaticProposalType
{
    TradeAgreement,
    NonAggressionPact
}

public enum DiplomaticProposalStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired,
    Withdrawn
}

public sealed class DiplomaticProposal
{
    public Guid Id { get; } = Guid.NewGuid();

    public DiplomaticProposalType Type { get; init; }

    public required Country SourceCountry { get; init; }

    public required Country TargetCountry { get; init; }

    public required GameDate CreatedOn { get; init; }

    public int MonthsOpen { get; internal set; }

    public DiplomaticProposalStatus Status { get; internal set; } =
        DiplomaticProposalStatus.Pending;
}
