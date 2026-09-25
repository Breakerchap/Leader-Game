using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;

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
            Legitimacy = 78,
            Influence = 100
        };

        var treasurer = new Character
        {
            Id = 2,
            FirstName = "Johann",
            LastName = "Keller",
            Age = 53,
            Competence = 84,
            Ambition = 32,
            Influence = 63,
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
            Influence = 76,
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
            Influence = 72,
            Position = Position.Chancellor
        };

        var marta = new Character
        {
            Id = 5,
            FirstName = "Marta",
            LastName = "Vogel",
            Age = 39,
            Competence = 69,
            Ambition = 35,
            Influence = 38
        };

        var lukas = new Character
        {
            Id = 6,
            FirstName = "Lukas",
            LastName = "Hartmann",
            Age = 45,
            Competence = 94,
            Ambition = 87,
            Influence = 68,
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
            Influence = 44,
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

        var countryKey = PoliticalKeys.Country(country.Id);
        var lineageKey = PoliticalKeys.Lineage(lineage.Id);

        king.SetAllegiance(countryKey, 90);
        king.SetAllegiance(lineageKey, 100);

        treasurer.SetAllegiance(countryKey, 78);
        treasurer.SetAllegiance(lineageKey, 62);

        marshal.SetAllegiance(countryKey, 82);
        marshal.SetAllegiance(lineageKey, 47);

        chancellor.SetAllegiance(countryKey, 86);
        chancellor.SetAllegiance(lineageKey, 79);

        marta.SetAllegiance(countryKey, 76);
        marta.SetAllegiance(lineageKey, 88);

        lukas.SetAllegiance(countryKey, 70);
        lukas.SetAllegiance(lineageKey, 28);

        heir.SetAllegiance(countryKey, 85);
        heir.SetAllegiance(lineageKey, 100);

        state.Relationships.Set(treasurer, king, opinion: 35, trust: 62, fear: 20);
        state.Relationships.Set(marshal, king, opinion: 5, trust: 43, fear: 32);
        state.Relationships.Set(chancellor, king, opinion: 52, trust: 76, fear: 12);
        state.Relationships.Set(marta, king, opinion: 67, trust: 81, fear: 5);
        state.Relationships.Set(lukas, king, opinion: -22, trust: 29, fear: 11);
        state.Relationships.Set(heir, king, opinion: 74, trust: 84, fear: 8);

        // The reverse direction matters too; relationships are deliberately asymmetric.
        state.Relationships.Set(king, treasurer, opinion: 38, trust: 67, fear: 0);
        state.Relationships.Set(king, marshal, opinion: 18, trust: 48, fear: 4);
        state.Relationships.Set(king, chancellor, opinion: 57, trust: 78, fear: 0);
        state.Relationships.Set(king, marta, opinion: 41, trust: 58, fear: 0);
        state.Relationships.Set(king, lukas, opinion: -8, trust: 31, fear: 8);
        state.Relationships.Set(king, heir, opinion: 79, trust: 86, fear: 0);

        return state;
    }
}
