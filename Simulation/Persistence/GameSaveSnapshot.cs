using LeaderGame.Simulation.Campaign;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Persistence;

public sealed class GameSaveSnapshot
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public DateTimeOffset SavedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public required DateSnapshot Date { get; init; }

    public ulong RandomState { get; init; }

    public ulong InformationRandomState { get; init; }

    public required PlayerSnapshot Player { get; init; }

    public CampaignSnapshot? Campaign { get; init; }

    public List<CharacterSnapshot> Characters { get; init; } = [];

    public List<CountrySnapshot> Countries { get; init; } = [];

    public List<RelationshipSnapshot> Relationships { get; init; } = [];

    public List<DiplomaticRelationSnapshot> Diplomacy { get; init; } = [];

    public List<WarSnapshot> Wars { get; init; } = [];

    public List<PoliticalPlotSnapshot> Plots { get; init; } = [];

    public List<PowerBaseDemandSnapshot> PowerBaseDemands { get; init; } = [];

    public List<PoliticalBlocSnapshot> PoliticalBlocs { get; init; } = [];

    public List<LegislativeProposalSnapshot> LegislativeProposals { get; init; } = [];

    public List<CabinetProposalSnapshot> CabinetProposals { get; init; } = [];

    public List<ElectionPromiseSnapshot> ElectionPromises { get; init; } = [];

    public List<DiplomaticProposalSnapshot> DiplomaticProposals { get; init; } = [];

    public List<SimulationReportSnapshot> Reports { get; init; } = [];

    public List<KnownInformationSnapshot> Knowledge { get; init; } = [];

    public List<AdvisorReportSnapshot> AdvisorReports { get; init; } = [];

    public List<InformationRequestSnapshot> InformationRequests { get; init; } = [];

    public List<TruthHistorySnapshot> InformationHistory { get; init; } = [];

    public List<OrderSnapshot> PendingOrders { get; init; } = [];
}

public sealed record DateSnapshot(int Year, int Month);

public sealed class CampaignSnapshot
{
    public required string ScenarioId { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required DateSnapshot StartedOn { get; init; }

    public List<CampaignObjectiveSnapshot> Objectives { get; init; } = [];
}

public sealed class CampaignObjectiveSnapshot
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public CampaignObjectiveType Type { get; init; }

    public double TargetValue { get; init; }

    public double SecondaryTargetValue { get; init; }

    public string? RelatedCountryId { get; init; }

    public int RequiredMonths { get; init; }

    public int ProgressMonths { get; init; }

    public bool IsCompleted { get; init; }

    public DateSnapshot? CompletedOn { get; init; }
}

public sealed class PlayerSnapshot
{
    public required int CurrentCharacterId { get; init; }

    public required string CountryId { get; init; }

    public required string LineageId { get; init; }

    public required string LineageName { get; init; }

    public PoliticalLineageType LineageType { get; init; }

    public List<int> LineageMemberIds { get; init; } = [];

    public bool HasLost { get; init; }

    public string? LossReason { get; init; }

    public bool HasWon { get; init; }

    public string? WinReason { get; init; }

    public int ElectionsWon { get; init; }

    public int MonthsOutOfPower { get; init; }

    public int ConsecutiveLowViabilityMonths { get; init; }

    public double PoliticalViability { get; init; } = 100;
}

public sealed class CharacterSnapshot
{
    public required int Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public int Age { get; init; }

    public int Health { get; init; }

    public int Competence { get; init; }

    public int Ambition { get; init; }

    public int Legitimacy { get; init; }

    public int Influence { get; init; }

    public Position? Position { get; init; }

    public PoliticalStatus Status { get; init; }

    public bool IsAlive { get; init; }

    public Dictionary<string, int> Allegiances { get; init; } = new(StringComparer.Ordinal);

    public List<PowerBaseValueSnapshot> PowerBaseStanding { get; init; } = [];
}

public sealed class CountrySnapshot
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public long Population { get; init; }

    public decimal Gdp { get; init; }

    public decimal Treasury { get; init; }

    public decimal Debt { get; init; }

    public int ArmySize { get; init; }

    public double ArmyReadiness { get; init; }

    public decimal TaxRate { get; init; }

    public decimal AdministrativeEfficiency { get; init; }

    public decimal ArmyFunding { get; init; }

    public decimal AdministrationFunding { get; init; }

    public decimal CourtFunding { get; init; }

    public double WarExhaustion { get; init; }

    public double PublicUnrest { get; init; }

    public decimal LastMonthlyTaxRevenue { get; init; }

    public decimal LastMonthlyTradeIncome { get; init; }

    public decimal LastMonthlyExpenses { get; init; }

    public decimal LastMonthlyDebtInterest { get; init; }

    public decimal LastMonthlyBalance { get; init; }

    public GovernmentType GovernmentType { get; init; }

    public AdministrativeDevelopment AdministrativeDevelopment { get; init; }

    public double GovernmentStability { get; init; }

    public int ElectionIntervalMonths { get; init; }

    public int MonthsUntilElection { get; init; }

    public int ElectionCampaignMonths { get; init; }

    public ElectionMethod ElectionMethod { get; init; }

    public LegislativeBodyType LegislativeBody { get; init; }

    public int LegislativeIndependence { get; init; }

    public int RulerId { get; init; }

    public List<string> NeighborIds { get; init; } = [];

    public List<int> SuccessionOrderIds { get; init; } = [];

    public List<int> PoliticalFigureIds { get; init; } = [];

    public List<PowerBaseValueSnapshot> PowerBaseStrengths { get; init; } = [];

    public List<AdministrativeOfficeSnapshot> AdministrativeOffices { get; init; } = [];
}

