using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

var state = DemoScenario.Create();
var simulation = new GameSimulation(state);

while (true)
{
    var country = state.Player.Country;

    if (state.Player.HasLost)
    {
        Console.Clear();
        Console.WriteLine("Political lineage lost");
        Console.WriteLine("======================");
        Console.WriteLine();
        Console.WriteLine(state.Player.LossReason ?? "Your political lineage has lost power.");
        Console.WriteLine();
        Console.WriteLine($"{country.Name} continues under {country.Ruler.FullName}.");
        break;
    }

    Console.Clear();
    PrintDashboard(state);

    Console.WriteLine();
    Console.WriteLine("[Enter] Advance month");
    Console.WriteLine("[T] Order a tax-rate change");
    Console.WriteLine("[B] Order a new budget");
    Console.WriteLine("[A] Appoint or replace an adviser");
    Console.WriteLine("[D] Dismiss an office-holder");
    Console.WriteLine("[I] Investigate a political figure");
    Console.WriteLine("[F] Foreign affairs");
    Console.WriteLine("[M] Military");
    Console.WriteLine("[P] Prison and arrests");
    Console.WriteLine("[G] Political groups");
    Console.WriteLine("[C] Inspect the court");
    Console.WriteLine("[K] Intelligence and adviser reports");
    Console.WriteLine("[R] Read recent events");
    Console.WriteLine("[Q] Quit");

    var input = Console.ReadLine()?.Trim();

    if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
        break;

    if (string.Equals(input, "t", StringComparison.OrdinalIgnoreCase))
    {
        QueueTaxOrder(simulation, country);
        continue;
    }

    if (string.Equals(input, "b", StringComparison.OrdinalIgnoreCase))
    {
        QueueBudgetOrder(simulation, country);
        continue;
    }

    if (string.Equals(input, "a", StringComparison.OrdinalIgnoreCase))
    {
        QueueAppointmentOrder(simulation, country);
        continue;
    }

    if (string.Equals(input, "d", StringComparison.OrdinalIgnoreCase))
    {
        QueueDismissalOrder(simulation, country);
        continue;
    }

    if (string.Equals(input, "i", StringComparison.OrdinalIgnoreCase))
    {
        QueueInvestigationOrder(simulation, country);
        continue;
    }

    if (string.Equals(input, "f", StringComparison.OrdinalIgnoreCase))
    {
        ManageForeignAffairs(simulation, country);
        continue;
    }

    if (string.Equals(input, "m", StringComparison.OrdinalIgnoreCase))
    {
        ManageMilitary(simulation, country);
        continue;
    }

    if (string.Equals(input, "p", StringComparison.OrdinalIgnoreCase))
    {
        ManagePrison(simulation, country);
        continue;
    }

    if (string.Equals(input, "g", StringComparison.OrdinalIgnoreCase))
    {
        PrintPoliticalGroups(state);
        continue;
    }

    if (string.Equals(input, "c", StringComparison.OrdinalIgnoreCase))
    {
        PrintCourt(state);
        continue;
    }

    if (string.Equals(input, "k", StringComparison.OrdinalIgnoreCase))
    {
        ManageInformation(simulation, country);
        continue;
    }

    if (string.Equals(input, "r", StringComparison.OrdinalIgnoreCase))
    {
        PrintReports(state);
        continue;
    }

    simulation.AdvanceMonth();
}

static void PrintDashboard(GameState state)
{
    var country = state.Player.Country;

    Console.WriteLine($"{country.Name} — {state.Date}");
    Console.WriteLine($"Lineage: {state.Player.Lineage.Name}");
    Console.WriteLine();
    Console.WriteLine(
        $"Ruler: {country.Ruler.FullName} — age {country.Ruler.Age}, " +
        $"health {country.Ruler.Health}/100");
    Console.WriteLine(
        $"Population: {FormatKnown(state, InformationMetric.Population, country.Id)}");
    Console.WriteLine(
        $"GDP: {FormatKnown(state, InformationMetric.Gdp, country.Id)}");
    Console.WriteLine(
        $"Treasury: {FormatKnown(state, InformationMetric.Treasury, country.Id)}");
    Console.WriteLine(
        $"Debt: {FormatKnown(state, InformationMetric.Debt, country.Id)}");

    var taxRevenue = state.Knowledge.Get(
        InformationMetric.MonthlyTaxRevenue,
        country.Id);
    var expenses = state.Knowledge.Get(
        InformationMetric.MonthlyExpenses,
        country.Id);
    var balance = state.Knowledge.Get(
        InformationMetric.MonthlyBalance,
        country.Id);

    if (taxRevenue is not null || expenses is not null || balance is not null)
    {
        Console.WriteLine(
            $"Last budget estimate: revenue {FormatKnown(state, InformationMetric.MonthlyTaxRevenue, country.Id)}, " +
            $"expenses {FormatKnown(state, InformationMetric.MonthlyExpenses, country.Id)}, " +
            $"balance {FormatKnown(state, InformationMetric.MonthlyBalance, country.Id)}");
    }

    Console.WriteLine(
        $"Army: {FormatKnown(state, InformationMetric.ArmySize, country.Id)} — " +
        $"readiness {FormatKnown(state, InformationMetric.ArmyReadiness, country.Id)}");
    Console.WriteLine($"Tax rate: {country.TaxRate:P1} (enacted policy)");
    Console.WriteLine(
        $"Administrative efficiency: {FormatKnown(state, InformationMetric.AdministrativeEfficiency, country.Id)}");
    Console.WriteLine(
        $"Funding: army {country.ArmyFunding:P0}, administration " +
        $"{country.AdministrationFunding:P0}, court {country.CourtFunding:P0} (enacted policy)");
    Console.WriteLine(
        $"Public unrest: {FormatKnown(state, InformationMetric.PublicUnrest, country.Id)}");
    Console.WriteLine(
        $"Stability: {FormatKnown(state, InformationMetric.GovernmentStability, country.Id)}");
    Console.WriteLine(
        $"Political backing: {FormatKnown(state, InformationMetric.PoliticalBacking, country.Id)}");
    Console.WriteLine($"Pending orders: {state.PendingOrders.Count}");

    var pendingReports = state.InformationRequests.Count(request =>
        request.Status == InformationRequestStatus.Pending);

    if (pendingReports > 0)
        Console.WriteLine($"Reports being prepared: {pendingReports}");

    var activeDomesticDemands = state.PowerBaseDemands
        .Where(demand =>
            !demand.IsResolved &&
            ReferenceEquals(demand.Country, country))
        .OrderByDescending(demand => demand.EscalationLevel)
        .ThenByDescending(demand => demand.MonthsOpen)
        .ToList();

    if (activeDomesticDemands.Count > 0)
    {
        var urgent = activeDomesticDemands[0];

        Console.WriteLine(
            $"Domestic pressures: {activeDomesticDemands.Count} — most urgent: " +
            $"{FormatPowerBase(urgent.PowerBase)} wants {DescribeDemandBrief(urgent)}");
    }

    var activeBloc = state.PoliticalBlocs.FirstOrDefault(bloc =>
        bloc.IsActive &&
        ReferenceEquals(bloc.Country, country));

    if (activeBloc is not null)
    {
        Console.WriteLine(
            $"Organised opposition: {activeBloc.Leader.FullName} — " +
            $"{DescribeLevel(activeBloc.Cohesion)} cohesion");
    }

    var activeWars = state.Wars
        .Where(war =>
            war.Status == WarStatus.Active &&
            war.IsParticipant(country))
        .ToList();

    if (activeWars.Count > 0)
    {
        Console.WriteLine(
            $"War exhaustion: {FormatKnown(state, InformationMetric.WarExhaustion, country.Id)}");

        foreach (var war in activeWars)
        {
            var opponent = war.OpponentOf(country);

            Console.WriteLine(
                $"At war with {opponent.Name}: reported position " +
                $"{FormatKnown(state, InformationMetric.WarScore, country.Id, opponent.Id)}, " +
                $"month {war.MonthsActive}, stance {war.GetStance(country)}");
        }
    }

    var knownPlots = state.Plots.Count(plot =>
        !plot.IsResolved &&
        plot.DiscoveryStage > 0 &&
        ReferenceEquals(plot.Country, country));

    if (knownPlots > 0)
        Console.WriteLine($"Known political threats: {knownPlots}");

    var pendingDiplomaticOffers = state.DiplomaticProposals.Count(proposal =>
        proposal.Status == DiplomaticProposalStatus.Pending &&
        ReferenceEquals(proposal.TargetCountry, country));

    if (pendingDiplomaticOffers > 0)
        Console.WriteLine($"Pending diplomatic offers: {pendingDiplomaticOffers}");

    var prisonerCount = country.Prisoners.Count();
    if (prisonerCount > 0)
        Console.WriteLine($"Political prisoners: {prisonerCount}");

    Console.WriteLine();
    Console.WriteLine("Office-holders:");

    foreach (var advisor in country.ActiveAdvisors)
    {
        var relationship = state.Relationships.GetOrCreate(advisor, country.Ruler);
        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            country,
            advisor,
            country.Ruler);
        var threat = PoliticalCalculations.GetThreatScore(state, country, advisor);

        Console.WriteLine(
            $"{advisor.Position!.Value,-12} " +
            $"{advisor.FullName,-20} " +
            $"ability {DescribeLevel(advisor.Competence),-10}  " +
            $"trust {DescribeTrust(relationship.Trust),-10}  " +
            $"obedience {DescribeWillingness(willingness),-11}  " +
            $"risk {DescribeThreat(threat)}");
    }

    var candidates = country.AvailableAdvisors.ToList();

    if (candidates.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"Available political figures: {candidates.Count} — press C to inspect.");
    }

    Console.WriteLine();
    Console.WriteLine(
        "Numbers marked with an adviser/date are estimates. Press K to inspect sources, uncertainty and staleness.");
}

