using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
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
    private GameSimulation _simulation;
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

    private static string SaveDirectory
    {
        get
        {
            var root = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(root))
                root = AppContext.BaseDirectory;

            return Path.Combine(root, "LeaderGame", "saves");
        }
    }

    public static string DefaultSavePath =>
        Path.Combine(SaveDirectory, "campaign.json");

    public static string AutosavePath =>
        Path.Combine(SaveDirectory, "autosave.json");

    public bool DefaultSaveExists => File.Exists(DefaultSavePath);

    public bool AutosaveExists => File.Exists(AutosavePath);

    public void SaveDefault()
    {
        try
        {
            GameSaveService.SaveToFile(
                _simulation.State,
                DefaultSavePath);

            Refresh(
                $"Campaign saved at {_simulation.State.Date}.");
        }
        catch (Exception exception)
        {
            Refresh($"Save failed: {exception.Message}");
        }
    }

    public void LoadDefault()
    {
        if (!DefaultSaveExists)
        {
            Refresh("No saved campaign exists in the default slot.");
            return;
        }

        try
        {
            var state = GameSaveService.LoadFromFile(
                DefaultSavePath);

            _simulation = new GameSimulation(state);
            _statusMessage =
                $"Campaign loaded from {state.Date}.";
            View = BuildView();
        }
        catch (Exception exception)
        {
            Refresh($"Load failed: {exception.Message}");
        }
    }

    public void LoadAutosave()
    {
        if (!AutosaveExists)
        {
            Refresh("No autosave exists yet.");
            return;
        }

        try
        {
            var state = GameSaveService.LoadFromFile(
                AutosavePath);

            _simulation = new GameSimulation(state);
            _statusMessage =
                $"Autosave recovered from {state.Date}.";
            View = BuildView();
        }
        catch (Exception exception)
        {
            Refresh($"Autosave recovery failed: {exception.Message}");
        }
    }

    public void AdvanceMonth()
    {
        if (_simulation.State.Player.HasLost)
            return;

        _simulation.AdvanceMonth();

        try
        {
            GameSaveService.SaveToFile(
                _simulation.State,
                AutosavePath);

            _statusMessage =
                $"Time advanced to {_simulation.State.Date}. Autosaved.";
        }
        catch (Exception exception)
        {
            _statusMessage =
                $"Time advanced to {_simulation.State.Date}. Autosave failed: {exception.Message}";
        }

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

    public void ImproveRelations(string countryId)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var target = state.FindCountry(countryId);
        var chancellor = country.GetOfficeHolder(Position.Chancellor);

        if (target is null || ReferenceEquals(target, country))
        {
            Refresh("Choose another country for diplomatic outreach.");
            return;
        }

        if (chancellor is null)
        {
            Refresh("The Chancellery is vacant. There is nobody to conduct the mission.");
            return;
        }

        _simulation.SubmitOrder(new ImproveRelationsOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target
        });

        Refresh($"Diplomatic outreach to {target.Name} queued through {chancellor.FullName}.");
    }

    public void ToggleTradeAgreement(string countryId)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var target = state.FindCountry(countryId);
        var chancellor = country.GetOfficeHolder(Position.Chancellor);

        if (target is null || ReferenceEquals(target, country))
        {
            Refresh("Choose another country for trade policy.");
            return;
        }

        if (chancellor is null)
        {
            Refresh("The Chancellery is vacant. There is nobody to conduct trade diplomacy.");
            return;
        }

        var relation = state.Diplomacy.GetOrCreate(country, target);

        if (relation.HasTradeAgreement)
        {
            _simulation.SubmitOrder(new EndTradeAgreementOrder
            {
                Issuer = country.Ruler,
                Recipient = chancellor,
                IssuedOn = state.Date,
                SourceCountry = country,
                TargetCountry = target
            });

            Refresh($"Termination of the trade agreement with {target.Name} queued.");
            return;
        }

        _simulation.SubmitOrder(new NegotiateTradeAgreementOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target
        });

        Refresh($"Trade negotiations with {target.Name} queued through {chancellor.FullName}.");
    }

    public void DeclareWar(string countryId)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var target = state.FindCountry(countryId);
        var chancellor = country.GetOfficeHolder(Position.Chancellor);

        if (target is null || ReferenceEquals(target, country))
        {
            Refresh("Choose another country.");
            return;
        }

        if (!country.IsNeighbor(target))
        {
            Refresh($"{target.Name} is not currently a valid neighbouring war target.");
            return;
        }

        if (state.Wars.Any(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(country) &&
                war.IsParticipant(target)))
        {
            Refresh($"{country.Name} is already at war with {target.Name}.");
            return;
        }

        if (chancellor is null)
        {
            Refresh("The Chancellery is vacant. There is nobody to deliver the declaration.");
            return;
        }

        _simulation.SubmitOrder(new DeclareWarOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target
        });

        Refresh($"Declaration of war on {target.Name} queued through {chancellor.FullName}.");
    }

    public void RespondToDiplomaticProposal(Guid proposalId, bool accept)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var chancellor = country.GetOfficeHolder(Position.Chancellor);
        var proposal = state.DiplomaticProposals.FirstOrDefault(candidate =>
            candidate.Id == proposalId &&
            candidate.Status == DiplomaticProposalStatus.Pending &&
            ReferenceEquals(candidate.TargetCountry, country));

        if (proposal is null)
        {
            Refresh("That diplomatic proposal is no longer pending.");
            return;
        }

        if (chancellor is null)
        {
            Refresh("The Chancellery is vacant. There is nobody to deliver a formal response.");
            return;
        }

        _simulation.SubmitOrder(new RespondToDiplomaticProposalOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Proposal = proposal,
            Accept = accept
        });

        Refresh(
            $"{(accept ? "Acceptance" : "Rejection")} of {proposal.SourceCountry.Name}'s proposal queued.");
    }

    public void SetWarStance(Guid warId, string stanceName)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal);
        var war = state.Wars.FirstOrDefault(candidate =>
            candidate.Id == warId &&
            candidate.Status == WarStatus.Active &&
            candidate.IsParticipant(country));

        if (war is null)
        {
            Refresh("That campaign is no longer active.");
            return;
        }

        if (marshal is null)
        {
            Refresh("The Marshal's office is vacant. There is nobody to receive a campaign directive.");
            return;
        }

        if (!Enum.TryParse<WarStance>(stanceName, ignoreCase: true, out var stance))
        {
            Refresh("That campaign stance is not recognised.");
            return;
        }

        _simulation.SubmitOrder(new SetWarStanceOrder
        {
            Issuer = country.Ruler,
            Recipient = marshal,
            IssuedOn = state.Date,
            Country = country,
            War = war,
            RequestedStance = stance
        });

        Refresh($"{stance} campaign directive queued through {marshal.FullName}.");
    }

    public void OfferPeace(Guid warId, string termsName)
    {
        var state = _simulation.State;
        var country = state.Player.Country;
        var chancellor = country.GetOfficeHolder(Position.Chancellor);
        var war = state.Wars.FirstOrDefault(candidate =>
            candidate.Id == warId &&
            candidate.Status == WarStatus.Active &&
            candidate.IsParticipant(country));

        if (war is null)
        {
            Refresh("That campaign is no longer active.");
            return;
        }

        if (chancellor is null)
        {
            Refresh("The Chancellery is vacant. There is nobody to negotiate peace.");
            return;
        }

        if (!Enum.TryParse<PeaceOfferTerms>(termsName, ignoreCase: true, out var terms))
        {
            Refresh("Those peace terms are not recognised.");
            return;
        }

        _simulation.SubmitOrder(new OfferPeaceOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            War = war,
            Terms = terms
        });

        Refresh($"{terms} peace proposal queued through {chancellor.FullName}.");
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

        var pendingOrderDetails = state.PendingOrders
            .OrderBy(order => order.IssuedOn.Year)
            .ThenBy(order => order.IssuedOn.Month)
            .Select(order => new PendingOrderView(
                order.Id,
                DescribeOrderType(order),
                DescribePendingOrder(order),
                order.Recipient.FullName,
                order.IssuedOn.ToString()))
            .ToList();

        var recentOrderOutcomes = state.Reports
            .Where(report => report.Category == ReportCategory.Order)
            .TakeLast(8)
            .Reverse()
            .Select(report => new OrderOutcomeView(
                report.Date.ToString(),
                report.Title,
                report.Details,
                IsAttentionOrderOutcome(report)))
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
                    character.IsPoliticallyActive && !character.Position.HasValue);
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

        var chancellor = country.GetOfficeHolder(Position.Chancellor);
        var chancellorObedience = chancellor is null
            ? "Vacant"
            : PlayerInformationFormatter.Willingness(
                PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    chancellor,
                    ruler));

        var foreignStates = state.Countries
            .Where(other => !ReferenceEquals(other, country))
            .OrderBy(other => other.Name)
            .Select(other =>
            {
                var relation = state.Diplomacy.GetOrCreate(country, other);
                var activeWar = state.Wars.FirstOrDefault(war =>
                    war.Status == WarStatus.Active &&
                    war.IsParticipant(country) &&
                    war.IsParticipant(other));

                return new ForeignStateView(
                    other.Id,
                    other.Name,
                    other.Ruler.FullName,
                    other.Government.Type.ToString(),
                    KnownShort(
                        state,
                        InformationMetric.DiplomaticRelations,
                        other.Id,
                        country.Id),
                    KnownShort(
                        state,
                        InformationMetric.DiplomaticTrust,
                        other.Id,
                        country.Id),
                    KnownShort(
                        state,
                        InformationMetric.DiplomaticTension,
                        other.Id,
                        country.Id),
                    KnownShort(
                        state,
                        InformationMetric.Gdp,
                        other.Id),
                    KnownShort(
                        state,
                        InformationMetric.ArmySize,
                        other.Id),
                    relation.HasTradeAgreement ? "Active trade agreement" : "No trade agreement",
                    activeWar is null ? "At peace" : $"At war · month {activeWar.MonthsActive}",
                    relation.HasTradeAgreement,
                    activeWar is not null,
                    country.IsNeighbor(other) && activeWar is null);
            })
            .ToList();

        var incomingProposals = state.DiplomaticProposals
            .Where(proposal =>
                proposal.Status == DiplomaticProposalStatus.Pending &&
                ReferenceEquals(proposal.TargetCountry, country))
            .OrderByDescending(proposal => proposal.CreatedOn.Year)
            .ThenByDescending(proposal => proposal.CreatedOn.Month)
            .Select(proposal => new DiplomaticProposalView(
                proposal.Id,
                proposal.SourceCountry.Name,
                proposal.Type switch
                {
                    DiplomaticProposalType.TradeAgreement => "Trade agreement",
                    DiplomaticProposalType.TributeUltimatum => "Tribute ultimatum",
                    _ => proposal.Type.ToString()
                },
                proposal.Type == DiplomaticProposalType.TributeUltimatum
                    ? $"Payment demanded: {proposal.DemandedPayment:N0}"
                    : "Mutual trade access proposed",
                proposal.MonthsOpen == 0
                    ? "New"
                    : $"{proposal.MonthsOpen} month(s) open"))
            .ToList();

        var foreignAffairs = new ForeignAffairsView(
            chancellor?.FullName ?? "Vacant",
            chancellorObedience,
            foreignStates,
            incomingProposals);

        var marshal = country.GetOfficeHolder(Position.Marshal);
        var marshalObedience = marshal is null
            ? "Vacant"
            : PlayerInformationFormatter.Willingness(
                PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    marshal,
                    ruler));

        var campaigns = state.Wars
            .Where(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(country))
            .Select(war =>
            {
                var enemy = war.OpponentOf(country);

                return new WarCampaignView(
                    war.Id,
                    enemy.Id,
                    enemy.Name,
                    $"Month {war.MonthsActive}",
                    war.GetStance(country).ToString(),
                    KnownShort(
                        state,
                        InformationMetric.WarScore,
                        country.Id,
                        enemy.Id),
                    KnownShort(
                        state,
                        InformationMetric.ArmySize,
                        country.Id),
                    KnownShort(
                        state,
                        InformationMetric.ArmyReadiness,
                        country.Id),
                    KnownShort(
                        state,
                        InformationMetric.WarExhaustion,
                        country.Id),
                    KnownShort(
                        state,
                        InformationMetric.ArmySize,
                        enemy.Id),
                    KnownShort(
                        state,
                        InformationMetric.ArmyReadiness,
                        enemy.Id),
                    marshal?.FullName ?? "Vacant",
                    marshalObedience,
                    chancellor?.FullName ?? "Vacant",
                    chancellorObedience);
            })
            .OrderBy(campaign => campaign.OpponentName)
            .ToList();

        var military = new MilitaryView(
            marshal?.FullName ?? "Vacant",
            marshalObedience,
            KnownShort(state, InformationMetric.ArmySize, country.Id),
            KnownShort(state, InformationMetric.ArmyReadiness, country.Id),
            KnownShort(state, InformationMetric.WarExhaustion, country.Id),
            campaigns);

        var economySummary = new List<MetricCardView>
        {
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.Gdp,
                country.Id,
                "GDP"),
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
                InformationMetric.MonthlyTaxRevenue,
                country.Id,
                "Monthly tax revenue"),
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.MonthlyExpenses,
                country.Id,
                "Monthly expenses"),
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.MonthlyBalance,
                country.Id,
                "Monthly balance"),
            PlayerInformationFormatter.Metric(
                state,
                InformationMetric.AdministrativeEfficiency,
                country.Id,
                "Administrative efficiency")
        };

        var economyHistory = state.AdvisorReports
            .Where(report =>
                report.Topic == InformationTopic.Economy &&
                ReferenceEquals(report.SubjectCountry, country))
            .TakeLast(18)
            .Reverse()
            .Select(report => new EconomyReportSnapshotView(
                report.ProducedOn.ToString(),
                $"Data {PlayerInformationFormatter.Age(
                    Math.Max(
                        0,
                        (state.Date.Year - report.DataAsOf.Year) * 12 +
                        state.Date.Month -
                        report.DataAsOf.Month))}",
                report.Advisor.FullName,
                report.WasRequested
                    ? "Requested"
                    : "Unsolicited",
                ReportFactValue(report, InformationMetric.Treasury),
                ReportFactValue(report, InformationMetric.Debt),
                ReportFactValue(report, InformationMetric.MonthlyTaxRevenue),
                ReportFactValue(report, InformationMetric.MonthlyExpenses),
                ReportFactValue(report, InformationMetric.MonthlyBalance),
                report.Caveats.Count > 0))
            .ToList();

        var economy = new EconomyView(
            treasurer?.FullName ?? "Vacant",
            treasurerObedience,
            economySummary,
            economyHistory);

        var archiveEntries = new List<(int SortKey, int Priority, ArchiveEntryView View)>();

        foreach (var report in state.AdvisorReports)
        {
            var details = report.Summary;

            if (report.Caveats.Count > 0)
            {
                details += "  Visible caveats: " +
                           string.Join(" ", report.Caveats);
            }

            archiveEntries.Add((
                DateKey(report.ProducedOn),
                2,
                new ArchiveEntryView(
                    report.ProducedOn.ToString(),
                    "ADVISER REPORT",
                    report.Title,
                    details,
                    $"{report.Advisor.FullName} · {InformationSystem.FormatTopic(report.Topic)}",
                    report.Caveats.Count > 0)));
        }

        foreach (var report in state.Reports)
        {
            var duplicatesAdvisorReport = state.AdvisorReports.Any(advisorReport =>
                advisorReport.ProducedOn == report.Date &&
                advisorReport.Title == report.Title);

            if (duplicatesAdvisorReport)
                continue;

            archiveEntries.Add((
                DateKey(report.Date),
                1,
                new ArchiveEntryView(
                    report.Date.ToString(),
                    report.Category.ToString().ToUpperInvariant(),
                    report.Title,
                    report.Details,
                    "Government record",
                    report.Category is ReportCategory.Military
                        or ReportCategory.Politics
                        or ReportCategory.Personal)));
        }

        var archive = new ArchiveView(
            archiveEntries
                .OrderByDescending(entry => entry.SortKey)
                .ThenByDescending(entry => entry.Priority)
                .Take(200)
                .Select(entry => entry.View)
                .ToList());

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
            pendingOrderDetails,
            recentOrderOutcomes,
            metrics,
            advisors,
            briefings,
            pending,
            intelligenceReports,
            foreignCountries,
            government,
            court,
            foreignAffairs,
            military,
            economy,
            archive,
            _statusMessage);
    }

    private static string DescribeOrderType(Order order)
    {
        return order switch
        {
            ChangeTaxOrder => "Tax",
            SetBudgetOrder => "Budget",
            AppointAdvisorOrder => "Appointment",
            DismissAdvisorOrder => "Dismissal",
            InvestigateCharacterOrder => "Investigation",
            ArrestCharacterOrder => "Arrest",
            ReleasePrisonerOrder => "Release",
            ImproveRelationsOrder => "Diplomacy",
            NegotiateTradeAgreementOrder => "Trade",
            EndTradeAgreementOrder => "Trade",
            DeclareWarOrder => "War",
            SetWarStanceOrder => "Military",
            OfferPeaceOrder => "Peace",
            RespondToDiplomaticProposalOrder => "Diplomacy",
            RequestReportOrder => "Report",
            _ => "Order"
        };
    }

    private static string DescribePendingOrder(Order order)
    {
        return order switch
        {
            ChangeTaxOrder typed =>
                $"Set official tax rate toward {typed.TargetTaxRate:P0}.",

            SetBudgetOrder typed =>
                $"Set funding targets: army {typed.TargetArmyFunding:P0}, " +
                $"administration {typed.TargetAdministrationFunding:P0}, " +
                $"court {typed.TargetCourtFunding:P0}.",

            AppointAdvisorOrder typed =>
                $"Appoint {typed.Recipient.FullName} as {typed.Position}.",

            DismissAdvisorOrder typed =>
                $"Dismiss {typed.Recipient.FullName} from office.",

            InvestigateCharacterOrder typed =>
                $"Investigate {typed.Subject.FullName}.",

            ArrestCharacterOrder typed =>
                $"Arrest {typed.Subject.FullName}.",

            ReleasePrisonerOrder typed =>
                $"Release {typed.Recipient.FullName} from political imprisonment.",

            ImproveRelationsOrder typed =>
                $"Send diplomatic outreach to {typed.TargetCountry.Name}.",

            NegotiateTradeAgreementOrder typed =>
                $"Seek a trade agreement with {typed.TargetCountry.Name}.",

            EndTradeAgreementOrder typed =>
                $"End the trade agreement with {typed.TargetCountry.Name}.",

            DeclareWarOrder typed =>
                $"Deliver a declaration of war to {typed.TargetCountry.Name}.",

            SetWarStanceOrder typed =>
                $"Adopt a {typed.RequestedStance} stance against " +
                $"{typed.War.OpponentOf(typed.Country).Name}.",

            OfferPeaceOrder typed =>
                $"Offer {FormatPeaceTerms(typed.Terms)} to " +
                $"{typed.War.OpponentOf(typed.Country).Name}.",

            RespondToDiplomaticProposalOrder typed =>
                $"{(typed.Accept ? "Accept" : "Reject")} " +
                $"{typed.Proposal.SourceCountry.Name}'s " +
                $"{typed.Proposal.Type} proposal.",

            RequestReportOrder typed =>
                $"Request {InformationSystem.FormatTopic(typed.Topic).ToLowerInvariant()} " +
                $"report on {typed.SubjectCountry.Name}.",

            _ => order.GetType().Name
        };
    }

    private static string FormatPeaceTerms(PeaceOfferTerms terms)
    {
        return terms switch
        {
            PeaceOfferTerms.WhitePeace => "white peace",
            PeaceOfferTerms.DemandReparations => "peace with reparations demanded",
            PeaceOfferTerms.OfferReparations => "peace with reparations offered",
            _ => terms.ToString()
        };
    }

    private static bool IsAttentionOrderOutcome(SimulationReport report)
    {
        return report.Title.Contains("refus", StringComparison.OrdinalIgnoreCase) ||
               report.Title.Contains("reject", StringComparison.OrdinalIgnoreCase) ||
               report.Title.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
               report.Details.Contains("refus", StringComparison.OrdinalIgnoreCase) ||
               report.Details.Contains("reject", StringComparison.OrdinalIgnoreCase) ||
               report.Details.Contains("failed", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReportFactValue(
        AdvisorIntelligenceReport report,
        InformationMetric metric)
    {
        var fact = report.Facts.FirstOrDefault(candidate =>
            candidate.Key.Metric == metric);

        return fact is null
            ? "—"
            : $"~{PlayerInformationFormatter.Value(metric, fact.Estimate)}";
    }

    private static string KnownShort(
        GameState state,
        InformationMetric metric,
        string subjectCountryId,
        string? relatedCountryId = null)
    {
        var known = state.Knowledge.Get(
            metric,
            subjectCountryId,
            relatedCountryId);

        if (known is null)
            return "Unknown";

        var age = known.AgeInMonths(state.Date);

        return $"~{PlayerInformationFormatter.Value(metric, known.Estimate)} · " +
               PlayerInformationFormatter.Age(age);
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
