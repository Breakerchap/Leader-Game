namespace LeaderGame.Presentation;

public sealed record ScenarioOptionView(
    string Id,
    string Name,
    string Country,
    string Leader,
    string Lineage,
    string GovernmentStyle,
    string Summary,
    string StrategicProblem);

public sealed record CampaignObjectiveView(
    string Id,
    string Title,
    string Description,
    string Progress,
    bool IsCompleted);

public sealed record CampaignView(
    string ScenarioName,
    string Summary,
    int CompletedObjectives,
    int TotalObjectives,
    IReadOnlyList<CampaignObjectiveView> Objectives);

public sealed record MetricCardView(
    string Label,
    string Value,
    string Detail,
    bool IsStale);

public sealed record AdvisorCardView(
    string Office,
    string Name,
    string Ability,
    string Trust,
    string Obedience,
    string Risk);

public sealed record BriefingItemView(
    string Category,
    string Title,
    string Summary,
    string Meta,
    bool NeedsAttention);

public sealed record CabinetProposalView(
    Guid Id,
    string Advisor,
    string Office,
    string Title,
    string Description,
    string Age);

public sealed record CrisisChoiceView(
    Guid CrisisId,
    string Response,
    string Label,
    string Description);

public sealed record CrisisAdviceView(
    string Advisor,
    string Office,
    string Recommendation,
    string Reason);

public sealed record CrisisView(
    Guid Id,
    string Title,
    string Summary,
    string Severity,
    string Age,
    string ResponseStatus,
    bool CanRespond,
    IReadOnlyList<CrisisAdviceView> Advice,
    IReadOnlyList<CrisisChoiceView> Choices);

public sealed record PendingOrderView(
    Guid Id,
    string Type,
    string Description,
    string Recipient,
    string IssuedOn);

public sealed record OrderOutcomeView(
    string Date,
    string Title,
    string Details,
    bool NeedsAttention);

public sealed record PendingInquiryView(
    string Advisor,
    string Topic,
    string Subject,
    string Since);

public sealed record ReportFactView(
    string Label,
    string Value,
    string Margin,
    string Confidence);

public sealed record IntelligenceReportView(
    Guid Id,
    string Title,
    string Source,
    string Summary,
    string DataAge,
    string Origin,
    IReadOnlyList<ReportFactView> Facts,
    IReadOnlyList<string> Caveats);

public sealed record ForeignCountryOptionView(
    string Id,
    string Name);

public sealed record WarCampaignView(
    Guid Id,
    string OpponentId,
    string OpponentName,
    string MonthsActive,
    string Stance,
    string WarAim,
    string CampaignPosition,
    string OwnArmy,
    string OwnReadiness,
    string OwnWarExhaustion,
    string EnemyArmy,
    string EnemyReadiness,
    string MarshalName,
    string MarshalObedience,
    string ChancellorName,
    string ChancellorObedience);

public sealed record MilitaryView(
    string MarshalName,
    string MarshalObedience,
    string ArmyStrength,
    string ArmyReadiness,
    string WarExhaustion,
    IReadOnlyList<WarCampaignView> Campaigns);

public sealed record ForeignStateView(
    string Id,
    string Name,
    string RulerName,
    string Government,
    string Relations,
    string Trust,
    string Tension,
    string Gdp,
    string Army,
    string TradeStatus,
    string WarStatus,
    bool HasTradeAgreement,
    bool AtWar,
    bool CanDeclareWar);

public sealed record DiplomaticProposalView(
    Guid Id,
    string SourceCountry,
    string Type,
    string Terms,
    string Age);

public sealed record ForeignAffairsView(
    string ChancellorName,
    string ChancellorObedience,
    IReadOnlyList<ForeignStateView> Countries,
    IReadOnlyList<DiplomaticProposalView> IncomingProposals);

public sealed record ArchiveEntryView(
    string Date,
    string Category,
    string Title,
    string Details,
    string Source,
    bool NeedsAttention);

public sealed record ArchiveView(
    IReadOnlyList<ArchiveEntryView> Entries);

public sealed record EconomyReportSnapshotView(
    string Delivered,
    string DataAge,
    string Source,
    string Origin,
    string Treasury,
    string Debt,
    string Revenue,
    string Expenses,
    string Balance,
    bool HasCaveats);

