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

    public void SetTaxRate(double percent)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer);

        if (treasurer is null)
        {
            Refresh("The Treasury is vacant. There is nobody to implement a tax order.");
            return;
        }

        var clamped = Math.Clamp(percent, 0, 60) / 100.0;

        _simulation.SubmitOrder(new ChangeTaxOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = (decimal)clamped
        });

        Refresh($"Tax directive queued through {treasurer.FullName}: target {clamped:P0}.");
    }

    public void SetBudget(
        double armyPercent,
        double administrationPercent,
        double courtPercent)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer);

        if (treasurer is null)
        {
            Refresh("The Treasury is vacant. There is nobody to implement a budget order.");
            return;
        }

        _simulation.SubmitOrder(new SetBudgetOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetArmyFunding = (decimal)Math.Clamp(armyPercent / 100.0, 0.5, 1.5),
            TargetAdministrationFunding = (decimal)Math.Clamp(administrationPercent / 100.0, 0.5, 1.5),
            TargetCourtFunding = (decimal)Math.Clamp(courtPercent / 100.0, 0.5, 1.5)
        });

        Refresh($"Budget directive queued through {treasurer.FullName}.");
    }

    public void AppointAdvisor(int characterId, string positionName)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var candidate = country.PoliticalFigures.FirstOrDefault(character =>
            character.Id == characterId &&
            character.IsPoliticallyActive &&
            !ReferenceEquals(character, country.Ruler));

        if (candidate is null)
        {
            Refresh("That political figure is no longer available for appointment.");
            return;
        }

        if (!Enum.TryParse<Position>(positionName, ignoreCase: true, out var position))
        {
            Refresh("That government office does not exist.");
            return;
        }

        _simulation.SubmitOrder(new AppointAdvisorOrder
        {
            Issuer = country.Ruler,
            Recipient = candidate,
            IssuedOn = state.Date,
            Country = country,
            Position = position
        });

        Refresh($"Appointment of {candidate.FullName} as {position} queued.");
    }

    public void DismissAdvisor(string positionName)
    {
        var state = _simulation.State;
        var country = state.Player.Country;

        if (!Enum.TryParse<Position>(positionName, ignoreCase: true, out var position))
        {
            Refresh("That government office does not exist.");
            return;
        }

        var holder = country.GetOfficeHolder(position);

        if (holder is null)
        {
            Refresh($"The office of {position} is already vacant.");
            return;
        }

        _simulation.SubmitOrder(new DismissAdvisorOrder
        {
            Issuer = country.Ruler,
            Recipient = holder,
            IssuedOn = state.Date,
            Country = country
        });

        Refresh($"Dismissal of {holder.FullName} from {position} queued.");
    }

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

        var treasurer = country.GetOfficeHolder(Position.Treasurer);
        var treasurerObedience = treasurer is null
            ? "Vacant"
            : PlayerInformationFormatter.Willingness(
                PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    treasurer,
                    ruler));

        var government = new GovernmentPolicyView(
            (double)country.TaxRate * 100,
            (double)country.ArmyFunding * 100,
            (double)country.AdministrationFunding * 100,
            (double)country.CourtFunding * 100,
            treasurer?.FullName ?? "Vacant",
            treasurerObedience,
            "These are formal government settings, so the ruler knows what was officially enacted. " +
            "Their real effects still have to be learned through reports.");

        var offices = Enum.GetValues<Position>()
            .Select(position =>
            {
                var holder = country.GetOfficeHolder(position);

                if (holder is null)
                {
                    return new OfficeView(
                        position.ToString(),
                        "Vacant",
                        "—",
                        "—",
                        "—",
                        "—");
                }

                var relationship = state.Relationships.GetOrCreate(holder, ruler);
                var willingness = PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    holder,
                    ruler);
                var threat = PoliticalCalculations.GetThreatScore(
                    state,
                    country,
                    holder);

                return new OfficeView(
                    position.ToString(),
                    holder.FullName,
                    PlayerInformationFormatter.Level(holder.Competence),
                    PlayerInformationFormatter.Trust(relationship.Trust),
                    PlayerInformationFormatter.Willingness(willingness),
                    PlayerInformationFormatter.Threat(threat));
            })
            .ToList();

        var figures = country.PoliticalFigures
            .Where(character =>
                character.IsAlive &&
                !ReferenceEquals(character, ruler))
            .OrderByDescending(character =>
                PoliticalCalculations.GetThreatScore(
                    state,
                    country,
                    character))
            .Select(character =>
            {
                var relationship = state.Relationships.GetOrCreate(character, ruler);
                var willingness = PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    character,
                    ruler);
                var threat = PoliticalCalculations.GetThreatScore(
                    state,
                    country,
                    character);

                var strongestBases = Enum.GetValues<PowerBaseType>()
                    .OrderByDescending(powerBase =>
                        character.GetPowerBaseStanding(powerBase))
                    .Take(2)
                    .Select(powerBase =>
                        $"{FormatPowerBase(powerBase)} ({DescribeBacking(character.GetPowerBaseStanding(powerBase))})");

                return new CourtFigureView(
                    character.Id,
                    character.FullName,
                    character.Status == PoliticalStatus.Imprisoned
                        ? "Imprisoned"
                        : character.Status == PoliticalStatus.Exiled
                            ? "Exiled"
                            : character.Position?.ToString() ?? "Courtier",
                    PlayerInformationFormatter.Level(character.Competence),
                    DescribeAmbition(character.Ambition),
                    PlayerInformationFormatter.Level(character.Influence),
                    PlayerInformationFormatter.Trust(relationship.Trust),
                    PlayerInformationFormatter.Willingness(willingness),
                    PlayerInformationFormatter.Threat(threat),
                    string.Join(", ", strongestBases),
                    character.IsPoliticallyActive)
                ;
            })
            .ToList();

        var activeBloc = state.PoliticalBlocs.FirstOrDefault(bloc =>
            bloc.IsActive &&
            ReferenceEquals(bloc.Country, country));

        var opposition = activeBloc is null
            ? "No organised opposition bloc is clearly identified."
            : $"{activeBloc.Leader.FullName} leads organised opposition backed by " +
              string.Join(", ", activeBloc.PowerBases.Select(FormatPowerBase)) + ".";

        var activeDemands = state.PowerBaseDemands
            .Where(demand =>
                !demand.IsResolved &&
                ReferenceEquals(demand.Country, country))
            .OrderByDescending(demand => demand.EscalationLevel)
            .ToList();

        var pressure = activeDemands.Count == 0
            ? "No major organised demands are currently known."
            : string.Join("  ", activeDemands.Take(3).Select(demand =>
                $"{FormatPowerBase(demand.PowerBase)}: {DescribeDemand(demand)}"));

        var court = new CourtPoliticsView(
            offices,
            figures,
            opposition,
            pressure);

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
            government,
            court,
            _statusMessage);
    }

    private static string DescribeAmbition(int ambition) => ambition switch
    {
        >= 85 => "Extreme",
        >= 70 => "High",
        >= 50 => "Noticeable",
        >= 30 => "Modest",
        _ => "Low"
    };

    private static string DescribeBacking(int backing) => backing switch
    {
        >= 75 => "strong",
        >= 60 => "supportive",
        >= 45 => "uncertain",
        >= 30 => "hostile",
        _ => "very hostile"
    };

    private static string FormatPowerBase(PowerBaseType powerBase) => powerBase switch
    {
        PowerBaseType.RegionalElites => "Regional elites",
        PowerBaseType.RoyalFamily => "Royal family",
        _ => powerBase.ToString()
    };

    private static string DescribeDemand(PowerBaseDemand demand) => demand.Type switch
    {
        PowerBaseDemandType.LowerTaxes =>
            $"reduce taxes to {demand.TargetValue:P0} or lower",
        PowerBaseDemandType.RaiseArmyFunding =>
            $"raise army funding to at least {demand.TargetValue:P0}",
        PowerBaseDemandType.RaiseAdministrationFunding =>
            $"raise administration funding to at least {demand.TargetValue:P0}",
        PowerBaseDemandType.RaiseCourtFunding =>
            $"raise court funding to at least {demand.TargetValue:P0}",
        PowerBaseDemandType.EndWar =>
            "end the current war",
        _ => "make a political concession"
    };

    private static int DateKey(GameDate date) =>
        date.Year * 12 + date.Month;
}