static void QueueTaxOrder(GameSimulation simulation, Country country)
{
    var treasurer = country.GetOfficeHolder(Position.Treasurer);

    if (treasurer is null)
    {
        Pause("There is no living Treasurer to receive the order.");
        return;
    }

    var willingness = PoliticalCalculations.GetOrderWillingness(
        simulation.State,
        country,
        treasurer,
        simulation.State.Player.CurrentCharacter);

    Console.WriteLine(
        $"{treasurer.FullName}'s apparent willingness to obey is {DescribeWillingness(willingness)}.");
    Console.Write($"Target tax rate (0-60%, current {country.TaxRate:P1}): ");

    var raw = Console.ReadLine();

    if (!decimal.TryParse(raw, out var percentage) || percentage is < 0m or > 60m)
    {
        Pause("Enter a percentage between 0 and 60.");
        return;
    }

    simulation.SubmitOrder(new ChangeTaxOrder
    {
        Issuer = simulation.State.Player.CurrentCharacter,
        Recipient = treasurer,
        IssuedOn = simulation.State.Date,
        Country = country,
        TargetTaxRate = percentage / 100m
    });

    Pause($"Order sent to {treasurer.FullName}. It will be processed when the month advances.");
}

static void QueueBudgetOrder(GameSimulation simulation, Country country)
{
    var treasurer = country.GetOfficeHolder(Position.Treasurer);

    if (treasurer is null)
    {
        Pause("There is no living Treasurer to receive the budget order.");
        return;
    }

    Console.Clear();
    Console.WriteLine("Government budget");
    Console.WriteLine("=================");
    Console.WriteLine();
    Console.WriteLine(
        "Funding is relative to the normal level. 50% is severe austerity; " +
        "150% is heavy overfunding.");
    Console.WriteLine();
    Console.WriteLine(
        $"Current: army {country.ArmyFunding:P0}, administration " +
        $"{country.AdministrationFunding:P0}, court {country.CourtFunding:P0}");
    Console.WriteLine(
        $"Reported army readiness: {FormatKnown(simulation.State, InformationMetric.ArmyReadiness, country.Id)}");
    Console.WriteLine(
        $"Reported administrative efficiency: {FormatKnown(simulation.State, InformationMetric.AdministrativeEfficiency, country.Id)}");
    Console.WriteLine(
        $"Reported debt: {FormatKnown(simulation.State, InformationMetric.Debt, country.Id)}");
    Console.WriteLine();

    if (!TryReadFunding("Army funding", country.ArmyFunding, out var army) ||
        !TryReadFunding("Administration funding", country.AdministrationFunding, out var administration) ||
        !TryReadFunding("Court and patronage funding", country.CourtFunding, out var court))
    {
        Pause("Each funding level must be a percentage from 50 to 150.");
        return;
    }

    simulation.SubmitOrder(new SetBudgetOrder
    {
        Issuer = country.Ruler,
        Recipient = treasurer,
        IssuedOn = simulation.State.Date,
        Country = country,
        TargetArmyFunding = army,
        TargetAdministrationFunding = administration,
        TargetCourtFunding = court
    });

    Pause(
        $"Budget order sent to {treasurer.FullName}. The enacted figures may differ " +
        "from your targets depending on their competence and willingness.");
}

static bool TryReadFunding(
    string label,
    decimal current,
    out decimal funding)
{
    Console.Write($"{label} (50-150%, current {current:P0}): ");
    var raw = Console.ReadLine();

    if (decimal.TryParse(raw, out var percentage) &&
        percentage is >= 50m and <= 150m)
    {
        funding = percentage / 100m;
        return true;
    }

    funding = current;
    return false;
}

