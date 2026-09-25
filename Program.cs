using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Scenarios;

var state = DemoScenario.Create();
var simulation = new GameSimulation(state);

while (true)
{
    var country = state.Player.Country;

    Console.Clear();
    PrintDashboard(state);

    Console.WriteLine();
    Console.WriteLine("[Enter] Advance month");
    Console.WriteLine("[T] Order a tax-rate change");
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

    Console.WriteLine("Advisors:");

    foreach (var advisor in country.Advisors)
    {
        Console.WriteLine(
            $"{advisor.Position,-12} " +
            $"{advisor.FullName,-20} " +
            $"Competence {advisor.Competence,3}  " +
            $"Loyalty {advisor.Loyalty,3}  " +
            $"Ambition {advisor.Ambition,3}");
    }
}

static void QueueTaxOrder(GameSimulation simulation, LeaderGame.Simulation.Countries.Country country)
{
    var treasurer = country.GetAdvisor(Position.Treasurer);

    if (treasurer is null)
    {
        Pause("There is no living Treasurer to receive the order.");
        return;
    }

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

static void PrintReports(GameState state)
{
    Console.Clear();
    Console.WriteLine("Recent reports");
    Console.WriteLine("==============");
    Console.WriteLine();

    var reports = state.Reports.TakeLast(12).ToList();

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
