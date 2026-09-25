using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Information;

public sealed record InformationRequestResult(
    bool Accepted,
    bool Refused,
    string Title,
    string Details,
    InformationRequest? Request = null);

public static class InformationSystem
{
    public static void CaptureTruthSnapshot(
        GameState state,
        GameDate? date = null)
    {
        var snapshotDate = date ?? state.Date;

        state.InformationHistory.RemoveAll(existing =>
            existing.Date == snapshotDate);

        var snapshot = new InformationTruthSnapshot
        {
            Date = snapshotDate
        };

        foreach (var country in state.Countries)
        {
            foreach (var metric in new[]
                     {
                         InformationMetric.Population,
                         InformationMetric.Gdp,
                         InformationMetric.Treasury,
                         InformationMetric.Debt,
                         InformationMetric.MonthlyTaxRevenue,
                         InformationMetric.MonthlyTradeIncome,
                         InformationMetric.MonthlyExpenses,
                         InformationMetric.MonthlyBalance,
                         InformationMetric.AdministrativeEfficiency,
                         InformationMetric.ArmySize,
                         InformationMetric.ArmyReadiness,
                         InformationMetric.WarExhaustion,
                         InformationMetric.PublicUnrest,
                         InformationMetric.GovernmentStability,
                         InformationMetric.PoliticalBacking
                     })
            {
                var key = new InformationKey(metric, country.Id);
                snapshot.Values[key] =
                    GetCurrentTruthValue(state, metric, country, null);
            }
        }

        var playerCountry = state.Player.Country;

        foreach (var foreign in state.Countries.Where(candidate =>
                     !ReferenceEquals(candidate, playerCountry)))
        {
            foreach (var metric in new[]
                     {
                         InformationMetric.DiplomaticRelations,
                         InformationMetric.DiplomaticTrust,
                         InformationMetric.DiplomaticTension
                     })
            {
                var key = new InformationKey(
                    metric,
                    foreign.Id,
                    playerCountry.Id);

                snapshot.Values[key] =
                    GetCurrentTruthValue(
                        state,
                        metric,
                        foreign,
                        playerCountry.Id);
            }
        }

        foreach (var war in state.Wars.Where(war =>
                     war.Status == WarStatus.Active &&
                     war.IsParticipant(playerCountry)))
        {
            var enemy = war.OpponentOf(playerCountry);
            var key = new InformationKey(
                InformationMetric.WarScore,
                playerCountry.Id,
                enemy.Id);

            snapshot.Values[key] =
                GetCurrentTruthValue(
                    state,
                    InformationMetric.WarScore,
                    playerCountry,
                    enemy.Id);
        }

        state.InformationHistory.Add(snapshot);
        state.InformationHistory.Sort((a, b) =>
            CompareDates(a.Date, b.Date));

        if (state.InformationHistory.Count > 36)
            state.InformationHistory.RemoveRange(
                0,
                state.InformationHistory.Count - 36);
    }

    public static void SeedInitialBriefings(GameState state)
    {
        if (state.AdvisorReports.Count > 0 || state.Knowledge.Latest.Count > 0)
            return;

        if (state.InformationHistory.Count == 0)
        {
            CaptureTruthSnapshot(state, SubtractMonths(state.Date, 2));
            CaptureTruthSnapshot(state, SubtractMonths(state.Date, 1));
        }

        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer);
        var marshal = country.GetOfficeHolder(Position.Marshal);
        var chancellor = country.GetOfficeHolder(Position.Chancellor);

        if (treasurer is not null)
            DeliverInitialReport(state, treasurer, InformationTopic.Economy, country);

        if (marshal is not null)
            DeliverInitialReport(state, marshal, InformationTopic.Military, country);

