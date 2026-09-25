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
    string StatusMessage);