public sealed class AdministrativeOfficeSnapshot
{
    public AdministrativeFunction Function { get; init; }

    public required string Name { get; init; }

    public Position? ResponsiblePosition { get; init; }

    public int Capacity { get; init; }

    public int Reach { get; init; }

    public int Integrity { get; init; }

    public int Workload { get; init; }

    public int PatronageDependence { get; init; }
}

public sealed record PowerBaseValueSnapshot(
    PowerBaseType Type,
    int Value);

public sealed record RelationshipSnapshot(
    int FromId,
    int ToId,
    int Opinion,
    int Trust,
    int Fear);

public sealed class DiplomaticRelationSnapshot
{
    public required string CountryAId { get; init; }

    public required string CountryBId { get; init; }

    public int Relations { get; init; }

    public int Trust { get; init; }

    public int Tension { get; init; }

    public bool HasTradeAgreement { get; init; }

    public DateSnapshot? TradeAgreementStartedOn { get; init; }
}

public sealed class WarSnapshot
{
    public Guid Id { get; init; }

    public required string AttackerId { get; init; }

    public required string DefenderId { get; init; }

    public required DateSnapshot StartedOn { get; init; }

    public double WarScore { get; init; }

    public int MonthsActive { get; init; }

    public WarStance AttackerStance { get; init; }

    public WarStance DefenderStance { get; init; }

    public WarStatus Status { get; init; }
}

public sealed class PoliticalPlotSnapshot
{
    public Guid Id { get; init; }

    public PlotType Type { get; init; }

    public required string CountryId { get; init; }

    public int InstigatorId { get; init; }

    public double Progress { get; init; }

    public List<int> SupporterIds { get; init; } = [];

    public int DiscoveryStage { get; init; }

    public bool IsResolved { get; init; }

    public bool Succeeded { get; init; }
}

public sealed class PowerBaseDemandSnapshot
{
    public Guid Id { get; init; }

    public required string CountryId { get; init; }

    public PowerBaseType PowerBase { get; init; }

    public PowerBaseDemandType Type { get; init; }

    public int? SpokespersonId { get; init; }

    public decimal TargetValue { get; init; }

    public int MonthsOpen { get; init; }

    public int EscalationLevel { get; init; }

    public bool AcknowledgedByRuler { get; init; }

    public bool IsRejected { get; init; }

    public DateSnapshot? ResolvedOn { get; init; }

    public bool IsResolved { get; init; }
}

public sealed class PoliticalBlocSnapshot
{
    public Guid Id { get; init; }

    public required string CountryId { get; init; }

    public int LeaderId { get; init; }

    public List<PowerBaseType> PowerBases { get; init; } = [];

    public List<int> MemberIds { get; init; } = [];

    public int MonthsActive { get; init; }

    public double Cohesion { get; init; }

    public bool IsActive { get; init; }
}

public sealed class LegislativeProposalSnapshot
{
    public Guid Id { get; init; }

    public required string CountryId { get; init; }

    public int SponsorId { get; init; }

    public int DrafterId { get; init; }

    public LegislativeProposalType Type { get; init; }

    public required DateSnapshot CreatedOn { get; init; }

    public int MonthsOpen { get; init; }

    public LegislativeProposalStatus Status { get; init; }

    public decimal? TargetTaxRate { get; init; }

    public decimal? TargetArmyFunding { get; init; }

    public decimal? TargetAdministrationFunding { get; init; }

    public decimal? TargetCourtFunding { get; init; }
}

public sealed class CabinetProposalSnapshot
{
    public Guid Id { get; init; }

    public required string CountryId { get; init; }

    public int AdvisorId { get; init; }

    public CabinetProposalType Type { get; init; }

    public string? TargetCountryId { get; init; }

    public Guid? WarId { get; init; }

    public decimal TargetValue { get; init; }

    public string? Rationale { get; init; }

    public required DateSnapshot CreatedOn { get; init; }

    public int MonthsOpen { get; init; }

    public CabinetProposalStatus Status { get; init; }
}

