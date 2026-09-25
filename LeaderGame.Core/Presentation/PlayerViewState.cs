namespace LeaderGame.Presentation;

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

public sealed record GovernmentPolicyView(
    double TaxPercent,
    double ArmyFundingPercent,
    double AdministrationFundingPercent,
    double CourtFundingPercent,
    string TreasurerName,
    string TreasurerObedience,
    string PolicyNote);

public sealed record OfficeView(
    string Position,
    string HolderName,
    string Ability,
    string Trust,
    string Obedience,
    string Risk);

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
    bool IsAvailableForOffice);

public sealed record CourtPoliticsView(
    IReadOnlyList<OfficeView> Offices,
    IReadOnlyList<CourtFigureView> Figures,
    string KnownOpposition,
    string KnownPressure);

public sealed record PlayerViewState(
    string CountryName,
    string Date,
    string LineageName,
    string RulerName,
    string RulerSummary,
    bool HasLost,
    string? LossReason,
    int PendingOrders,
    int PendingReports,
    IReadOnlyList<MetricCardView> Metrics,
    IReadOnlyList<AdvisorCardView> Advisors,
    IReadOnlyList<BriefingItemView> Briefings,
    IReadOnlyList<PendingInquiryView> PendingInquiries,
    IReadOnlyList<IntelligenceReportView> IntelligenceReports,
    IReadOnlyList<ForeignCountryOptionView> ForeignCountries,
    GovernmentPolicyView Government,
    CourtPoliticsView Court,
    ForeignAffairsView ForeignAffairs,
    string StatusMessage);
