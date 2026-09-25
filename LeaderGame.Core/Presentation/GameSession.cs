using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Presentation;

/// <summary>
/// Player-facing application façade.
///
/// UI hosts should prefer this type over reading GameState directly. It converts
/// hidden simulation state into the information, public acts and qualitative
/// judgements the ruler can reasonably act on.
/// </summary>
public sealed class GameSession
{
    private readonly GameSimulation _simulation;
    private string _statusMessage = "Inherited briefings loaded.";

    public GameSession()
        : this(new GameSimulation(DemoScenario.Create()))
    {
    }

    internal GameSession(GameSimulation simulation)
    {
        _simulation = simulation;
        View = BuildView();
    }

    public PlayerViewState View { get; private set; }

    public void AdvanceMonth()
    {
        if (_simulation.State.Player.HasLost)
            return;

        _simulation.AdvanceMonth();
        _statusMessage = $"Time advanced to {_simulation.State.Date}.";
        Refresh();
    }

    public void RequestEconomyReport() =>
        QueueReport(InformationTopic.Economy, _simulation.State.Player.Country.Id);

    public void RequestDomesticPoliticsReport() =>
        QueueReport(InformationTopic.DomesticPolitics, _simulation.State.Player.Country.Id);

    public void RequestOwnMilitaryReport() =>
        QueueReport(InformationTopic.Military, _simulation.State.Player.Country.Id);

    public void RequestForeignMilitaryReport(string countryId) =>
        QueueReport(InformationTopic.Military, countryId);

    public void RequestForeignAffairsReport(string countryId) =>
        QueueReport(InformationTopic.ForeignAffairs, countryId);

    public void Refresh(string? statusMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(statusMessage))
            _statusMessage = statusMessage;

