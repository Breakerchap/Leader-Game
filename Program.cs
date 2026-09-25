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
    Console.WriteLine("[A] Appoint or replace an adviser");
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

    if (string.Equals(input, "a", StringComparison.OrdinalIgnoreCase))
    {
        QueueAppointmentOrder(simulation, country);
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
    Console.WriteLine($"Ruler: {country.Ruler.FullName}");
    Console.WriteLine($"Population: {country.Population:N0}");
    Console.WriteLine($"GDP: {country.Gdp:N0}");
    Console.WriteLine($"Treasury: {country.Treasury:N0}");
    Console.WriteLine($"Army: {country.ArmySize:N0}");
    Console.WriteLine($"Tax rate: {country.TaxRate:P1}");
    Console.WriteLine($"Administrative efficiency: {country.AdministrativeEfficiency:P0}");
    Console.WriteLine($"Public unrest: {country.PublicUnrest:F1}");
    Console.WriteLine($"Stability: {country.Government.Stability:F1}");
    Console.WriteLine($"Pending orders: {state.PendingOrders.Count}");
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
            : character.Position?.ToString() ?? "Courtier";

        Console.WriteLine($"{character.FullName} — {role}");
        Console.WriteLine(
            $"  Competence {character.Competence,3}   Ambition {character.Ambition,3}   " +
            $"Influence {character.Influence,3}   Legitimacy {character.Legitimacy,3}");

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

    var reports = state.Reports.TakeLast(16).ToList();

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