        if (chancellor is not null)
        {
            DeliverInitialReport(
                state,
                chancellor,
                InformationTopic.DomesticPolitics,
                country);

            foreach (var foreign in state.Countries.Where(candidate =>
                         !ReferenceEquals(candidate, country)))
            {
                DeliverInitialReport(
                    state,
                    chancellor,
                    InformationTopic.ForeignAffairs,
                    foreign);
            }
        }
    }

    public static InformationRequestResult QueueRequestedReport(
        GameState state,
        Country issuingCountry,
        Character advisor,
        InformationTopic topic,
        Country subjectCountry,
        Country? relatedCountry = null)
    {
        if (!advisor.IsPoliticallyActive ||
            !issuingCountry.ContainsPoliticalFigure(advisor) ||
            ReferenceEquals(advisor, issuingCountry.Ruler) ||
            !IsAppropriateAdvisor(advisor, topic))
        {
            return new InformationRequestResult(
                Accepted: false,
                Refused: false,
                "Report request rejected",
                $"{advisor.FullName} is not currently in the office responsible for that report.");
        }

        if (!IsValidSubject(state, issuingCountry, topic, subjectCountry, relatedCountry))
        {
            return new InformationRequestResult(
                Accepted: false,
                Refused: false,
                "Report request rejected",
                "That adviser cannot procure a meaningful report on the requested subject.");
        }

        if (state.InformationRequests.Any(request =>
                request.Status == InformationRequestStatus.Pending &&
                request.Topic == topic &&
                ReferenceEquals(request.Advisor, advisor) &&
                ReferenceEquals(request.SubjectCountry, subjectCountry) &&
                ReferenceEquals(request.RelatedCountry, relatedCountry)))
        {
            return new InformationRequestResult(
                Accepted: false,
                Refused: false,
                "Report already in progress",
                $"{advisor.FullName} is already preparing that report.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            issuingCountry,
            advisor,
            issuingCountry.Ruler);

        var refusalChance = willingness switch
        {
            < 15 => 1.0,
            < 25 => 0.55,
            < 35 => 0.25,
            _ => 0.0
        };

        if (state.InformationRandom.NextDouble() < refusalChance)
        {
            return new InformationRequestResult(
                Accepted: false,
                Refused: true,
                $"{advisor.FullName} refuses the report request",
                $"{advisor.FullName} declines to devote their office to the requested inquiry.");
        }

        var delay = CalculateRequestDelay(
            state,
            issuingCountry,
            advisor,
            topic,
            subjectCountry,
            willingness);

        var request = new InformationRequest
        {
            Topic = topic,
            Advisor = advisor,
            SubjectCountry = subjectCountry,
            RelatedCountry = relatedCountry,
            RequestedOn = state.Date,
            RemainingMonths = delay,
            WillingnessAtRequest = willingness
        };

        state.InformationRequests.Add(request);

        var timeText = delay == 1
            ? "about one month"
            : $"roughly {delay} months";

        return new InformationRequestResult(
            Accepted: true,
            Refused: false,
            $"{advisor.FullName} begins the report",
            $"{advisor.FullName} agrees to investigate. They expect to report in {timeText}, " +
            "though difficult inquiries and reluctant officials can slip.",
            request);
    }

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var notifications = new List<SimulationReport>();

        ProcessRequests(state, notifications);
        GenerateProactiveReports(state, notifications);

        return notifications;
    }

    private static void ProcessRequests(
        GameState state,
        List<SimulationReport> notifications)
    {
        foreach (var request in state.InformationRequests.Where(request =>
                     request.Status == InformationRequestStatus.Pending).ToList())
        {
            // A request issued during this turn cannot also be completed during
            // the same turn. RemainingMonths measures full future turns of work.
            if (request.RequestedOn == state.Date)
                continue;

            if (!request.Advisor.IsPoliticallyActive ||
                !IsAppropriateAdvisor(request.Advisor, request.Topic))
            {
                request.Status = InformationRequestStatus.Cancelled;

                notifications.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Personal,
                    "Requested report cancelled",
                    $"{request.Advisor.FullName} can no longer complete the requested " +
                    $"{FormatTopic(request.Topic).ToLowerInvariant()} report."));
                continue;
            }

            request.RemainingMonths--;

            if (request.RemainingMonths > 0)
            {
                if (request.WillingnessAtRequest < 45 &&
                    state.InformationRandom.NextDouble() < 0.12)
                {
                    request.RemainingMonths++;
                }

                continue;
            }

            var report = ProduceReport(
                state,
                request.Advisor,
                request.Topic,
                request.SubjectCountry,
                request.RelatedCountry,
                wasRequested: true);

            request.Status = InformationRequestStatus.Completed;

            notifications.Add(new SimulationReport(
                state.Date,
                ReportCategory.Personal,
                report.Title,
                $"{request.Advisor.FullName} has delivered the requested report. " +
                "Its figures are estimates, not direct access to simulation truth."));
        }
    }

    private static void GenerateProactiveReports(
        GameState state,
        List<SimulationReport> notifications)
    {
        var country = state.Player.Country;

        foreach (var advisor in country.ActiveAdvisors)
        {
            var assignment = ChooseProactiveAssignment(state, country, advisor);

            if (assignment is null)
                continue;

            var (topic, subject, related, urgency) = assignment.Value;
            var willingness = PoliticalCalculations.GetOrderWillingness(
                state,
                country,
                advisor,
                country.Ruler);

            var chance =
                0.12 +
                advisor.Competence / 250.0 +
                willingness / 400.0 +
                urgency * 0.15;

            if (IsOppositionAligned(state, country, advisor))
                chance -= 0.15;

            if (state.InformationRandom.NextDouble() >= Math.Clamp(chance, 0.05, 0.92))
                continue;

            var report = ProduceReport(
                state,
                advisor,
                topic,
                subject,
                related,
                wasRequested: false);

            notifications.Add(new SimulationReport(
                state.Date,
                ReportCategory.Personal,
                report.Title,
                $"{advisor.FullName} chose to submit a new {FormatTopic(topic).ToLowerInvariant()} report."));
        }
    }

    private static (InformationTopic Topic, Country Subject, Country? Related, double Urgency)?
        ChooseProactiveAssignment(
            GameState state,
            Country country,
            Character advisor)
    {
        if (advisor.Position == Position.Treasurer)
        {
            var urgency = GetStalenessUrgency(
                state,
                InformationMetric.Treasury,
                country.Id,
                null,
                preferredAge: 3);

            if (country.LastMonthlyBalance < 0 || country.Debt > country.Gdp * 0.10m)
                urgency += 0.45;

            return urgency >= 0.75
                ? (InformationTopic.Economy, country, null, urgency)
                : null;
        }

        if (advisor.Position == Position.Marshal)
        {
            var war = state.Wars.FirstOrDefault(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(country));

            if (war is not null)
            {
                var enemy = war.OpponentOf(country);
                var foreignUrgency = GetStalenessUrgency(
                    state,
                    InformationMetric.ArmySize,
                    enemy.Id,
                    null,
                    preferredAge: 1) + 0.55;

                return (
                    InformationTopic.Military,
                    enemy,
                    country,
                    foreignUrgency);
            }

            var ownUrgency = GetStalenessUrgency(
                state,
                InformationMetric.ArmySize,
                country.Id,
                null,
                preferredAge: 2);

            return ownUrgency >= 0.75
                ? (InformationTopic.Military, country, null, ownUrgency)
                : null;
        }

        if (advisor.Position != Position.Chancellor)
            return null;

        var domesticUrgency = GetStalenessUrgency(
            state,
            InformationMetric.GovernmentStability,
            country.Id,
            null,
            preferredAge: 3);

        if (country.PublicUnrest >= 45 ||
            state.PowerBaseDemands.Any(demand =>
                !demand.IsResolved &&
                ReferenceEquals(demand.Country, country)) ||
            state.PoliticalBlocs.Any(bloc =>
                bloc.IsActive &&
                ReferenceEquals(bloc.Country, country)))
        {
            domesticUrgency += 0.50;
        }

        var bestForeign = state.Countries
            .Where(candidate => !ReferenceEquals(candidate, country))
            .Select(candidate => new
            {
                Country = candidate,
                Urgency = GetStalenessUrgency(
                    state,
                    InformationMetric.DiplomaticRelations,
                    candidate.Id,
                    country.Id,
                    preferredAge: 4) +
                    ForeignUrgencyBonus(state, country, candidate)
            })
            .OrderByDescending(entry => entry.Urgency)
            .FirstOrDefault();

        if (bestForeign is not null && bestForeign.Urgency > domesticUrgency)
        {
            return bestForeign.Urgency >= 0.75
                ? (
                    InformationTopic.ForeignAffairs,
                    bestForeign.Country,
                    country,
                    bestForeign.Urgency)
                : null;
        }

        return domesticUrgency >= 0.75
            ? (InformationTopic.DomesticPolitics, country, null, domesticUrgency)
            : null;
    }

    private static double ForeignUrgencyBonus(
        GameState state,
        Country player,
        Country foreign)
    {
        var relation = state.Diplomacy.GetOrCreate(player, foreign);
        var bonus = 0.0;

        if (relation.Tension >= 60)
            bonus += 0.35;

        if (state.DiplomaticProposals.Any(proposal =>
                proposal.Status == Diplomacy.DiplomaticProposalStatus.Pending &&
                ReferenceEquals(proposal.SourceCountry, foreign) &&
                ReferenceEquals(proposal.TargetCountry, player)))
        {
            bonus += 0.35;
        }

        return bonus;
    }

    private static double GetStalenessUrgency(
        GameState state,
        InformationMetric metric,
        string subjectId,
        string? relatedId,
        int preferredAge)
    {
        var known = state.Knowledge.Get(metric, subjectId, relatedId);

        if (known is null)
            return 1.5;

        return known.AgeInMonths(state.Date) / (double)Math.Max(1, preferredAge);
    }

    private static int CalculateRequestDelay(
        GameState state,
        Country issuingCountry,
        Character advisor,
        InformationTopic topic,
        Country subjectCountry,
        double willingness)
    {
        var foreign = !ReferenceEquals(subjectCountry, issuingCountry);

        var delay = topic switch
        {
            InformationTopic.Economy => 1,
            InformationTopic.DomesticPolitics => 1,
            InformationTopic.Military when foreign => 2,
            InformationTopic.Military => 1,
            InformationTopic.ForeignAffairs => 2,
            _ => 2
        };

        if (advisor.Competence < 55)
            delay++;

        if (willingness < 50)
            delay++;

        if (willingness < 30)
            delay++;

        if (foreign && state.InformationRandom.NextDouble() < 0.35)
            delay++;

        if (advisor.Competence >= 85 && state.InformationRandom.NextDouble() < 0.40)
            delay--;

        return Math.Clamp(delay, 1, 6);
    }

    private static AdvisorIntelligenceReport ProduceReport(
        GameState state,
        Character advisor,
        InformationTopic topic,
        Country subjectCountry,
        Country? relatedCountry,
        bool wasRequested)
    {
        var playerCountry = state.Player.Country;
        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            playerCountry,
            advisor,
            playerCountry.Ruler);

        var dataLag = CalculateDataLag(
            state,
            advisor,
            topic,
            subjectCountry,
            playerCountry,
            wasRequested);

        var asOf = SubtractMonths(state.Date, dataLag);
        var title = BuildTitle(topic, subjectCountry, advisor);
        var report = new AdvisorIntelligenceReport
        {
            Topic = topic,
            Advisor = advisor,
            SubjectCountry = subjectCountry,
            RelatedCountry = relatedCountry,
            ProducedOn = state.Date,
            DataAsOf = asOf,
            WasRequested = wasRequested,
            Title = title,
            Summary = BuildSummary(topic, subjectCountry, dataLag)
        };

        var facts = BuildFacts(
            state,
            advisor,
            topic,
            subjectCountry,
            relatedCountry,
            willingness,
            asOf,
            wasRequested);

        report.Facts.AddRange(facts);

        if (report.Facts.Count == 0)
        {
            report.Caveats.Add(
                "The report contains no usable quantitative findings despite the tasking.");
        }

        foreach (var fact in report.Facts)
        {
            var previous = state.Knowledge.Get(
                fact.Key.Metric,
                fact.Key.SubjectCountryId,
                fact.Key.RelatedCountryId);

            if (previous is not null)
            {
                var disagreement = Math.Abs(fact.Estimate - previous.Estimate);
                var tolerance = Math.Max(1, Math.Max(fact.Margin, previous.Margin));

                if (disagreement > tolerance * 1.25)
                {
                    report.Caveats.Add(
                        $"{FormatMetricForCaveat(fact.Key.Metric)} differs materially from " +
                        $"the previous estimate supplied by {previous.SourceAdvisorName}. " +
                        "The available reports do not establish which estimate is closer to the truth.");
                }
            }

            state.Knowledge.Update(fact);
        }

        state.AdvisorReports.Add(report);
        return report;
    }

    private static IEnumerable<KnownInformation> BuildFacts(
        GameState state,
        Character advisor,
        InformationTopic topic,
        Country subjectCountry,
        Country? relatedCountry,
        double willingness,
        GameDate asOf,
        bool wasRequested)
    {
        var playerCountry = state.Player.Country;
        var metrics = topic switch
        {
            InformationTopic.Economy =>
            [
                InformationMetric.Population,
                InformationMetric.Gdp,
                InformationMetric.Treasury,
                InformationMetric.Debt,
                InformationMetric.AdministrativeEfficiency
            ],

            InformationTopic.Military =>
            [
                InformationMetric.ArmySize,
                InformationMetric.ArmyReadiness,
                InformationMetric.WarExhaustion
            ],

            InformationTopic.ForeignAffairs =>
            [
                InformationMetric.Population,
                InformationMetric.Gdp,
                InformationMetric.DiplomaticRelations,
                InformationMetric.DiplomaticTrust,
                InformationMetric.DiplomaticTension
            ],

            InformationTopic.DomesticPolitics =>
            [
                InformationMetric.PublicUnrest,
                InformationMetric.GovernmentStability,
                InformationMetric.PoliticalBacking
            ],

            _ => Array.Empty<InformationMetric>()
        };

        foreach (var metric in metrics)
        {
            if (ShouldOmitFact(
                    state,
                    advisor,
                    willingness,
                    metric,
                    wasRequested))
            {
                continue;
            }

            var relatedId = metric is InformationMetric.DiplomaticRelations
                or InformationMetric.DiplomaticTrust
                or InformationMetric.DiplomaticTension
                ? playerCountry.Id
                : null;

            yield return CreateFact(
                state,
                advisor,
                metric,
                subjectCountry,
                relatedId,
                willingness,
                asOf,
                wasRequested);
        }

        if (topic == InformationTopic.Military &&
            !ReferenceEquals(subjectCountry, playerCountry))
        {
            var war = state.Wars.FirstOrDefault(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(playerCountry) &&
                war.IsParticipant(subjectCountry));

            if (war is not null)
            {
                yield return CreateFact(
                    state,
                    advisor,
                    InformationMetric.WarScore,
                    playerCountry,
                    subjectCountry.Id,
                    willingness,
                    asOf,
                    wasRequested);
            }
        }
    }

    private static bool ShouldOmitFact(
        GameState state,
        Character advisor,
        double willingness,
        InformationMetric metric,
        bool wasRequested)
    {
        if (willingness >= 50)
            return false;

        var oppositionAligned =
            IsOppositionAligned(state, state.Player.Country, advisor);

        var chance =
            Math.Max(0, 45 - willingness) / 100.0 * 0.28 +
            (oppositionAligned ? 0.06 : 0);

        if (wasRequested)
            chance *= 0.65;

        if (metric is InformationMetric.Population or InformationMetric.Gdp)
            chance *= 0.50;

        return state.InformationRandom.NextDouble() <
               Math.Clamp(chance, 0, 0.22);
    }

    private static KnownInformation CreateFact(
        GameState state,
        Character advisor,
        InformationMetric metric,
        Country subjectCountry,
        string? relatedCountryId,
        double willingness,
        GameDate asOf,
        bool wasRequested)
    {
        var truth = GetTruthValueAtDate(
            state,
            metric,
            subjectCountry,
            relatedCountryId,
            asOf);

        var foreign = !ReferenceEquals(subjectCountry, state.Player.Country);
        var fog = BaseFog(metric, foreign);
        var institutionalQuality = metric switch
        {
            InformationMetric.Treasury or
            InformationMetric.Debt or
            InformationMetric.MonthlyTaxRevenue or
            InformationMetric.MonthlyTradeIncome or
            InformationMetric.MonthlyExpenses or
            InformationMetric.MonthlyBalance or
            InformationMetric.AdministrativeEfficiency =>
                (double)state.Player.Country.AdministrativeEfficiency * 100,

            InformationMetric.ArmySize or
            InformationMetric.ArmyReadiness or
            InformationMetric.WarExhaustion or
            InformationMetric.WarScore =>
                state.Player.Country.ArmyReadiness,

            _ => state.Player.Country.Government.Stability
        };

        var quality = Math.Clamp(
            advisor.Competence * 0.58 +
            willingness * 0.22 +
            institutionalQuality * 0.20,
            0,
            100);

        var errorScale =
            fog +
            (100 - quality) / 100.0 * 0.34;

        if (wasRequested)
            errorScale *= 0.88;

        var randomNoise =
            (state.InformationRandom.NextDouble() +
             state.InformationRandom.NextDouble() -
             1.0) *
            errorScale;

        var oppositionAligned =
            IsOppositionAligned(state, state.Player.Country, advisor);

        var lieChance =
            Math.Max(0, 42 - willingness) / 100.0 * 0.75 +
            (oppositionAligned ? 0.14 : 0) +
            (advisor.Ambition >= 80 ? 0.04 : 0);

        if (wasRequested && willingness < 45)
            lieChance += 0.04;

        var deliberate = state.InformationRandom.NextDouble() <
                         Math.Clamp(lieChance, 0, 0.55);

        var distortion = 0.0;

        if (deliberate)
        {
            var magnitude =
                0.08 +
                state.InformationRandom.NextDouble() * 0.18 +
                Math.Max(0, 50 - willingness) / 250.0;

            var direction = oppositionAligned
                ? OppositionDistortionDirection(metric, foreign)
                : (state.InformationRandom.NextDouble() < 0.5 ? -1 : 1);

            distortion = magnitude * direction;
        }

        var rawEstimate = PerturbValue(
            metric,
            truth,
            randomNoise,
            distortion);

        var estimate = RoundEstimate(metric, rawEstimate);
        var margin = RoundMargin(
            metric,
            EstimateMargin(metric, truth, errorScale));

        var reportedConfidence = (int)Math.Round(Math.Clamp(
            advisor.Competence * 0.62 +
            willingness * 0.13 +
            (1 - fog) * 25,
            20,
            96));

        return new KnownInformation
        {
            Key = new InformationKey(
                metric,
                subjectCountry.Id,
                relatedCountryId),
            Estimate = estimate,
            Margin = margin,
            ReportedConfidence = reportedConfidence,
            AsOf = asOf,
            ReceivedOn = state.Date,
            SourceAdvisorId = advisor.Id,
            SourceAdvisorName = advisor.FullName,
            WasRequested = wasRequested
        };
    }

    private static double GetTruthValueAtDate(
        GameState state,
        InformationMetric metric,
        Country subject,
        string? relatedCountryId,
        GameDate asOf)
    {
        var key = new InformationKey(
            metric,
            subject.Id,
            relatedCountryId);

        var snapshot = state.InformationHistory
            .Where(candidate => CompareDates(candidate.Date, asOf) <= 0)
            .OrderByDescending(candidate => candidate.Date.Year)
            .ThenByDescending(candidate => candidate.Date.Month)
            .FirstOrDefault(candidate => candidate.Values.ContainsKey(key));

        if (snapshot is not null)
            return snapshot.Values[key];

        return GetCurrentTruthValue(
            state,
            metric,
            subject,
            relatedCountryId);
    }

    private static double GetCurrentTruthValue(
        GameState state,
        InformationMetric metric,
        Country subject,
        string? relatedCountryId)
    {
        return metric switch
        {
            InformationMetric.Population => subject.Population,
            InformationMetric.Gdp => (double)subject.Gdp,
            InformationMetric.Treasury => (double)subject.Treasury,
            InformationMetric.Debt => (double)subject.Debt,
            InformationMetric.MonthlyTaxRevenue => (double)subject.LastMonthlyTaxRevenue,
            InformationMetric.MonthlyTradeIncome => (double)subject.LastMonthlyTradeIncome,
            InformationMetric.MonthlyExpenses => (double)subject.LastMonthlyExpenses,
            InformationMetric.MonthlyBalance => (double)subject.LastMonthlyBalance,
            InformationMetric.AdministrativeEfficiency =>
                (double)subject.AdministrativeEfficiency * 100,
            InformationMetric.ArmySize => subject.ArmySize,
            InformationMetric.ArmyReadiness => subject.ArmyReadiness,
            InformationMetric.WarExhaustion => subject.WarExhaustion,
            InformationMetric.PublicUnrest => subject.PublicUnrest,
            InformationMetric.GovernmentStability => subject.Government.Stability,
            InformationMetric.PoliticalBacking =>
                PoliticalCalculations.GetPowerBaseInfluence(subject, subject.Ruler),
            InformationMetric.DiplomaticRelations =>
                GetRelation(state, subject, relatedCountryId).Relations,
            InformationMetric.DiplomaticTrust =>
                GetRelation(state, subject, relatedCountryId).Trust,
            InformationMetric.DiplomaticTension =>
                GetRelation(state, subject, relatedCountryId).Tension,
            InformationMetric.WarScore =>
                GetPlayerWarScore(state, subject, relatedCountryId),
            _ => 0
        };
    }

    private static Diplomacy.DiplomaticRelation GetRelation(
        GameState state,
        Country subject,
        string? relatedCountryId)
    {
        var related = relatedCountryId is null
            ? state.Player.Country
            : state.FindCountry(relatedCountryId) ?? state.Player.Country;

        return state.Diplomacy.GetOrCreate(subject, related);
    }

    private static double GetPlayerWarScore(
        GameState state,
        Country playerCountry,
        string? enemyId)
    {
        var enemy = enemyId is null
            ? null
            : state.FindCountry(enemyId);

        if (enemy is null)
            return 0;

        var war = state.Wars.FirstOrDefault(war =>
            war.Status == WarStatus.Active &&
            war.IsParticipant(playerCountry) &&
            war.IsParticipant(enemy));

        if (war is null)
            return 0;

        return ReferenceEquals(playerCountry, war.Attacker)
            ? war.WarScore
            : -war.WarScore;
    }

    private static double BaseFog(
        InformationMetric metric,
        bool foreign)
    {
        if (metric is InformationMetric.DiplomaticRelations
            or InformationMetric.DiplomaticTrust
            or InformationMetric.DiplomaticTension)
        {
            return 0.09;
        }

        if (metric == InformationMetric.WarScore)
            return 0.12;

        if (foreign &&
            metric is InformationMetric.ArmySize
                or InformationMetric.ArmyReadiness
                or InformationMetric.WarExhaustion)
        {
            return 0.18;
        }

        if (foreign && metric == InformationMetric.Population)
            return 0.13;

        if (foreign && metric == InformationMetric.Gdp)
            return 0.17;

        return metric switch
        {
            InformationMetric.Population => 0.08,
            InformationMetric.Gdp => 0.10,
            InformationMetric.Treasury => 0.05,
            InformationMetric.Debt => 0.04,
            InformationMetric.MonthlyTaxRevenue => 0.06,
            InformationMetric.MonthlyTradeIncome => 0.09,
            InformationMetric.MonthlyExpenses => 0.06,
            InformationMetric.MonthlyBalance => 0.08,
            InformationMetric.AdministrativeEfficiency => 0.07,
            InformationMetric.ArmySize => 0.05,
            InformationMetric.ArmyReadiness => 0.07,
            InformationMetric.WarExhaustion => 0.08,
            InformationMetric.PublicUnrest => 0.11,
            InformationMetric.GovernmentStability => 0.10,
            InformationMetric.PoliticalBacking => 0.13,
            _ => 0.10
        };
    }

    private static int OppositionDistortionDirection(
        InformationMetric metric,
        bool foreign)
    {
        if (foreign &&
            metric is InformationMetric.ArmySize
                or InformationMetric.ArmyReadiness)
        {
            return 1;
        }

        return metric switch
        {
            InformationMetric.Debt => 1,
            InformationMetric.MonthlyExpenses => 1,
            InformationMetric.WarExhaustion => 1,
            InformationMetric.PublicUnrest => 1,
            InformationMetric.DiplomaticTension => 1,

            InformationMetric.Gdp => -1,
            InformationMetric.Treasury => -1,
            InformationMetric.MonthlyTaxRevenue => -1,
            InformationMetric.MonthlyTradeIncome => -1,
            InformationMetric.MonthlyBalance => -1,
            InformationMetric.AdministrativeEfficiency => -1,
            InformationMetric.ArmySize => -1,
            InformationMetric.ArmyReadiness => -1,
            InformationMetric.GovernmentStability => -1,
            InformationMetric.PoliticalBacking => -1,
            InformationMetric.DiplomaticRelations => -1,
            InformationMetric.DiplomaticTrust => -1,
            InformationMetric.WarScore => -1,
            _ => -1
        };
    }

    private static double PerturbValue(
        InformationMetric metric,
        double truth,
        double randomNoise,
        double distortion)
    {
        if (metric is InformationMetric.ArmyReadiness
            or InformationMetric.WarExhaustion
            or InformationMetric.PublicUnrest
            or InformationMetric.GovernmentStability
            or InformationMetric.PoliticalBacking
            or InformationMetric.AdministrativeEfficiency)
        {
            return Math.Clamp(
                truth + (randomNoise + distortion) * 100,
                0,
                100);
        }

        if (metric is InformationMetric.DiplomaticRelations
            or InformationMetric.WarScore)
        {
            return Math.Clamp(
                truth + (randomNoise + distortion) * 100,
                -100,
                100);
        }

        if (metric is InformationMetric.DiplomaticTrust
            or InformationMetric.DiplomaticTension)
        {
            return Math.Clamp(
                truth + (randomNoise + distortion) * 100,
                0,
                100);
        }

        if (metric == InformationMetric.MonthlyBalance)
        {
            var scale = Math.Max(10_000, Math.Abs(truth));
            return truth + scale * (randomNoise + distortion);
        }

        return Math.Max(
            0,
            truth * (1 + randomNoise + distortion));
    }

    private static double EstimateMargin(
        InformationMetric metric,
        double truth,
        double errorScale)
    {
        if (metric is InformationMetric.ArmyReadiness
            or InformationMetric.WarExhaustion
            or InformationMetric.PublicUnrest
            or InformationMetric.GovernmentStability
            or InformationMetric.PoliticalBacking
            or InformationMetric.AdministrativeEfficiency
            or InformationMetric.DiplomaticRelations
            or InformationMetric.DiplomaticTrust
            or InformationMetric.DiplomaticTension
            or InformationMetric.WarScore)
        {
            return Math.Max(3, errorScale * 75);
        }

        return Math.Max(
            MetricRoundUnit(metric),
            Math.Max(Math.Abs(truth), MetricRoundUnit(metric) * 2) *
            Math.Max(0.04, errorScale * 0.75));
    }

    private static double RoundEstimate(
        InformationMetric metric,
        double value)
    {
        var unit = MetricRoundUnit(metric);

        if (unit <= 1)
            return Math.Round(value);

        return Math.Round(value / unit) * unit;
    }

    private static double RoundMargin(
        InformationMetric metric,
        double value)
    {
        var unit = Math.Max(1, MetricRoundUnit(metric));
        return Math.Max(unit, Math.Round(value / unit) * unit);
    }

    private static double MetricRoundUnit(InformationMetric metric)
    {
        return metric switch
        {
            InformationMetric.Population => 10_000,
            InformationMetric.Gdp => 1_000_000,
            InformationMetric.Treasury => 10_000,
            InformationMetric.Debt => 10_000,
            InformationMetric.MonthlyTaxRevenue => 1_000,
            InformationMetric.MonthlyTradeIncome => 1_000,
            InformationMetric.MonthlyExpenses => 1_000,
            InformationMetric.MonthlyBalance => 1_000,
            InformationMetric.ArmySize => 100,
            _ => 1
        };
    }

    private static int CalculateDataLag(
        GameState state,
        Character advisor,
        InformationTopic topic,
        Country subject,
        Country playerCountry,
        bool wasRequested)
    {
        var foreign = !ReferenceEquals(subject, playerCountry);

        var lag = topic switch
        {
            InformationTopic.Economy => 0,
            InformationTopic.DomesticPolitics => 0,
            InformationTopic.Military when foreign => 1,
            InformationTopic.Military => 0,
            InformationTopic.ForeignAffairs => 1,
            _ => 1
        };

        if (advisor.Competence < 50)
            lag++;

        if (foreign && state.InformationRandom.NextDouble() < 0.35)
            lag++;

        if (wasRequested && advisor.Competence >= 75)
            lag--;

        return Math.Clamp(lag, 0, 4);
    }

    private static void DeliverInitialReport(
        GameState state,
        Character advisor,
        InformationTopic topic,
        Country subject)
    {
        var related = topic == InformationTopic.ForeignAffairs
            ? state.Player.Country
            : null;

        var report = ProduceInitialReport(
            state,
            advisor,
            topic,
            subject,
            related);

        state.AdvisorReports.Add(report);

        foreach (var fact in report.Facts)
            state.Knowledge.Update(fact);
    }

    private static AdvisorIntelligenceReport ProduceInitialReport(
        GameState state,
        Character advisor,
        InformationTopic topic,
        Country subject,
        Country? related)
    {
        var asOf = SubtractMonths(state.Date, topic is InformationTopic.ForeignAffairs ? 2 : 1);
        var report = new AdvisorIntelligenceReport
        {
            Topic = topic,
            Advisor = advisor,
            SubjectCountry = subject,
            RelatedCountry = related,
            ProducedOn = state.Date,
            DataAsOf = asOf,
            WasRequested = false,
            Title = $"Inherited {FormatTopic(topic).ToLowerInvariant()} briefing",
            Summary = "This is the administration's inherited working estimate at the start of play."
        };

        var metrics = topic switch
        {
            InformationTopic.Economy =>
            [
                InformationMetric.Population,
                InformationMetric.Gdp,
                InformationMetric.Treasury,
                InformationMetric.Debt,
                InformationMetric.MonthlyTaxRevenue,
                InformationMetric.MonthlyTradeIncome,
                InformationMetric.MonthlyExpenses,
                InformationMetric.MonthlyBalance,
                InformationMetric.AdministrativeEfficiency
            ],
            InformationTopic.Military =>
            [
                InformationMetric.ArmySize,
                InformationMetric.ArmyReadiness,
                InformationMetric.WarExhaustion
            ],
            InformationTopic.ForeignAffairs =>
            [
                InformationMetric.Population,
                InformationMetric.Gdp,
                InformationMetric.DiplomaticRelations,
                InformationMetric.DiplomaticTrust,
                InformationMetric.DiplomaticTension
            ],
            InformationTopic.DomesticPolitics =>
            [
                InformationMetric.PublicUnrest,
                InformationMetric.GovernmentStability,
                InformationMetric.PoliticalBacking
            ],
            _ => Array.Empty<InformationMetric>()
        };

        foreach (var metric in metrics)
        {
            var relatedId = metric is InformationMetric.DiplomaticRelations
                or InformationMetric.DiplomaticTrust
                or InformationMetric.DiplomaticTension
                ? state.Player.Country.Id
                : null;

            var truth = GetTruthValueAtDate(
                state,
                metric,
                subject,
                relatedId,
                asOf);
            var fog = BaseFog(metric, !ReferenceEquals(subject, state.Player.Country));
            var estimate = RoundEstimate(metric, truth);
            var margin = RoundMargin(
                metric,
                EstimateMargin(metric, truth, Math.Max(0.08, fog)));

            report.Facts.Add(new KnownInformation
            {
                Key = new InformationKey(metric, subject.Id, relatedId),
                Estimate = estimate,
                Margin = margin,
                ReportedConfidence = Math.Clamp(55 + advisor.Competence / 3, 55, 88),
                AsOf = asOf,
                ReceivedOn = state.Date,
                SourceAdvisorId = advisor.Id,
                SourceAdvisorName = advisor.FullName,
                WasRequested = false
            });
        }

        return report;
    }

    private static bool IsAppropriateAdvisor(
        Character advisor,
        InformationTopic topic)
    {
        return topic switch
        {
            InformationTopic.Economy =>
                advisor.Position == Position.Treasurer,
            InformationTopic.Military =>
                advisor.Position == Position.Marshal,
            InformationTopic.ForeignAffairs or
            InformationTopic.DomesticPolitics =>
                advisor.Position == Position.Chancellor,
            _ => false
        };
    }

    private static bool IsValidSubject(
        GameState state,
        Country issuingCountry,
        InformationTopic topic,
        Country subject,
        Country? related)
    {
        return topic switch
        {
            InformationTopic.Economy =>
                ReferenceEquals(subject, issuingCountry),
            InformationTopic.DomesticPolitics =>
                ReferenceEquals(subject, issuingCountry),
            InformationTopic.Military =>
                state.Countries.Contains(subject),
            InformationTopic.ForeignAffairs =>
                !ReferenceEquals(subject, issuingCountry),
            _ => false
        };
    }

    private static bool IsOppositionAligned(
        GameState state,
        Country country,
        Character advisor)
    {
        return state.PoliticalBlocs.Any(bloc =>
            bloc.IsActive &&
            ReferenceEquals(bloc.Country, country) &&
            (ReferenceEquals(bloc.Leader, advisor) ||
             bloc.MemberIds.Contains(advisor.Id)));
    }

    private static string BuildTitle(
        InformationTopic topic,
        Country subject,
        Character advisor)
    {
        return topic switch
        {
            InformationTopic.Economy =>
                $"{advisor.FullName}'s fiscal report",
            InformationTopic.Military =>
                $"{advisor.FullName}'s military report on {subject.Name}",
            InformationTopic.ForeignAffairs =>
                $"{advisor.FullName}'s assessment of {subject.Name}",
            InformationTopic.DomesticPolitics =>
                $"{advisor.FullName}'s domestic political report",
            _ => $"{advisor.FullName}'s report"
        };
    }

    private static string BuildSummary(
        InformationTopic topic,
        Country subject,
        int dataLag)
    {
        var freshness = dataLag == 0
            ? "mostly current observations"
            : dataLag == 1
                ? "information roughly a month old"
                : $"information assembled over roughly the previous {dataLag} months";

        return topic switch
        {
            InformationTopic.Economy =>
                $"An estimate of the government's finances and economy using {freshness}.",
            InformationTopic.Military =>
                $"An estimate of military strength and condition for {subject.Name}, using {freshness}.",
            InformationTopic.ForeignAffairs =>
                $"An assessment of relations with {subject.Name}, using {freshness}.",
            InformationTopic.DomesticPolitics =>
                $"An assessment of domestic stability and political backing using {freshness}.",
            _ => $"A report based on {freshness}."
        };
    }

    private static string FormatMetricForCaveat(InformationMetric metric)
    {
        return metric switch
        {
            InformationMetric.Gdp => "GDP",
            InformationMetric.ArmySize => "Army strength",
            InformationMetric.ArmyReadiness => "Army readiness",
            InformationMetric.GovernmentStability => "Government stability",
            InformationMetric.PoliticalBacking => "Political backing",
            InformationMetric.DiplomaticRelations => "Diplomatic relations",
            InformationMetric.DiplomaticTrust => "Diplomatic trust",
            InformationMetric.DiplomaticTension => "Diplomatic tension",
            InformationMetric.WarScore => "Campaign position",
            _ => metric.ToString()
        };
    }

    public static string FormatTopic(InformationTopic topic)
    {
        return topic switch
        {
            InformationTopic.ForeignAffairs => "Foreign affairs",
            InformationTopic.DomesticPolitics => "Domestic politics",
            _ => topic.ToString()
        };
    }

    private static int CompareDates(GameDate first, GameDate second)
    {
        var year = first.Year.CompareTo(second.Year);

        return year != 0
            ? year
            : first.Month.CompareTo(second.Month);
    }

    private static GameDate SubtractMonths(GameDate date, int months)
    {
        var total = (date.Year - 1) * 12 + (date.Month - 1) - months;
        total = Math.Max(0, total);
        return new GameDate(total / 12 + 1, total % 12 + 1);
    }
}
