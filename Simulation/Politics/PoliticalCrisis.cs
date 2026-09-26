using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Politics;

public enum PoliticalCrisisType
{
    RegionalBreakdown,
    FiscalEmergency,
    PoliticalStandoff,
    SuccessionDispute,
    WarEmergency
}

public enum PoliticalCrisisStatus
{
    Active,
    Resolved,
    BrokeAgainstGovernment
}

public enum PoliticalCrisisResponse
{
    StrengthenRegionalControl,
    OfferRegionalConcessions,
    FundRegionalRelief,

    RaiseEmergencyRevenue,
    CutStateCommitments,
    BorrowForTime,

    CooptOpposition,
    ConstitutionalCompromise,
    ConfrontOpposition,

    PubliclyNameSuccessor,
    BalanceSuccessionFactions,
    ConveneSuccessionSettlement,

    EmergencyMobilisation,
    DismissMarshal,
    SeekPeaceSettlement
}

/// <summary>
/// A crisis is an ongoing political situation rather than a one-off event.
/// Player responses can reduce the immediate pressure, but the crisis only
/// ends when the underlying problem actually improves.
/// </summary>
public sealed class PoliticalCrisis
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Country Country { get; init; }

    public required PoliticalCrisisType Type { get; init; }

    public Region? Region { get; init; }

    public Guid? RelatedBlocId { get; init; }

    public Guid? RelatedWarId { get; init; }

    public required GameDate StartedOn { get; init; }

    public int Stage { get; set; } = 1;

    public int MonthsActive { get; set; }

    public int MonthsAtCurrentStage { get; set; }

    public bool AwaitingDecision { get; set; } = true;

    public PoliticalCrisisResponse? LastResponse { get; set; }

    public PoliticalCrisisStatus Status { get; set; } =
        PoliticalCrisisStatus.Active;

    public GameDate? ResolvedOn { get; set; }
}
