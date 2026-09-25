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

        var falkenreich = new Country
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

        falkenreich.PoliticalFigures.AddRange(
            [king, treasurer, marshal, chancellor, marta, lukas, heir]);
        falkenreich.SuccessionOrder.AddRange([heir, lukas]);

        var nordmarkQueen = new Character
        {
            Id = 101,
            FirstName = "Ingrid",
            LastName = "af Skeld",
            Age = 46,
            Competence = 71,
            Ambition = 56,
            Legitimacy = 84,
            Influence = 100
        };

        var nordmarkChancellor = new Character
        {
            Id = 102,
            FirstName = "Erik",
            LastName = "Lund",
            Age = 51,
            Competence = 79,
            Ambition = 37,
            Influence = 74,
            Position = Position.Chancellor
        };

        var nordmarkTreasurer = new Character
        {
            Id = 103,
            FirstName = "Freja",
            LastName = "Holm",
            Age = 44,
            Competence = 83,
            Ambition = 29,
            Influence = 64,
            Position = Position.Treasurer
        };

        var nordmarkMarshal = new Character
        {
            Id = 104,
            FirstName = "Sten",
            LastName = "Varg",
            Age = 49,
            Competence = 86,
            Ambition = 61,
            Influence = 77,
            Position = Position.Marshal
        };

        var nordmarkHeir = new Character
        {
            Id = 105,
            FirstName = "Astrid",
            LastName = "af Skeld",
            Age = 21,
            Competence = 62,
            Ambition = 49,
            Legitimacy = 77,
            Influence = 43
        };

        var nordmark = new Country
        {
            Id = "nordmark",
            Name = "Nordmark",
            Population = 980_000,
            Gdp = 62_000_000m,
            Treasury = 460_000m,
            ArmySize = 6_500,
            TaxRate = 0.11m,
            AdministrativeEfficiency = 0.78m,
            PublicUnrest = 14,
            Government = new Government
            {
                Type = GovernmentType.FeudalMonarchy,
                Stability = 74
            },
            Ruler = nordmarkQueen
        };

        nordmark.PoliticalFigures.AddRange(
            [nordmarkQueen, nordmarkChancellor, nordmarkTreasurer, nordmarkMarshal, nordmarkHeir]);
        nordmark.SuccessionOrder.Add(nordmarkHeir);

        var valerianDoge = new Character
        {
            Id = 201,
            FirstName = "Marco",
            LastName = "Vieri",
            Age = 55,
            Competence = 77,
            Ambition = 68,
            Legitimacy = 63,
            Influence = 100
        };

        var valerianChancellor = new Character
        {
            Id = 202,
            FirstName = "Lucia",
            LastName = "Bellandi",
            Age = 48,
            Competence = 88,
            Ambition = 52,
            Influence = 81,
            Position = Position.Chancellor
        };

        var valerianTreasurer = new Character
        {
            Id = 203,
            FirstName = "Paolo",
            LastName = "Serra",
            Age = 60,
            Competence = 90,
            Ambition = 44,
            Influence = 69,
            Position = Position.Treasurer
        };

        var valerianMarshal = new Character
        {
            Id = 204,
            FirstName = "Alessio",
            LastName = "Rossi",
            Age = 43,
            Competence = 82,
            Ambition = 74,
            Influence = 79,
            Position = Position.Marshal
        };

        var valerianSuccessor = new Character
        {
            Id = 205,
            FirstName = "Elena",
            LastName = "Conti",
            Age = 38,
            Competence = 81,
            Ambition = 66,
            Legitimacy = 58,
            Influence = 61
        };

        var valeria = new Country
        {
            Id = "valeria",
            Name = "Valeria",
            Population = 1_420_000,
            Gdp = 91_000_000m,
            Treasury = 690_000m,
            ArmySize = 7_300,
            TaxRate = 0.09m,
            AdministrativeEfficiency = 0.82m,
            PublicUnrest = 25,
            Government = new Government
            {
                Type = GovernmentType.Republic,
                Stability = 59
            },
            Ruler = valerianDoge
        };

        valeria.PoliticalFigures.AddRange(
            [valerianDoge, valerianChancellor, valerianTreasurer, valerianMarshal, valerianSuccessor]);
        valeria.SuccessionOrder.Add(valerianSuccessor);

        falkenreich.NeighborIds.UnionWith([nordmark.Id, valeria.Id]);
        nordmark.NeighborIds.UnionWith([falkenreich.Id, valeria.Id]);
        valeria.NeighborIds.UnionWith([falkenreich.Id, nordmark.Id]);

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
            Country = falkenreich,
            Lineage = lineage
        };

        var state = new GameState
        {
            Date = new GameDate(1450, 1),
            Player = player
        };

        state.Countries.AddRange([falkenreich, nordmark, valeria]);

        ConfigureFalkenreichPolitics(
            state,
            falkenreich,
            lineage,
            king,
            treasurer,
            marshal,
            chancellor,
            marta,
            lukas,
            heir);

        ConfigureForeignCourt(
            state,
            nordmark,
            nordmarkQueen,
            [
                (nordmarkChancellor, 86, 38, 76, 8),
                (nordmarkTreasurer, 90, 44, 82, 4),
                (nordmarkMarshal, 88, 24, 62, 12),
                (nordmarkHeir, 94, 69, 85, 3)
            ]);

        ConfigureForeignCourt(
            state,
            valeria,
            valerianDoge,
            [
                (valerianChancellor, 82, 28, 66, 6),
                (valerianTreasurer, 87, 36, 74, 4),
                (valerianMarshal, 84, 11, 52, 10),
                (valerianSuccessor, 79, 19, 59, 5)
            ]);

        state.Diplomacy.Set(
            falkenreich,
            nordmark,
            relations: 32,
            trust: 61,
            tension: 14);

        state.Diplomacy.Set(
            falkenreich,
            valeria,
            relations: -26,
            trust: 31,
            tension: 47);

        state.Diplomacy.Set(
            nordmark,
            valeria,
            relations: -12,
            trust: 42,
            tension: 34);

        return state;
    }

    private static void ConfigureFalkenreichPolitics(
        GameState state,
        Country country,
        PoliticalLineage lineage,
        Character king,
        Character treasurer,
        Character marshal,
        Character chancellor,
        Character marta,
        Character lukas,
        Character heir)
    {
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

        state.Relationships.Set(king, treasurer, opinion: 38, trust: 67, fear: 0);
        state.Relationships.Set(king, marshal, opinion: 18, trust: 48, fear: 4);
        state.Relationships.Set(king, chancellor, opinion: 57, trust: 78, fear: 0);
        state.Relationships.Set(king, marta, opinion: 41, trust: 58, fear: 0);
        state.Relationships.Set(king, lukas, opinion: -8, trust: 31, fear: 8);
        state.Relationships.Set(king, heir, opinion: 79, trust: 86, fear: 0);

        state.Relationships.Set(marshal, lukas, opinion: 45, trust: 70, fear: 0);
        state.Relationships.Set(lukas, marshal, opinion: 35, trust: 64, fear: 0);
        state.Relationships.Set(chancellor, lukas, opinion: -20, trust: 30, fear: 0);
        state.Relationships.Set(treasurer, lukas, opinion: -5, trust: 38, fear: 0);
        state.Relationships.Set(marta, lukas, opinion: 5, trust: 45, fear: 0);
        state.Relationships.Set(heir, lukas, opinion: -15, trust: 35, fear: 0);

        state.Relationships.Set(marta, chancellor, opinion: 52, trust: 72, fear: 0);
        state.Relationships.Set(chancellor, marta, opinion: 44, trust: 68, fear: 0);
    }

    private static void ConfigureForeignCourt(
        GameState state,
        Country country,
        Character ruler,
        IEnumerable<(Character Character, int CountryAllegiance, int Opinion, int Trust, int Fear)> courtiers)
    {
        var countryKey = PoliticalKeys.Country(country.Id);
        ruler.SetAllegiance(countryKey, 95);

        foreach (var entry in courtiers)
        {
            entry.Character.SetAllegiance(countryKey, entry.CountryAllegiance);
            state.Relationships.Set(
                entry.Character,
                ruler,
                entry.Opinion,
                entry.Trust,
                entry.Fear);
        }
    }
}