public sealed class ElectionPromiseSnapshot
{
    public Guid Id { get; init; }

    public required string CountryId { get; init; }

    public int CandidateId { get; init; }

    public ElectionPromiseType Type { get; init; }

    public decimal TargetValue { get; init; }

    public required DateSnapshot MadeOn { get; init; }

    public ElectionPromiseStatus Status { get; init; }

    public int MonthsSinceElection { get; init; }
}

public sealed class DiplomaticProposalSnapshot
{
    public Guid Id { get; init; }

    public DiplomaticProposalType Type { get; init; }

    public required string SourceCountryId { get; init; }

    public required string TargetCountryId { get; init; }

    public required DateSnapshot CreatedOn { get; init; }

    public decimal DemandedPayment { get; init; }

    public int MonthsOpen { get; init; }

    public DiplomaticProposalStatus Status { get; init; }
}

public sealed record SimulationReportSnapshot(
    DateSnapshot Date,
    ReportCategory Category,
    string Title,
    string Details);

public sealed class KnownInformationSnapshot
{
    public InformationMetric Metric { get; init; }

    public required string SubjectCountryId { get; init; }

    public string? RelatedCountryId { get; init; }

    public double Estimate { get; init; }

    public double Margin { get; init; }

    public int ReportedConfidence { get; init; }

    public required DateSnapshot AsOf { get; init; }

    public required DateSnapshot ReceivedOn { get; init; }

    public int SourceAdvisorId { get; init; }

    public required string SourceAdvisorName { get; init; }

    public bool WasRequested { get; init; }
}

public sealed class AdvisorReportSnapshot
{
    public Guid Id { get; init; }

    public InformationTopic Topic { get; init; }

    public int AdvisorId { get; init; }

    public required string SubjectCountryId { get; init; }

    public string? RelatedCountryId { get; init; }

    public required DateSnapshot ProducedOn { get; init; }

    public required DateSnapshot DataAsOf { get; init; }

    public bool WasRequested { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public List<KnownInformationSnapshot> Facts { get; init; } = [];

    public List<string> Caveats { get; init; } = [];
}

public sealed class InformationRequestSnapshot
{
    public Guid Id { get; init; }

    public InformationTopic Topic { get; init; }

    public int AdvisorId { get; init; }

    public required string SubjectCountryId { get; init; }

    public string? RelatedCountryId { get; init; }

    public required DateSnapshot RequestedOn { get; init; }

    public int RemainingMonths { get; init; }

    public double WillingnessAtRequest { get; init; }

    public InformationRequestStatus Status { get; init; }
}

public sealed class TruthHistorySnapshot
{
    public required DateSnapshot Date { get; init; }

    public List<TruthValueSnapshot> Values { get; init; } = [];
}

public sealed class TruthValueSnapshot
{
    public InformationMetric Metric { get; init; }

    public required string SubjectCountryId { get; init; }

    public string? RelatedCountryId { get; init; }

    public double Value { get; init; }
}

public enum SaveOrderKind
{
    ChangeTax,
    SetBudget,
    AppointAdvisor,
    DismissAdvisor,
    InvestigateCharacter,
    ArrestCharacter,
    ReleasePrisoner,
    ImproveRelations,
    NegotiateTradeAgreement,
    EndTradeAgreement,
    DeclareWar,
    SetWarStance,
    OfferPeace,
    RespondToDiplomaticProposal,
    RequestReport,
    OppositionAction,
    AdministrativeReform
}

public sealed record OrderSnapshot
{
    public Guid Id { get; init; }

    public SaveOrderKind Kind { get; init; }

    public int IssuerId { get; init; }

    public int RecipientId { get; init; }

    public required DateSnapshot IssuedOn { get; init; }

    public OrderStatus Status { get; init; }

    public string? CountryId { get; init; }

    public string? SourceCountryId { get; init; }

    public string? TargetCountryId { get; init; }

    public int? SubjectCharacterId { get; init; }

    public Position? Position { get; init; }

    public decimal? TargetTaxRate { get; init; }

    public decimal? TargetArmyFunding { get; init; }

    public decimal? TargetAdministrationFunding { get; init; }

    public decimal? TargetCourtFunding { get; init; }

    public Guid? WarId { get; init; }

    public WarStance? RequestedStance { get; init; }

    public PeaceOfferTerms? PeaceTerms { get; init; }

    public Guid? DiplomaticProposalId { get; init; }

    public bool? Accept { get; init; }

    public InformationTopic? Topic { get; init; }

    public string? SubjectCountryId { get; init; }

    public string? RelatedCountryId { get; init; }

    public OppositionActionType? OppositionAction { get; init; }

    public PowerBaseType? TargetPowerBase { get; init; }

    public AdministrativeFunction? AdministrativeFunction { get; init; }

    public AdministrativeReformType? AdministrativeReform { get; init; }
}
