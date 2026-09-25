using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Player;

namespace LeaderGame.Simulation.Scenarios;

public static class DemoScenario
{
    public static GameState Create()
    {
        var king = new Character
        {
            Id = 1,
            FirstName = "Friedrich",
            LastName = "von Falken",
            Age = 41,
            Competence = 64,
            Ambition = 72,
            Loyalty = 100,
            Legitimacy = 78
        };

        var treasurer = new Character
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

        var marshal = new Character
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

        var chancellor = new Character
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

        // Two deliberately different candidates: one is loyal but ordinary,
        // the other brilliant but ambitious and personally unreliable.
        var marta = new Character
        {
            Id = 5,
            FirstName = "Marta",
            LastName = "Vogel",
            Age = 39,
            Competence = 69,
            Ambition = 35,
            Loyalty = 91
        };

        var lukas = new Character
        {
            Id = 6,
            FirstName = "Lukas",
            LastName = "Hartmann",
            Age = 45,
            Competence = 94,
            Ambition = 87,
            Loyalty = 33,
            Legitimacy = 28
        };

        var heir = new Character
        {
            Id = 7,
            FirstName = "Heinrich",
            LastName = "von Falken",
            Age = 19,
            Competence = 55,
            Ambition = 58,
            Loyalty = 88,
            Legitimacy = 72
        };

        var country = new Country
        {
            Id = "falkenreich",
            Name = "Falkenreich",
            Population = 1_250_000,
            Gdp = 85_000_000m,
            Treasury = 320_000m,
            ArmySize = 8_200,
            TaxRate = 0.10m,
            AdministrativeEfficiency = 0.75m,
            PublicUnrest = 20,
            Government = new Government
            {
                Type = GovernmentType.FeudalMonarchy,
                Stability = 67
            },
            Ruler = king
        };

        country.PoliticalFigures.AddRange(
            [king, treasurer, marshal, chancellor, marta, lukas, heir]);

        // For the prototype this ordering is explicit. Later it should be derived
        // from laws, family relationships, claims, elections, coups and other systems.
        country.SuccessionOrder.AddRange([heir, lukas]);

        var lineage = new PoliticalLineage
        {
            Id = "house_von_falken",
            Name = "House von Falken",
            Type = PoliticalLineageType.Dynasty
        };
        lineage.Members.AddRange([king, heir]);

        var player = new PlayerState
        {
            CurrentCharacter = king,
            Country = country,
            Lineage = lineage
        };

        var state = new GameState
        {
            Date = new GameDate(1450, 1),
            Player = player
        };

        state.Countries.Add(country);

        return state;
    }
}