static void QueueAppointmentOrder(GameSimulation simulation, Country country)
{
    var candidates = country.AvailableAdvisors.ToList();

    if (candidates.Count == 0)
    {
        Pause("There are no available political figures to appoint.");
        return;
    }

    Console.Clear();
    Console.WriteLine("Available political figures");
    Console.WriteLine("===========================");
    Console.WriteLine();

    for (var i = 0; i < candidates.Count; i++)
    {
        var candidate = candidates[i];
        var relationship = simulation.State.Relationships.GetOrCreate(
            candidate,
            country.Ruler);
        var threat = PoliticalCalculations.GetThreatScore(
            simulation.State,
            country,
            candidate);
        var powerBaseInfluence =
            PoliticalCalculations.GetPowerBaseInfluence(country, candidate);

        Console.WriteLine(
            $"[{i + 1}] {candidate.FullName,-20} " +
            $"ability {DescribeLevel(candidate.Competence),-10}  " +
            $"ambition {DescribeAmbition(candidate.Ambition),-10}  " +
            $"trust {DescribeTrust(relationship.Trust),-10}  " +
            $"influence {DescribeLevel(candidate.Influence),-10}  " +
            $"base {DescribeLevel(powerBaseInfluence),-10}  " +
            $"risk {DescribeThreat(threat)}");
    }

    Console.WriteLine();
    Console.Write("Choose candidate: ");

    if (!int.TryParse(Console.ReadLine(), out var candidateNumber) ||
        candidateNumber < 1 ||
        candidateNumber > candidates.Count)
    {
        Pause("Invalid candidate.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("[1] Chancellor");
    Console.WriteLine("[2] Treasurer");
    Console.WriteLine("[3] Marshal");
    Console.Write("Choose office: ");

    if (!int.TryParse(Console.ReadLine(), out var positionNumber) ||
        positionNumber is < 1 or > 3)
    {
        Pause("Invalid office.");
        return;
    }

    var position = positionNumber switch
    {
        1 => Position.Chancellor,
        2 => Position.Treasurer,
        3 => Position.Marshal,
        _ => throw new InvalidOperationException()
    };

    var selectedCandidate = candidates[candidateNumber - 1];

    simulation.SubmitOrder(new AppointAdvisorOrder
    {
        Issuer = simulation.State.Player.CurrentCharacter,
        Recipient = selectedCandidate,
        IssuedOn = simulation.State.Date,
        Country = country,
        Position = position
    });

    var currentHolder = country.GetOfficeHolder(position);
    var replacement = currentHolder is null
        ? "The office is currently vacant."
        : $"This will replace {currentHolder.FullName}, which will create a serious grievance.";

    Pause(
        $"Appointment of {selectedCandidate.FullName} as {position} queued. " +
        $"{replacement} It will be processed when the month advances.");
}

static void QueueDismissalOrder(GameSimulation simulation, Country country)
{
    var officeHolders = country.ActiveAdvisors.ToList();

    if (officeHolders.Count == 0)
    {
        Pause("There are no office-holders to dismiss.");
        return;
    }

    Console.Clear();
    Console.WriteLine("Dismiss an office-holder");
    Console.WriteLine("========================");
    Console.WriteLine();

    for (var i = 0; i < officeHolders.Count; i++)
    {
        var character = officeHolders[i];
        var relationship = simulation.State.Relationships.GetOrCreate(
            character,
            country.Ruler);

        Console.WriteLine(
            $"[{i + 1}] {character.Position,-12} {character.FullName,-20} " +
            $"{DescribeOpinion(relationship.Opinion),-18}  " +
            $"trust {DescribeTrust(relationship.Trust),-10}  " +
            $"influence {DescribeLevel(character.Influence)}");
    }

    Console.WriteLine();
    Console.Write("Choose office-holder: ");

    if (!int.TryParse(Console.ReadLine(), out var number) ||
        number < 1 ||
        number > officeHolders.Count)
    {
        Pause("Invalid choice.");
        return;
    }

    var target = officeHolders[number - 1];

    simulation.SubmitOrder(new DismissAdvisorOrder
    {
        Issuer = country.Ruler,
        Recipient = target,
        IssuedOn = simulation.State.Date,
        Country = country
    });

    Pause(
        $"Dismissal of {target.FullName} queued. Removing them from office will " +
        "reduce their immediate access to power, but create a personal grievance.");
}

static void QueueInvestigationOrder(GameSimulation simulation, Country country)
{
    var chancellor = country.GetOfficeHolder(Position.Chancellor);

    if (chancellor is null)
    {
        Pause("There is no living Chancellor to conduct an investigation.");
        return;
    }

    var subjects = country.PoliticalFigures
        .Where(character =>
            character.IsPoliticallyActive &&
            !ReferenceEquals(character, country.Ruler) &&
            !ReferenceEquals(character, chancellor))
        .OrderByDescending(character =>
            PoliticalCalculations.GetThreatScore(simulation.State, country, character))
        .ToList();

    if (subjects.Count == 0)
    {
        Pause("There is nobody available to investigate.");
        return;
    }

    Console.Clear();
    Console.WriteLine($"Investigation by {chancellor.FullName}");
    Console.WriteLine(new string('=', 17 + chancellor.FullName.Length));
    Console.WriteLine();

    for (var i = 0; i < subjects.Count; i++)
    {
        var subject = subjects[i];
        var threat = PoliticalCalculations.GetThreatScore(
            simulation.State,
            country,
            subject);

        Console.WriteLine(
            $"[{i + 1}] {subject.FullName,-20} " +
            $"Role {(subject.Position?.ToString() ?? "Courtier"),-11} " +
            $"influence {DescribeLevel(subject.Influence),-10}  " +
            $"risk {DescribeThreat(threat)}");
    }

    Console.WriteLine();
    Console.WriteLine(
        "Investigating somebody may expose or disrupt a plot, but scrutiny itself " +
        "damages their relationship with the ruler.");
    Console.Write("Choose subject: ");

    if (!int.TryParse(Console.ReadLine(), out var number) ||
        number < 1 ||
        number > subjects.Count)
    {
        Pause("Invalid choice.");
        return;
    }

    var subjectToInvestigate = subjects[number - 1];

    simulation.SubmitOrder(new InvestigateCharacterOrder
    {
        Issuer = country.Ruler,
        Recipient = chancellor,
        Subject = subjectToInvestigate,
        IssuedOn = simulation.State.Date,
        Country = country
    });

    Pause(
        $"Investigation of {subjectToInvestigate.FullName} queued through " +
        $"{chancellor.FullName}.");
}


static void ManageForeignAffairs(GameSimulation simulation, Country country)
{
    var chancellor = country.GetOfficeHolder(Position.Chancellor);

    if (chancellor is null)
    {
        Pause("There is no active Chancellor to conduct foreign policy.");
        return;
    }

    var foreignCountries = simulation.State.Countries
        .Where(candidate => !ReferenceEquals(candidate, country))
        .OrderBy(candidate => candidate.Name)
        .ToList();

    Console.Clear();
    Console.WriteLine("Foreign affairs");
    Console.WriteLine("===============");
    Console.WriteLine();
    Console.WriteLine(
        $"Chancellor: {chancellor.FullName} — assessed ability {DescribeLevel(chancellor.Competence)}");
    Console.WriteLine(
        "Diplomatic figures below are your Chancellor's latest estimates, not live hidden state.");

    var incoming = simulation.State.DiplomaticProposals
        .Where(proposal =>
            proposal.Status == DiplomaticProposalStatus.Pending &&
            ReferenceEquals(proposal.TargetCountry, country))
        .ToList();

    if (incoming.Count > 0)
    {
        Console.WriteLine(
            $"Incoming proposals: {incoming.Count} — enter O to review.");
    }

    Console.WriteLine();

    for (var i = 0; i < foreignCountries.Count; i++)
    {
        var foreignCountry = foreignCountries[i];
        var relation = simulation.State.Diplomacy.GetOrCreate(country, foreignCountry);
        var trade = relation.HasTradeAgreement ? "trade" : "no trade";

        Console.WriteLine(
            $"[{i + 1}] {foreignCountry.Name,-12} " +
            $"relations {FormatKnownShort(simulation.State, InformationMetric.DiplomaticRelations, foreignCountry.Id, country.Id),-18} " +
            $"trust {FormatKnownShort(simulation.State, InformationMetric.DiplomaticTrust, foreignCountry.Id, country.Id),-16} " +
            $"tension {FormatKnownShort(simulation.State, InformationMetric.DiplomaticTension, foreignCountry.Id, country.Id),-16} " +
            $"{trade}");
    }

    Console.WriteLine();
    Console.Write("Choose country or O for offers: ");

    var selection = Console.ReadLine()?.Trim();

    if (string.Equals(selection, "o", StringComparison.OrdinalIgnoreCase))
    {
        ReviewDiplomaticProposals(simulation, country, chancellor);
        return;
    }

    if (!int.TryParse(selection, out var number) ||
        number < 1 ||
        number > foreignCountries.Count)
    {
        Pause("Invalid country.");
        return;
    }

    var target = foreignCountries[number - 1];
    var targetRelation = simulation.State.Diplomacy.GetOrCreate(country, target);
    ShowForeignCountry(simulation.State, country, target, chancellor, targetRelation);

    var activeWar = simulation.State.Wars.FirstOrDefault(war =>
        war.Status == WarStatus.Active &&
        war.IsParticipant(country) &&
        war.IsParticipant(target));

    if (activeWar is not null)
    {
        Pause(
            $"{country.Name} is already at war with {target.Name}. " +
            "Use the Military screen to direct the campaign.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("[1] Send a mission to improve relations");

    if (targetRelation.HasTradeAgreement)
        Console.WriteLine("[2] End the trade agreement");
    else
        Console.WriteLine("[2] Propose a trade agreement");

    if (country.IsNeighbor(target))
        Console.WriteLine("[3] Declare war");

    Console.WriteLine("[Enter] Cancel");
    Console.Write("Choose action: ");

    var action = Console.ReadLine()?.Trim();

    if (action == "1")
    {
        simulation.SubmitOrder(new ImproveRelationsOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = simulation.State.Date,
            SourceCountry = country,
            TargetCountry = target
        });

        Pause(
            $"Diplomatic mission to {target.Name} queued through {chancellor.FullName}.");
        return;
    }

    if (action == "3" && country.IsNeighbor(target))
    {
        Console.WriteLine();
        Console.WriteLine(
            "You do not have direct access to the target's true military strength. " +
            "Request military intelligence from your Marshal if the current estimate is stale.");
        Console.Write("Type DECLARE to confirm: ");

        if (!string.Equals(
                Console.ReadLine()?.Trim(),
                "DECLARE",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        simulation.SubmitOrder(new DeclareWarOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = simulation.State.Date,
            SourceCountry = country,
            TargetCountry = target
        });

        Pause(
            $"Declaration of war on {target.Name} queued through {chancellor.FullName}.");
        return;
    }

    if (action != "2")
        return;

    if (targetRelation.HasTradeAgreement)
    {
        simulation.SubmitOrder(new EndTradeAgreementOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = simulation.State.Date,
            SourceCountry = country,
            TargetCountry = target
        });

        Pause($"Termination of the {target.Name} trade agreement queued.");
        return;
    }

    simulation.SubmitOrder(new NegotiateTradeAgreementOrder
    {
        Issuer = country.Ruler,
        Recipient = chancellor,
        IssuedOn = simulation.State.Date,
        SourceCountry = country,
        TargetCountry = target
    });

    Pause(
        $"Trade negotiations with {target.Name} queued. Your Chancellor can carry out " +
        "the talks well and still be rejected by the foreign government.");
}

static void ReviewDiplomaticProposals(
    GameSimulation simulation,
    Country country,
    Character chancellor)
{
    var proposals = simulation.State.DiplomaticProposals
        .Where(proposal =>
            proposal.Status == DiplomaticProposalStatus.Pending &&
            ReferenceEquals(proposal.TargetCountry, country))
        .OrderByDescending(proposal =>
            proposal.Type == DiplomaticProposalType.TributeUltimatum)
        .ThenByDescending(proposal => proposal.MonthsOpen)
        .ToList();

    if (proposals.Count == 0)
    {
        Pause("There are no pending diplomatic proposals.");
        return;
    }

    Console.Clear();
    Console.WriteLine("Incoming diplomatic proposals");
    Console.WriteLine("=============================");
    Console.WriteLine();

    for (var i = 0; i < proposals.Count; i++)
    {
        var proposal = proposals[i];
        var relation = simulation.State.Diplomacy.GetOrCreate(
            proposal.SourceCountry,
            country);

        var description = proposal.Type switch
        {
            DiplomaticProposalType.TributeUltimatum =>
                $"ULTIMATUM — demands {proposal.DemandedPayment:N0}",
            _ => "trade agreement"
        };

        Console.WriteLine(
            $"[{i + 1}] {proposal.SourceCountry.Name,-12} " +
            $"{description}  open {proposal.MonthsOpen} month(s)");
        Console.WriteLine(
            $"    Relations {relation.Relations}, trust {relation.Trust}, " +
            $"tension {relation.Tension}");
    }

    Console.WriteLine();
    Console.Write("Choose proposal: ");

    if (!int.TryParse(Console.ReadLine(), out var number) ||
        number < 1 ||
        number > proposals.Count)
    {
        Pause("Invalid proposal.");
        return;
    }

    var selected = proposals[number - 1];

    Console.WriteLine();

    if (selected.Type == DiplomaticProposalType.TributeUltimatum)
    {
        Console.WriteLine(
            $"{selected.SourceCountry.Name} demands {selected.DemandedPayment:N0} " +
            $"from {country.Name}.");
        Console.WriteLine(
            "Rejecting or ignoring the demand can cause the foreign government to " +
            "escalate to war if it believes it has enough military leverage.");
        Console.WriteLine("[A] Yield and pay");
        Console.WriteLine("[R] Reject the ultimatum");
    }
    else
    {
        Console.WriteLine(
            $"{selected.SourceCountry.Name} proposes a trade agreement with {country.Name}.");
        Console.WriteLine("[A] Accept");
        Console.WriteLine("[R] Reject");
    }

    Console.WriteLine("[Enter] Leave unanswered");
    Console.Write("Response: ");

    var response = Console.ReadLine()?.Trim();

    if (!string.Equals(response, "a", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(response, "r", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var accept = string.Equals(response, "a", StringComparison.OrdinalIgnoreCase);

    simulation.SubmitOrder(new RespondToDiplomaticProposalOrder
    {
        Issuer = country.Ruler,
        Recipient = chancellor,
        IssuedOn = simulation.State.Date,
        Country = country,
        Proposal = selected,
        Accept = accept
    });

    Pause(
        $"{(accept ? "Acceptance" : "Rejection")} of {selected.SourceCountry.Name}'s " +
        $"{(selected.Type == DiplomaticProposalType.TributeUltimatum ? "ultimatum" : "proposal")} " +
        $"queued through {chancellor.FullName}.");
}

static void ShowForeignCountry(
    GameState state,
    Country country,
    Country target,
    Character chancellor,
    DiplomaticRelation relation)
{
    Console.Clear();
    Console.WriteLine(target.Name);
    Console.WriteLine(new string('=', target.Name.Length));
    Console.WriteLine();
    Console.WriteLine($"Ruler: {target.Ruler.FullName}");
    Console.WriteLine($"Government: {target.Government.Type}");
    Console.WriteLine(
        $"Population: {FormatKnown(state, InformationMetric.Population, target.Id)}");
    Console.WriteLine(
        $"GDP: {FormatKnown(state, InformationMetric.Gdp, target.Id)}");
    Console.WriteLine(
        $"Army strength: {FormatKnown(state, InformationMetric.ArmySize, target.Id)}");
    Console.WriteLine(
        $"Army readiness: {FormatKnown(state, InformationMetric.ArmyReadiness, target.Id)}");
    Console.WriteLine(
        $"Relations: {FormatKnown(state, InformationMetric.DiplomaticRelations, target.Id, country.Id)}");
    Console.WriteLine(
        $"Trust: {FormatKnown(state, InformationMetric.DiplomaticTrust, target.Id, country.Id)}");
    Console.WriteLine(
        $"Tension: {FormatKnown(state, InformationMetric.DiplomaticTension, target.Id, country.Id)}");
    Console.WriteLine(
        $"Trade agreement: {(relation.HasTradeAgreement ? "active" : "none")}");

    if (!relation.HasTradeAgreement && country.IsNeighbor(target))
    {
        var score = DiplomaticCalculations.GetTradeAcceptanceScore(
            state,
            country,
            target,
            chancellor);

        var assessment = score switch
        {
            >= 70 => "likely receptive",
            >= 55 => "plausibly receptive",
            >= 40 => "unlikely to accept",
            _ => "strongly opposed"
        };

        Console.WriteLine(
            $"Chancellor's judgement on a trade proposal: {assessment}. " +
            "This is an assessment, not a guaranteed foreign response.");
    }
}

static void ManageMilitary(GameSimulation simulation, Country country)
{
    var wars = simulation.State.Wars
        .Where(war =>
            war.Status == WarStatus.Active &&
            war.IsParticipant(country))
        .ToList();

    if (wars.Count == 0)
    {
        Pause("The country is not currently at war.");
        return;
    }

    Console.Clear();
    Console.WriteLine("Military campaigns");
    Console.WriteLine("==================");
    Console.WriteLine();
    Console.WriteLine(
        "Campaign figures are based on Marshal reports and field intelligence. " +
        "Enemy estimates can be especially wrong or stale.");
    Console.WriteLine();

    for (var i = 0; i < wars.Count; i++)
    {
        var war = wars[i];
        var opponent = war.OpponentOf(country);

        Console.WriteLine(
            $"[{i + 1}] {opponent.Name,-12} reported position " +
            $"{FormatKnownShort(simulation.State, InformationMetric.WarScore, country.Id, opponent.Id),-18} " +
            $"month {war.MonthsActive,2}  stance {war.GetStance(country)}");
    }

    Console.WriteLine();
    Console.Write("Choose campaign: ");

    if (!int.TryParse(Console.ReadLine(), out var number) ||
        number < 1 ||
        number > wars.Count)
    {
        Pause("Invalid campaign.");
        return;
    }

    var selected = wars[number - 1];
    var enemy = selected.OpponentOf(country);
    var marshal = country.GetOfficeHolder(Position.Marshal);
    var chancellor = country.GetOfficeHolder(Position.Chancellor);

    Console.Clear();
    Console.WriteLine($"{country.Name} vs {enemy.Name}");
    Console.WriteLine(new string('=', country.Name.Length + enemy.Name.Length + 4));
    Console.WriteLine();
    Console.WriteLine(
        $"Reported campaign position: {FormatKnown(simulation.State, InformationMetric.WarScore, country.Id, enemy.Id)}");
    Console.WriteLine($"Months active: {selected.MonthsActive}");
    Console.WriteLine(
        $"Our army: {FormatKnown(simulation.State, InformationMetric.ArmySize, country.Id)}");
    Console.WriteLine(
        $"Our readiness: {FormatKnown(simulation.State, InformationMetric.ArmyReadiness, country.Id)}");
    Console.WriteLine(
        $"Our war exhaustion: {FormatKnown(simulation.State, InformationMetric.WarExhaustion, country.Id)}");
    Console.WriteLine(
        $"Enemy army: {FormatKnown(simulation.State, InformationMetric.ArmySize, enemy.Id)}");
    Console.WriteLine(
        $"Enemy readiness: {FormatKnown(simulation.State, InformationMetric.ArmyReadiness, enemy.Id)}");
    Console.WriteLine($"Current ordered stance: {selected.GetStance(country)}");

    if (marshal is not null)
    {
        var willingness = PoliticalCalculations.GetOrderWillingness(
            simulation.State,
            country,
            marshal,
            country.Ruler);

        Console.WriteLine(
            $"Marshal: {marshal.FullName} — ability {DescribeLevel(marshal.Competence)}, " +
            $"obedience {DescribeWillingness(willingness)}");
    }
    else
    {
        Console.WriteLine("Marshal: vacant — the ruler is acting as commander.");
    }

    Console.WriteLine();
    if (marshal is not null)
    {
        Console.WriteLine("[S] Change campaign stance");
        Console.WriteLine("[I] Request fresh military intelligence on the enemy");
    }

    if (chancellor is not null)
        Console.WriteLine("[P] Propose peace");

    Console.WriteLine("[Enter] Cancel");
    Console.Write("Action: ");

    var action = Console.ReadLine()?.Trim();

    if (string.Equals(action, "i", StringComparison.OrdinalIgnoreCase) &&
        marshal is not null)
    {
        simulation.SubmitOrder(new RequestReportOrder
        {
            Issuer = country.Ruler,
            Recipient = marshal,
            IssuedOn = simulation.State.Date,
            Country = country,
            Topic = InformationTopic.Military,
            SubjectCountry = enemy,
            RelatedCountry = country
        });

        Pause(
            $"Military intelligence request on {enemy.Name} sent to {marshal.FullName}. " +
            "It may take several months, and the result may still be wrong.");
        return;
    }

    if (string.Equals(action, "p", StringComparison.OrdinalIgnoreCase) &&
        chancellor is not null)
    {
        QueuePeaceOffer(simulation, country, selected, chancellor);
        return;
    }

    if (!string.Equals(action, "s", StringComparison.OrdinalIgnoreCase) ||
        marshal is null)
    {
        return;
    }

    Console.WriteLine();
    Console.WriteLine("[1] Defensive — lower losses, weaker pressure");
    Console.WriteLine("[2] Balanced");
    Console.WriteLine("[3] Aggressive — stronger pressure, higher losses/cost");
    Console.WriteLine("[Enter] Cancel");
    Console.Write("Order stance: ");

    var input = Console.ReadLine()?.Trim();

    var stance = input switch
    {
        "1" => WarStance.Defensive,
        "2" => WarStance.Balanced,
        "3" => WarStance.Aggressive,
        _ => (WarStance?)null
    };

    if (!stance.HasValue)
        return;

    simulation.SubmitOrder(new SetWarStanceOrder
    {
        Issuer = country.Ruler,
        Recipient = marshal,
        IssuedOn = simulation.State.Date,
        Country = country,
        War = selected,
        RequestedStance = stance.Value
    });

    Pause(
        $"{stance.Value} campaign directive queued through {marshal.FullName}. " +
        "A sufficiently unwilling Marshal may refuse or moderate the instruction.");
}

static void QueuePeaceOffer(
    GameSimulation simulation,
    Country country,
    War war,
    Character chancellor)
{
    var enemy = war.OpponentOf(country);

    Console.Clear();
    Console.WriteLine($"Peace with {enemy.Name}");
    Console.WriteLine(new string('=', 11 + enemy.Name.Length));
    Console.WriteLine();
    Console.WriteLine(
        "The Chancellor can estimate the foreign government's position, but the " +
        "acceptance calculation itself is not shown.");

    var options = new[]
    {
        PeaceOfferTerms.WhitePeace,
        PeaceOfferTerms.DemandReparations,
        PeaceOfferTerms.OfferReparations
    };

    for (var i = 0; i < options.Length; i++)
    {
        var score = PeaceCalculations.GetAcceptanceScore(
            simulation.State,
            war,
            country,
            options[i]);

        var assessment = score switch
        {
            >= 75 => "likely acceptable",
            >= 55 => "plausibly acceptable",
            >= 40 => "unlikely",
            _ => "very unlikely"
        };

        Console.WriteLine($"[{i + 1}] {options[i],-18} — {assessment}");
    }

    Console.WriteLine();
    Console.Write("Choose terms: ");

    if (!int.TryParse(Console.ReadLine(), out var number) ||
        number < 1 ||
        number > options.Length)
    {
        Pause("Invalid peace terms.");
        return;
    }

    var terms = options[number - 1];

    simulation.SubmitOrder(new OfferPeaceOrder
    {
        Issuer = country.Ruler,
        Recipient = chancellor,
        IssuedOn = simulation.State.Date,
        Country = country,
        War = war,
        Terms = terms
    });

    Pause(
        $"{terms} proposal queued through {chancellor.FullName}. " +
        $"{enemy.Name} may accept or reject it.");
}

static void ManagePrison(GameSimulation simulation, Country country)
{
    Console.Clear();
    Console.WriteLine("Prison and arrests");
    Console.WriteLine("==================");
    Console.WriteLine();
    Console.WriteLine("[1] Order an arrest");
    Console.WriteLine("[2] Release a prisoner");
    Console.WriteLine("[Enter] Cancel");
    Console.Write("Choose action: ");

    var input = Console.ReadLine()?.Trim();

    if (input == "1")
        QueueArrestOrder(simulation, country);
    else if (input == "2")
        QueueReleaseOrder(simulation, country);
}

static void QueueArrestOrder(GameSimulation simulation, Country country)
{
    var marshal = country.GetOfficeHolder(Position.Marshal);

    if (marshal is null)
    {
        Pause("There is no active Marshal to carry out an arrest.");
        return;
    }

    var subjects = country.PoliticalFigures
        .Where(character =>
            character.IsPoliticallyActive &&
            !ReferenceEquals(character, country.Ruler) &&
            !ReferenceEquals(character, marshal))
        .OrderByDescending(character =>
            PoliticalCalculations.GetThreatScore(simulation.State, country, character))
        .ToList();

    if (subjects.Count == 0)
    {
        Pause("There is nobody available to arrest.");
        return;
    }

    Console.Clear();
    Console.WriteLine($"Arrest order through {marshal.FullName}");
    Console.WriteLine(new string('=', 21 + marshal.FullName.Length));
    Console.WriteLine();
    Console.WriteLine(
        $"Reported army readiness: {FormatKnown(simulation.State, InformationMetric.ArmyReadiness, country.Id)}. " +
        "A failed arrest can strengthen the target and destabilise the government.");
    Console.WriteLine();

    for (var i = 0; i < subjects.Count; i++)
    {
        var subject = subjects[i];
        var threat = PoliticalCalculations.GetThreatScore(
            simulation.State,
            country,
            subject);

        var plot = simulation.State.Plots.FirstOrDefault(candidate =>
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Country, country) &&
            ReferenceEquals(candidate.Instigator, subject));

        var evidence = plot?.DiscoveryStage switch
        {
            >= 2 => "credible evidence",
            1 => "rumours",
            _ => "no evidence"
        };

        Console.WriteLine(
            $"[{i + 1}] {subject.FullName,-20} " +
            $"influence {DescribeLevel(subject.Influence),-10}  " +
            $"risk {DescribeThreat(threat),-9}  {evidence}");
    }

    Console.WriteLine();
    Console.Write("Choose subject: ");

    if (!int.TryParse(Console.ReadLine(), out var number) ||
        number < 1 ||
        number > subjects.Count)
    {
        Pause("Invalid choice.");
        return;
    }

    var subjectToArrest = subjects[number - 1];

    simulation.SubmitOrder(new ArrestCharacterOrder
    {
        Issuer = country.Ruler,
        Recipient = marshal,
        Subject = subjectToArrest,
        IssuedOn = simulation.State.Date,
        Country = country
    });

    Pause(
        $"Arrest of {subjectToArrest.FullName} queued through {marshal.FullName}. " +
        "Without credible evidence, even a successful arrest will cost legitimacy.");
}

static void QueueReleaseOrder(GameSimulation simulation, Country country)
{
    var prisoners = country.Prisoners.ToList();

    if (prisoners.Count == 0)
    {
        Pause("There are no political prisoners to release.");
        return;
    }

    Console.Clear();
    Console.WriteLine("Release a prisoner");
    Console.WriteLine("==================");
    Console.WriteLine();

    for (var i = 0; i < prisoners.Count; i++)
    {
        var prisoner = prisoners[i];
        var relationship = simulation.State.Relationships.GetOrCreate(
            prisoner,
            country.Ruler);

        Console.WriteLine(
            $"[{i + 1}] {prisoner.FullName,-20} " +
            $"influence {DescribeLevel(prisoner.Influence),-10}  " +
            $"{DescribeOpinion(relationship.Opinion),-18}  fear {DescribeFear(relationship.Fear)}");
    }

    Console.WriteLine();
    Console.Write("Choose prisoner: ");

    if (!int.TryParse(Console.ReadLine(), out var number) ||
        number < 1 ||
        number > prisoners.Count)
    {
        Pause("Invalid choice.");
        return;
    }

    var prisonerToRelease = prisoners[number - 1];

    simulation.SubmitOrder(new ReleasePrisonerOrder
    {
        Issuer = country.Ruler,
        Recipient = prisonerToRelease,
        IssuedOn = simulation.State.Date,
        Country = country
    });

    Pause($"Release of {prisonerToRelease.FullName} queued.");
}

static void PrintPoliticalGroups(GameState state)
{
    Console.Clear();

    var country = state.Player.Country;
    var ruler = country.Ruler;

    Console.WriteLine($"Political groups of {country.Name}");
    Console.WriteLine(new string('=', 20 + country.Name.Length));
    Console.WriteLine();
    Console.WriteLine(
        "This is a political reading, not a census of hidden simulation values. " +
        "Group importance and loyalty are shown qualitatively.");
    Console.WriteLine();

    foreach (var powerBase in Enum.GetValues<PowerBaseType>()
                 .Where(powerBase => country.GetPowerBaseStrength(powerBase) > 0)
                 .OrderByDescending(powerBase => country.GetPowerBaseStrength(powerBase)))
    {
        var strength = country.GetPowerBaseStrength(powerBase);
        var backing = ruler.GetPowerBaseStanding(powerBase);

        var rival = country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, ruler))
            .OrderByDescending(character => character.GetPowerBaseStanding(powerBase))
            .ThenByDescending(character => character.Influence)
            .FirstOrDefault();

        var rivalText = rival is null
            ? "no obvious alternative"
            : $"best-known alternative: {rival.FullName}";

        Console.WriteLine(
            $"{FormatPowerBase(powerBase),-18} " +
            $"{DescribeStructuralImportance(strength),-10}  " +
            $"ruler backing {DescribeBacking(backing),-12}  {rivalText}");
    }

    Console.WriteLine();
    Console.WriteLine(
        $"Overall political backing: {FormatKnown(state, InformationMetric.PoliticalBacking, country.Id)}");
    Console.WriteLine(
        $"Reported unrest: {FormatKnown(state, InformationMetric.PublicUnrest, country.Id)}");
    Console.WriteLine(
        $"Reported stability: {FormatKnown(state, InformationMetric.GovernmentStability, country.Id)}");

    var demands = state.PowerBaseDemands
        .Where(candidate =>
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Country, country))
        .OrderByDescending(candidate => candidate.EscalationLevel)
        .ThenByDescending(candidate => candidate.MonthsOpen)
        .ToList();

    if (demands.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Known active demands:");

        foreach (var demand in demands)
        {
            Console.WriteLine(
                $"  {FormatPowerBase(demand.PowerBase),-18} " +
                $"{DescribeDemandBrief(demand)} — " +
                $"{DescribeEscalation(demand.EscalationLevel)} pressure");
        }
    }

    var bloc = state.PoliticalBlocs.FirstOrDefault(candidate =>
        candidate.IsActive &&
        ReferenceEquals(candidate.Country, country));

    if (bloc is not null)
    {
        var members = country.PoliticalFigures
            .Where(character => bloc.MemberIds.Contains(character.Id))
            .Select(character => character.FullName)
            .ToList();

        Console.WriteLine();
        Console.WriteLine("Known organised opposition:");
        Console.WriteLine(
            $"  Leader: {bloc.Leader.FullName}   cohesion appears {DescribeLevel(bloc.Cohesion)}");
        Console.WriteLine(
            $"  Backed by: {FormatPowerBaseList(bloc.PowerBases)}");
        Console.WriteLine(
            members.Count == 0
                ? "  No other major political figures are clearly aligned."
                : $"  Known aligned figures: {string.Join(", ", members)}");
    }

    Console.WriteLine();
    Console.WriteLine(
        "For a fresher overall domestic assessment, request a report from the Chancellor with K.");
    Pause();
}


static string FormatPowerBase(PowerBaseType powerBase)
{
    return powerBase switch
    {
        PowerBaseType.RegionalElites => "Regional elites",
        PowerBaseType.RoyalFamily => "Royal family",
        _ => powerBase.ToString()
    };
}

static string FormatPowerBaseList(IEnumerable<PowerBaseType> powerBases)
{
    var names = powerBases
        .Select(FormatPowerBase)
        .OrderBy(name => name)
        .ToList();

    if (names.Count == 0)
        return "no major groups";

    if (names.Count == 1)
        return names[0];

    return string.Join(", ", names.Take(names.Count - 1)) +
           " and " + names[^1];
}

static string DescribeDemandBrief(PowerBaseDemand demand)
{
    return demand.Type switch
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
}

static void PrintCourt(GameState state)
{
    Console.Clear();

    var country = state.Player.Country;
    var countryKey = PoliticalKeys.Country(country.Id);
    var lineageKey = PoliticalKeys.Lineage(state.Player.Lineage.Id);

    Console.WriteLine($"Court of {country.Name}");
    Console.WriteLine(new string('=', 9 + country.Name.Length));
    Console.WriteLine();
    Console.WriteLine(
        "These are the ruler's political impressions. Personality, loyalty and threat " +
        "are deliberately qualitative rather than exact hidden-state meters.");
    Console.WriteLine();

    var knownPlots = state.Plots
        .Where(plot =>
            !plot.IsResolved &&
            plot.DiscoveryStage > 0 &&
            ReferenceEquals(plot.Country, country))
        .ToList();

    if (knownPlots.Count > 0)
    {
        Console.WriteLine("Known political threats:");

        foreach (var plot in knownPlots)
        {
            var certainty = plot.DiscoveryStage >= 2
                ? "credible evidence"
                : "rumours";

            Console.WriteLine($"  {plot.Instigator.FullName}: {certainty}");

            if (plot.DiscoveryStage >= 2 && plot.SupporterIds.Count > 0)
            {
                var supporters = country.PoliticalFigures
                    .Where(character => plot.SupporterIds.Contains(character.Id))
                    .Select(character => character.FullName);

                Console.WriteLine($"    Suspected supporters: {string.Join(", ", supporters)}");
            }
        }

        Console.WriteLine();
    }

    var figures = country.PoliticalFigures
        .Where(character => character.IsAlive)
        .OrderByDescending(character =>
            ReferenceEquals(character, country.Ruler)
                ? 101
                : PoliticalCalculations.GetThreatScore(state, country, character))
        .ToList();

    foreach (var character in figures)
    {
        var role = ReferenceEquals(character, country.Ruler)
            ? "Ruler"
            : character.Status == PoliticalStatus.Imprisoned
                ? "Imprisoned"
                : character.Status == PoliticalStatus.Exiled
                    ? "Exiled"
                    : character.Position?.ToString() ?? "Courtier";

        Console.WriteLine($"{character.FullName} — {role}");
        Console.WriteLine(
            $"  Age {character.Age}   health {DescribeHealth(character.Health)}   " +
            $"ability {DescribeLevel(character.Competence)}   ambition {DescribeAmbition(character.Ambition)}");
        Console.WriteLine(
            $"  Influence {DescribeLevel(character.Influence)}   " +
            $"legitimacy {DescribeLevel(character.Legitimacy)}");

        if (!ReferenceEquals(character, country.Ruler))
        {
            var relationship = state.Relationships.GetOrCreate(character, country.Ruler);
            var willingness = PoliticalCalculations.GetOrderWillingness(
                state,
                country,
                character,
                country.Ruler);
            var threat = PoliticalCalculations.GetThreatScore(state, country, character);

            Console.WriteLine(
                $"  Toward ruler: {DescribeOpinion(relationship.Opinion)}, " +
                $"trust {DescribeTrust(relationship.Trust)}, fear {DescribeFear(relationship.Fear)}, " +
                $"obedience {DescribeWillingness(willingness)}");
            Console.WriteLine(
                $"  Allegiance: country {DescribeBacking(character.GetAllegiance(countryKey))}, " +
                $"{state.Player.Lineage.Name} {DescribeBacking(character.GetAllegiance(lineageKey))}; " +
                $"political risk {DescribeThreat(threat)}");
        }

        var strongestBases = Enum.GetValues<PowerBaseType>()
            .OrderByDescending(character.GetPowerBaseStanding)
            .Take(2)
            .Select(powerBase =>
                $"{FormatPowerBase(powerBase)} ({DescribeBacking(character.GetPowerBaseStanding(powerBase))})");

        Console.WriteLine($"  Strongest apparent constituencies: {string.Join(", ", strongestBases)}");
        Console.WriteLine();
    }

    Pause();
}


static void ManageInformation(GameSimulation simulation, Country country)
{
    Console.Clear();
    Console.WriteLine("Intelligence and adviser reports");
    Console.WriteLine("===============================");
    Console.WriteLine();
    Console.WriteLine(
        "These are the ruler's working estimates, not the simulation's true values. " +
        "Advisers can be mistaken, delayed, overconfident or deliberately misleading.");
    Console.WriteLine();

    var pending = simulation.State.InformationRequests
        .Where(request => request.Status == InformationRequestStatus.Pending)
        .OrderBy(request => request.RequestedOn.Year)
        .ThenBy(request => request.RequestedOn.Month)
        .ToList();

    if (pending.Count > 0)
    {
        Console.WriteLine("Pending inquiries:");

        foreach (var request in pending)
        {
            var targetText = ReferenceEquals(request.SubjectCountry, country)
                ? country.Name
                : request.SubjectCountry.Name;

            Console.WriteLine(
                $"  {request.Advisor.FullName}: {InformationSystem.FormatTopic(request.Topic)} " +
                $"— {targetText}; underway since {request.RequestedOn}");
        }

        Console.WriteLine();
    }

    var recent = simulation.State.AdvisorReports
        .TakeLast(6)
        .Reverse()
        .ToList();

    if (recent.Count > 0)
    {
        Console.WriteLine("Recent adviser reports:");

        for (var i = 0; i < recent.Count; i++)
        {
            var report = recent[i];
            var age = MonthsBetween(report.DataAsOf, simulation.State.Date);

            Console.WriteLine(
                $"  [{i + 1}] {report.Title} — data {FormatAge(age)}");
        }

        Console.WriteLine();
    }

    Console.WriteLine("[1] Request economic report from Treasurer");
    Console.WriteLine("[2] Request report on our military from Marshal");
    Console.WriteLine("[3] Request military intelligence on another country");
    Console.WriteLine("[4] Request foreign-affairs assessment from Chancellor");
    Console.WriteLine("[5] Request domestic-political assessment from Chancellor");
    if (recent.Count > 0)
        Console.WriteLine("[V] View a recent adviser report");
    Console.WriteLine("[Enter] Return");
    Console.Write("Action: ");

    var input = Console.ReadLine()?.Trim();

    if (string.Equals(input, "v", StringComparison.OrdinalIgnoreCase) &&
        recent.Count > 0)
    {
        Console.Write("Report number: ");

        if (int.TryParse(Console.ReadLine(), out var reportNumber) &&
            reportNumber >= 1 &&
            reportNumber <= recent.Count)
        {
            PrintAdvisorReport(simulation.State, recent[reportNumber - 1]);
        }

        return;
    }

    if (input is not ("1" or "2" or "3" or "4" or "5"))
        return;

    Character? advisor;
    InformationTopic topic;
    Country subject = country;
    Country? related = null;

    switch (input)
    {
        case "1":
            advisor = country.GetOfficeHolder(Position.Treasurer);
            topic = InformationTopic.Economy;
            break;

        case "2":
            advisor = country.GetOfficeHolder(Position.Marshal);
            topic = InformationTopic.Military;
            break;

        case "3":
            advisor = country.GetOfficeHolder(Position.Marshal);
            topic = InformationTopic.Military;
            subject = ChooseForeignCountry(simulation.State, country) ?? country;

            if (ReferenceEquals(subject, country))
                return;

            related = country;
            break;

        case "4":
            advisor = country.GetOfficeHolder(Position.Chancellor);
            topic = InformationTopic.ForeignAffairs;
            subject = ChooseForeignCountry(simulation.State, country) ?? country;

            if (ReferenceEquals(subject, country))
                return;

            related = country;
            break;

        default:
            advisor = country.GetOfficeHolder(Position.Chancellor);
            topic = InformationTopic.DomesticPolitics;
            break;
    }

    if (advisor is null)
    {
        Pause("The responsible office is vacant, so there is nobody to task with that report.");
        return;
    }

    simulation.SubmitOrder(new RequestReportOrder
    {
        Issuer = country.Ruler,
        Recipient = advisor,
        IssuedOn = simulation.State.Date,
        Country = country,
        Topic = topic,
        SubjectCountry = subject,
        RelatedCountry = related
    });

    Pause(
        $"Request sent to {advisor.FullName}. They may refuse, delay the work, or " +
        "return information that is incomplete or wrong.");
}

static Country? ChooseForeignCountry(GameState state, Country country)
{
    var foreign = state.Countries
        .Where(candidate => !ReferenceEquals(candidate, country))
        .OrderBy(candidate => candidate.Name)
        .ToList();

    Console.WriteLine();

    for (var i = 0; i < foreign.Count; i++)
        Console.WriteLine($"[{i + 1}] {foreign[i].Name}");

    Console.Write("Country: ");

    if (!int.TryParse(Console.ReadLine(), out var number) ||
        number < 1 ||
        number > foreign.Count)
    {
        return null;
    }

    return foreign[number - 1];
}

static void PrintAdvisorReport(
    GameState state,
    AdvisorIntelligenceReport report)
{
    Console.Clear();
    Console.WriteLine(report.Title);
    Console.WriteLine(new string('=', report.Title.Length));
    Console.WriteLine();
    Console.WriteLine(
        $"From: {report.Advisor.FullName} ({report.Advisor.Position})");
    Console.WriteLine(
        $"Delivered: {report.ProducedOn}   Information describes: {report.DataAsOf} " +
        $"({FormatAge(MonthsBetween(report.DataAsOf, state.Date))})");
    Console.WriteLine(
        $"Origin: {(report.WasRequested ? "requested inquiry" : "adviser-initiated report")}");
    Console.WriteLine();
    Console.WriteLine(report.Summary);
    Console.WriteLine();

    foreach (var fact in report.Facts)
    {
        Console.WriteLine(
            $"{FormatMetricName(fact.Key.Metric),-28} " +
            $"{FormatKnownValue(fact.Key.Metric, fact.Estimate),16}  " +
            $"± {FormatKnownMargin(fact.Key.Metric, fact.Margin),-12} " +
            $"confidence {fact.ReportedConfidence,2}%");
    }

    if (report.Caveats.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Problems or contradictions visible in this report:");

        foreach (var caveat in report.Caveats)
            Console.WriteLine($"  - {caveat}");
    }

    Console.WriteLine();
    Console.WriteLine(
        "Confidence is the adviser's apparent confidence, not a guarantee that the estimate is honest or correct.");
    Pause();
}

static void PrintReports(GameState state)
{
    Console.Clear();
    Console.WriteLine("Recent events and order outcomes");
    Console.WriteLine("===============================");
    Console.WriteLine();

    var reports = state.Reports.TakeLast(20).ToList();

    if (reports.Count == 0)
    {
        Console.WriteLine("No reports yet.");
    }
    else
    {
        foreach (var report in reports)
        {
            Console.WriteLine($"[{report.Date}] {report.Title}");
            Console.WriteLine(report.Details);
            Console.WriteLine();
        }
    }

    Pause();
}

static string FormatKnown(
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
        return "unknown";

    var age = known.AgeInMonths(state.Date);
    var stale = age >= 6
        ? "STALE, "
        : string.Empty;

    return $"~{FormatKnownValue(metric, known.Estimate)} ±{FormatKnownMargin(metric, known.Margin)} " +
           $"[{stale}{known.SourceAdvisorName}, {FormatAge(age)}]";
}

static string FormatKnownShort(
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
        return "unknown";

    var age = known.AgeInMonths(state.Date);
    var stale = age >= 6 ? " stale" : string.Empty;

    return $"~{FormatKnownValue(metric, known.Estimate)} ({FormatAge(age)}{stale})";
}

static string FormatKnownValue(
    InformationMetric metric,
    double value)
{
    return metric switch
    {
        InformationMetric.Population or
        InformationMetric.Gdp or
        InformationMetric.Treasury or
        InformationMetric.Debt or
        InformationMetric.MonthlyTaxRevenue or
        InformationMetric.MonthlyTradeIncome or
        InformationMetric.MonthlyExpenses or
        InformationMetric.ArmySize =>
            $"{value:N0}",

        InformationMetric.MonthlyBalance =>
            $"{value:+#,##0;-#,##0;0}",

        InformationMetric.AdministrativeEfficiency or
        InformationMetric.ArmyReadiness or
        InformationMetric.WarExhaustion or
        InformationMetric.PublicUnrest or
        InformationMetric.GovernmentStability or
        InformationMetric.PoliticalBacking =>
            $"{value:F0}/100",

        InformationMetric.DiplomaticRelations or
        InformationMetric.WarScore =>
            $"{value:+0;-0;0}",

        InformationMetric.DiplomaticTrust or
        InformationMetric.DiplomaticTension =>
            $"{value:F0}/100",

        _ => $"{value:F0}"
    };
}

static string FormatKnownMargin(
    InformationMetric metric,
    double margin)
{
    margin = Math.Abs(margin);

    return metric switch
    {
        InformationMetric.Population or
        InformationMetric.Gdp or
        InformationMetric.Treasury or
        InformationMetric.Debt or
        InformationMetric.MonthlyTaxRevenue or
        InformationMetric.MonthlyTradeIncome or
        InformationMetric.MonthlyExpenses or
        InformationMetric.MonthlyBalance or
        InformationMetric.ArmySize =>
            $"{margin:N0}",

        _ => $"{margin:F0}"
    };
}

static string FormatMetricName(InformationMetric metric)
{
    return metric switch
    {
        InformationMetric.Gdp => "GDP",
        InformationMetric.MonthlyTaxRevenue => "Monthly tax revenue",
        InformationMetric.MonthlyTradeIncome => "Monthly trade income",
        InformationMetric.MonthlyExpenses => "Monthly expenses",
        InformationMetric.MonthlyBalance => "Monthly balance",
        InformationMetric.AdministrativeEfficiency => "Administrative efficiency",
        InformationMetric.ArmySize => "Army strength",
        InformationMetric.ArmyReadiness => "Army readiness",
        InformationMetric.WarExhaustion => "War exhaustion",
        InformationMetric.WarScore => "Campaign position",
        InformationMetric.PublicUnrest => "Public unrest",
        InformationMetric.GovernmentStability => "Government stability",
        InformationMetric.PoliticalBacking => "Ruler's political backing",
        InformationMetric.DiplomaticRelations => "Relations",
        InformationMetric.DiplomaticTrust => "Diplomatic trust",
        InformationMetric.DiplomaticTension => "Diplomatic tension",
        _ => metric.ToString()
    };
}

static int MonthsBetween(GameDate earlier, GameDate later)
{
    return Math.Max(
        0,
        (later.Year - earlier.Year) * 12 +
        later.Month -
        earlier.Month);
}

static string FormatAge(int months)
{
    return months switch
    {
        <= 0 => "current-ish",
        1 => "1 month old",
        _ => $"{months} months old"
    };
}

static string DescribeHealth(int health)
{
    return health switch
    {
        >= 90 => "good",
        >= 70 => "fair",
        >= 45 => "poor",
        >= 20 => "very poor",
        _ => "critical"
    };
}

static string DescribeAmbition(int ambition)
{
    return ambition switch
    {
        >= 85 => "extreme",
        >= 70 => "high",
        >= 50 => "noticeable",
        >= 30 => "modest",
        _ => "low"
    };
}

static string DescribeOpinion(int opinion)
{
    return opinion switch
    {
        >= 60 => "warm",
        >= 20 => "favourable",
        > -20 => "neutral",
        > -60 => "hostile",
        _ => "bitterly hostile"
    };
}

static string DescribeFear(int fear)
{
    return fear switch
    {
        >= 75 => "very high",
        >= 50 => "high",
        >= 25 => "noticeable",
        > 0 => "slight",
        _ => "none"
    };
}

static string DescribeStructuralImportance(int strength)
{
    return strength switch
    {
        >= 80 => "dominant",
        >= 60 => "major",
        >= 40 => "important",
        >= 20 => "secondary",
        _ => "minor"
    };
}

static string DescribeBacking(int backing)
{
    return backing switch
    {
        >= 75 => "strong",
        >= 60 => "supportive",
        >= 45 => "uncertain",
        >= 30 => "hostile",
        _ => "very hostile"
    };
}

static string DescribeEscalation(int escalation)
{
    return escalation switch
    {
        <= 0 => "initial",
        1 => "growing",
        2 => "serious",
        3 => "severe",
        _ => "critical"
    };
}

static string DescribeLevel(double value)
{
    return value switch
    {
        >= 85 => "exceptional",
        >= 70 => "strong",
        >= 55 => "solid",
        >= 40 => "mixed",
        >= 25 => "weak",
        _ => "very weak"
    };
}

static string DescribeTrust(int trust)
{
    return trust switch
    {
        >= 80 => "very high",
        >= 65 => "high",
        >= 45 => "mixed",
        >= 25 => "low",
        _ => "very low"
    };
}

static string DescribeWillingness(double willingness)
{
    return willingness switch
    {
        >= 80 => "very likely",
        >= 65 => "likely",
        >= 45 => "uncertain",
        >= 25 => "reluctant",
        _ => "hostile"
    };
}

static string DescribeThreat(double threat)
{
    return threat switch
    {
        >= 80 => "severe",
        >= 60 => "high",
        >= 40 => "moderate",
        >= 20 => "some",
        _ => "low"
    };
}

static void Pause(string? message = null)
{
    if (!string.IsNullOrWhiteSpace(message))
    {
        Console.WriteLine();
        Console.WriteLine(message);
    }

    Console.WriteLine();
    Console.Write("Press Enter to continue...");
    Console.ReadLine();
}