public sealed record EconomyView(
    string TreasurerName,
    string TreasurerObedience,
    IReadOnlyList<MetricCardView> Summary,
    IReadOnlyList<EconomyReportSnapshotView> History);

public sealed record LegislativeProposalView(
    Guid Id,
    string Title,
    string Detail,
    string Age,
    string Status);

public sealed record AdministrativeOfficeView(
    string Name,
    string Function,
    string Head,
    string Performance,
    string Reach,
    string Integrity,
    string Workload,
    string Structure);

public sealed record RegionView(
    string Id,
    string Name,
    string Importance,
    string CentralControl,
    string LocalPower,
    string Unrest,
    string Privileges,
    string Prosperity);

public sealed record GovernmentPolicyView(
    double TaxPercent,
    double ArmyFundingPercent,
    double AdministrationFundingPercent,
    double CourtFundingPercent,
    string TreasurerName,
    string TreasurerObedience,
    string PolicyNote,
    string LegislativeBody,
    string LegislativeAuthority,
    bool HasLegislation,
    IReadOnlyList<LegislativeProposalView> Legislation,
    string AdministrativeEfficiency,
    string AdministrativeDevelopment,
    IReadOnlyList<AdministrativeOfficeView> AdministrativeOffices,
    bool CanDirectAdministration,
    string AdministrativeActionStatus,
    IReadOnlyList<RegionView> Regions,
    bool CanDirectRegions,
    string RegionalActionStatus,
    string Regime,
    string PoliticalCycle,
    string PoliticalCycleDetail,
    bool HasElection,
    bool IsElectionCampaignActive,
    bool CanMakeElectionPromise,
    string ElectionPromise,
    string ElectionPromiseStatus);

public sealed record OfficeView(
    string Position,
    string HolderName,
    string Ability,
    string Trust,
    string Obedience,
    string Risk);

public sealed record PoliticalDemandView(
    Guid Id,
    string Group,
    string Request,
    string Spokesperson,
    string Age,
    string Escalation,
    string ResponseStatus);

public sealed record CourtFigureView(
    int Id,
    string Name,
    string Role,
    string Ability,
    string Ambition,
    string Influence,
    string Trust,
    string Obedience,
    string Risk,
    string Constituencies,
    string KnownEvidence,
    bool HasKnownEvidence,
    bool IsAvailableForOffice,
    bool CanInvestigate,
    bool CanArrest,
    bool CanRelease,
    bool CanReceiveCourtAction);

public sealed record CourtPoliticsView(
    IReadOnlyList<OfficeView> Offices,
    IReadOnlyList<CourtFigureView> Figures,
    IReadOnlyList<PoliticalDemandView> Demands,
    string KnownOpposition,
    string KnownPressure,
    bool IsPlayerInOpposition,
    bool CanTakeOppositionAction,
    IReadOnlyList<string> OppositionPowerBases,
    string OppositionStatus,
    bool CanTakeCourtAction,
    string CourtActionStatus);

public sealed record PlayerViewState(
    string CountryName,
    string Date,
    string LineageName,
    string RulerName,
    string RulerSummary,
    bool HasLost,
    string? LossReason,
    bool HasWon,
    string? WinReason,
    CampaignView Campaign,
    int PendingOrders,
    int PendingReports,
    IReadOnlyList<PendingOrderView> PendingOrderDetails,
    IReadOnlyList<OrderOutcomeView> RecentOrderOutcomes,
    IReadOnlyList<CabinetProposalView> CabinetProposals,
    IReadOnlyList<CrisisView> Crises,
    IReadOnlyList<MetricCardView> Metrics,
    IReadOnlyList<AdvisorCardView> Advisors,
    IReadOnlyList<BriefingItemView> Briefings,
    IReadOnlyList<PendingInquiryView> PendingInquiries,
    IReadOnlyList<IntelligenceReportView> IntelligenceReports,
    IReadOnlyList<ForeignCountryOptionView> ForeignCountries,
    GovernmentPolicyView Government,
    CourtPoliticsView Court,
    ForeignAffairsView ForeignAffairs,
    MilitaryView Military,
    EconomyView Economy,
    ArchiveView Archive,
    string StatusMessage);
