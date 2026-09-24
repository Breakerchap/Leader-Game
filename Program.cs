using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Player;

var king = new Ruler
{
    Id = 1,
    FirstName = "Friedrich",
    LastName = "von Falken",
    Age = 41,
    Competence = 64,
    Ambition = 72,
    Legitimacy = 78
};

var treasurer = new Advisor
{
    Id = 2,
    FirstName = "Johann",
    LastName = "Keller",
    Age = 53,
    Competence = 84,
    Ambition = 32,
    Loyalty = 61,
    Position = Position.Treasurer
};

var marshal = new Advisor
{
    Id = 3,
    FirstName = "Otto",
    LastName = "Bauer",
    Age = 47,
    Competence = 91,
    Ambition = 76,
    Loyalty = 43,
    Position = Position.Marshal
};

var chancellor = new Advisor
{
    Id = 4,
    FirstName = "Konrad",
    LastName = "Stein",
    Age = 58,
    Competence = 73,
    Ambition = 41,
    Loyalty = 81,
    Position = Position.Chancellor
};

var country = new Country
{
    Id = "falkenreich",
    Name = "Falkenreich",

    Population = 1_250_000,

    Gdp = 85_000_000m,
    Treasury = 320_000m,

    ArmySize = 8_200,

    Government = new Government
    {
        Type = GovernmentType.FeudalMonarchy,
        Stability = 67
    },

    Ruler = king
};

country.Advisors.Add(treasurer);
country.Advisors.Add(marshal);
country.Advisors.Add(chancellor);

var player = new PlayerState
{
    CurrentCharacter = king,
    Country = country
};

var state = new GameState
{
    Date = new GameDate(1450, 1),
    Player = player
};

state.Countries.Add(country);

var simulation = new GameSimulation(state);

while (true)
{
    Console.Clear();

    Console.WriteLine($"{country.Name} — {state.Date}");
    Console.WriteLine();
    Console.WriteLine($"Ruler: {country.Ruler.FullName}");
    Console.WriteLine($"Population: {country.Population:N0}");
    Console.WriteLine($"Treasury: {country.Treasury:N0}");
    Console.WriteLine($"Army: {country.ArmySize:N0}");
    Console.WriteLine($"Stability: {country.Government.Stability:F0}");
    Console.WriteLine();

    Console.WriteLine("Advisors:");

    foreach (var advisor in country.Advisors)
    {
        Console.WriteLine(
            $"{advisor.Position,-12} " +
            $"{advisor.FullName,-20} " +
            $"Competence {advisor.Competence,3}  " +
            $"Loyalty {advisor.Loyalty,3}  " +
            $"Ambition {advisor.Ambition,3}"
        );
    }

    Console.WriteLine();
    Console.WriteLine("[Enter] Advance month");
    Console.WriteLine("[Q] Quit");

    var input = Console.ReadLine();

    if (input?.Equals("q", StringComparison.OrdinalIgnoreCase) == true)
        break;

    simulation.AdvanceMonth();
}