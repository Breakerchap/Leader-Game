using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
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
    Console.WriteLine("[P] Prison and arrests");
    Console.WriteLine("[C] Inspect the court");
    Console.WriteLine("[R] Read recent reports");
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

    if (string.Equals(input, "p", StringComparison.OrdinalIgnoreCase))
    {
        ManagePrison(simulation, country);
        continue;
    }

    if (string.Equals(input, "c", StringComparison.OrdinalIgnoreCase))
    {
        PrintCourt(state);
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
    Console.WriteLine($"Population: {country.Population:N0}");
    Console.WriteLine($"GDP: {country.Gdp:N0}");
    Console.WriteLine($"Treasury: {country.Treasury:N0}");
    Console.WriteLine($"Debt: {country.Debt:N0}");

    if (country.LastMonthlyTaxRevenue != 0m || country.LastMonthlyExpenses != 0m)
    {
        Console.WriteLine(
            $"Last budget: revenue {country.LastMonthlyTaxRevenue:N0}, " +
            $"expenses {country.LastMonthlyExpenses:N0}, " +
            $"balance {country.LastMonthlyBalance:N0}");
    }

    Console.WriteLine($"Army: {country.ArmySize:N0} — readiness {country.ArmyReadiness:F0}/100");
    Console.WriteLine($"Tax rate: {country.TaxRate:P1}");
    Console.WriteLine($"Administrative efficiency: {country.AdministrativeEfficiency:P0}");
    Console.WriteLine(
        $"Funding: army {country.ArmyFunding:P0}, administration " +
        $"{country.AdministrationFunding:P0}, court {country.CourtFunding:P0}");
    Console.WriteLine($"Public unrest: {country.PublicUnrest:F1}");
    Console.WriteLine($"Stability: {country.Government.Stability:F1}");
    Console.WriteLine($"Pending orders: {state.PendingOrders.Count}");

    var knownPlots = state.Plots.Count(plot =>
        !plot.IsResolved &&
        plot.DiscoveryStage > 0 &&
        ReferenceEquals(plot.Country, country));

    if (knownPlots > 0)
        Console.WriteLine($"Known political threats: {knownPlots}");

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
            $"Comp {advisor.Competence,3}  " +
            $"Trust {relationship.Trust,3}  " +
            $"Will {willingness,5:F0}  " +
            $"Threat {threat,5:F0}");
    }

    var candidates = country.AvailableAdvisors.ToList();

    if (candidates.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"Available political figures: {candidates.Count} — press C to inspect.");
    }
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
        $"{treasurer.FullName}'s current willingness to obey: {willingness:F0}/100.");
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
        $"Army readiness {country.ArmyReadiness:F0}/100, administrative efficiency " +
        $"{country.AdministrativeEfficiency:P0}, debt {country.Debt:N0}");
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

        Console.WriteLine(
            $"[{i + 1}] {candidate.FullName,-20} " +
            $"Comp {candidate.Competence,3}  " +
            $"Amb {candidate.Ambition,3}  " +
            $"Trust {relationship.Trust,3}  " +
            $"Infl {candidate.Influence,3}  " +
            $"Threat {threat,5:F0}");
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
            $"Opinion {relationship.Opinion,4}  Trust {relationship.Trust,3}  " +
            $"Influence {character.Influence,3}");
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
            $"Influence {subject.Influence,3}  Threat {threat,5:F0}");
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
        $"Army readiness: {country.ArmyReadiness:F0}/100. A failed arrest can strengthen " +
        "the target and destabilise the government.");
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
            $"Influence {subject.Influence,3}  Threat {threat,5:F0}  {evidence}");
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
            $"Influence {prisoner.Influence,3}  Opinion {relationship.Opinion,4}  " +
            $"Fear {relationship.Fear,3}");
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
        "Opinion is personal feeling; trust is reliability; fear can force obedience. " +
        "Threat is political danger, not a coup probability.");
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
            $"  Age {character.Age,3}   Health {character.Health,3}   " +
            $"Competence {character.Competence,3}   Ambition {character.Ambition,3}");
        Console.WriteLine(
            $"  Influence {character.Influence,3}   Legitimacy {character.Legitimacy,3}   " +
            $"Status {character.Status}");

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
                $"  Toward ruler: opinion {relationship.Opinion,4}   trust {relationship.Trust,3}   " +
                $"fear {relationship.Fear,3}   willingness {willingness,5:F0}");
            Console.WriteLine(
                $"  Allegiance: country {character.GetAllegiance(countryKey),3}   " +
                $"{state.Player.Lineage.Name} {character.GetAllegiance(lineageKey),3}   " +
                $"threat {threat,5:F0}");
        }

        Console.WriteLine();
    }

    Pause();
}

static void PrintReports(GameState state)
{
    Console.Clear();
    Console.WriteLine("Recent reports");
    Console.WriteLine("==============");
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