        View = BuildView();
    }

    private void QueueReport(InformationTopic topic, string subjectCountryId)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var subject = state.FindCountry(subjectCountryId);

        if (subject is null)
        {
            Refresh("That country is no longer available as a report subject.");
            return;
        }

        var advisor = topic switch
        {
            InformationTopic.Economy => country.GetOfficeHolder(Position.Treasurer),
            InformationTopic.Military => country.GetOfficeHolder(Position.Marshal),
            InformationTopic.ForeignAffairs or InformationTopic.DomesticPolitics =>
                country.GetOfficeHolder(Position.Chancellor),
            _ => null
        };

        if (advisor is null)
        {
            Refresh($"The office responsible for {InformationSystem.FormatTopic(topic).ToLowerInvariant()} is vacant.");
            return;
        }

        var alreadyQueued = state.PendingOrders
            .OfType<RequestReportOrder>()
            .Any(order =>
                order.Topic == topic &&
                ReferenceEquals(order.Recipient, advisor) &&
                ReferenceEquals(order.SubjectCountry, subject));

        var alreadyUnderway = state.InformationRequests.Any(request =>
            request.Status == InformationRequestStatus.Pending &&
            request.Topic == topic &&
            ReferenceEquals(request.Advisor, advisor) &&
            ReferenceEquals(request.SubjectCountry, subject));

        if (alreadyQueued || alreadyUnderway)
        {
            Refresh($"{advisor.FullName} is already handling that inquiry.");
            return;
        }

        if (topic is InformationTopic.Economy or InformationTopic.DomesticPolitics &&
            !ReferenceEquals(subject, country))
        {
            Refresh("That office can only produce this kind of report about the ruler's own government.");
            return;
        }

        if (topic == InformationTopic.ForeignAffairs &&
            ReferenceEquals(subject, country))
        {
            Refresh("Foreign-affairs assessments require another country.");
            return;
        }

        _simulation.SubmitOrder(new RequestReportOrder
        {
            Issuer = country.Ruler,
            Recipient = advisor,
            IssuedOn = state.Date,
            Country = country,
            Topic = topic,
            SubjectCountry = subject,
            RelatedCountry = ReferenceEquals(subject, country) ? null : country
        });

        _statusMessage =
            $"Request queued for {advisor.FullName}. It will enter their office when time advances.";
        Refresh();
    }

    private PlayerViewState BuildView()
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var ruler = country.Ruler;

        var metrics = new List<MetricCardView>
        {
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.Treasury,
                country.Id,
                "Treasury"),
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.Debt,
                country.Id,
                "Debt"),
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.ArmySize,
                country.Id,
                "Army strength"),
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.ArmyReadiness,
                country.Id,
                "Army readiness"),
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.GovernmentStability,
                country.Id,
                "Government stability"),
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.PoliticalBacking,
                country.Id,
                "Political backing")
        };

        var advisors = country.ActiveAdvisors
            .OrderBy(advisor => advisor.Position)
            .Select(advisor =>
            {
                var relationship = state.Relationships.GetOrCreate(advisor, ruler);
                var willingness = PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    advisor,
                    ruler);
                var threat = PoliticalCalculations.GetThreatScore(
                    state,
                    country,
                    advisor);

                return new AdvisorCardView(
                    advisor.Position?.ToString() ?? "Adviser",
                    advisor.FullName,
                    PlayerInformationFormatter.Level(advisor.Competence),
                    PlayerInformationFormatter.Trust(relationship.Trust),
                    PlayerInformationFormatter.Willingness(willingness),
                    PlayerInformationFormatter.Threat(threat));
            })
            .ToList();

        var pending = state.InformationRequests
            .Where(request => request.Status == InformationRequestStatus.Pending)
            .OrderBy(request => request.RequestedOn.Year)
            .ThenBy(request => request.RequestedOn.Month)
            .Select(request => new PendingInquiryView(
                request.Advisor.FullName,
                InformationSystem.FormatTopic(request.Topic),
                request.SubjectCountry.Name,
                request.RequestedOn.ToString()))
            .ToList();

        var intelligenceReports = state.AdvisorReports
            .TakeLast(30)
            .Reverse()
            .Select(report => new IntelligenceReportView(
                report.Id,
                report.Title,
                $"{report.Advisor.FullName} · {report.Advisor.Position}",
                report.Summary,
                $"Data {PlayerInformationFormatter.Age(
                    Math.Max(
                        0,
                        (state.Date.Year - report.DataAsOf.Year) * 12 +
                        state.Date.Month -
                        report.DataAsOf.Month))}",
                report.WasRequested ? "Requested inquiry" : "Adviser-initiated",
                report.Facts
                    .Select(fact => new ReportFactView(
                        PlayerInformationFormatter.MetricName(fact.Key.Metric),
                        PlayerInformationFormatter.Value(fact.Key.Metric, fact.Estimate),
                        $"±{PlayerInformationFormatter.Margin(fact.Key.Metric, fact.Margin)}",
                        $"{fact.ReportedConfidence}% apparent confidence"))
                    .ToList(),
                report.Caveats.ToList()))
            .ToList();

        var briefingEntries = new List<(int SortKey, int Priority, BriefingItemView View)>();

        foreach (var report in state.AdvisorReports.TakeLast(10))
        {
            var ageMonths = Math.Max(
                0,
                (state.Date.Year - report.DataAsOf.Year) * 12 +
                state.Date.Month -
                report.DataAsOf.Month);

            var meta =
                $"{report.Advisor.FullName} · {InformationSystem.FormatTopic(report.Topic)} · " +
                $"data {PlayerInformationFormatter.Age(ageMonths)}";

            briefingEntries.Add((
                DateKey(report.ProducedOn),
                2,
                new BriefingItemView(
                    "ADVISER REPORT",
                    report.Title,
                    report.Summary,
                    meta,
                    report.Caveats.Count > 0)));
        }

        foreach (var report in state.Reports.TakeLast(16))
        {
            var duplicatesAdvisorReport = state.AdvisorReports.Any(advisorReport =>
                advisorReport.ProducedOn == report.Date &&
                advisorReport.Title == report.Title);

            if (duplicatesAdvisorReport)
                continue;

            briefingEntries.Add((
                DateKey(report.Date),
                1,
                new BriefingItemView(
                    report.Category.ToString().ToUpperInvariant(),
                    report.Title,
                    report.Details,
                    report.Date.ToString(),
                    report.Category is ReportCategory.Military
                        or ReportCategory.Politics
                        or ReportCategory.Personal)));
        }

        var briefings = briefingEntries
            .OrderByDescending(entry => entry.SortKey)
            .ThenByDescending(entry => entry.Priority)
            .Take(12)
            .Select(entry => entry.View)
            .ToList();

        if (briefings.Count == 0)
        {
            briefings.Add(new BriefingItemView(
                "BRIEFING",
                "No new dispatches",
                "The administration has nothing new to place before the ruler.",
                state.Date.ToString(),
                false));
        }

        var foreignCountries = state.Countries
            .Where(candidate => !ReferenceEquals(candidate, country))
            .OrderBy(candidate => candidate.Name)
            .Select(candidate => new ForeignCountryOptionView(
                candidate.Id,
                candidate.Name))
            .ToList();

        return new PlayerViewState(
            country.Name,
            state.Date.ToString(),
            state.Player.Lineage.Name,
            ruler.FullName,
            $"Age {ruler.Age} · health {PlayerInformationFormatter.Health(ruler.Health)}",
            state.Player.HasLost,
            state.Player.LossReason,
            state.PendingOrders.Count,
            pending.Count,
            metrics,
            advisors,
            briefings,
            pending,
            intelligenceReports,
            foreignCountries,
            _statusMessage);
    }

    private static int DateKey(GameDate date) =>
        date.Year * 12 + date.Month;
}
