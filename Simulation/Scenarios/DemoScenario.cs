using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;

namespace LeaderGame.Simulation.Scenarios;

public static class DemoScenario
{
    public static GameState Create(string scenarioId = ScenarioCatalog.FalkenreichId)
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
                Stability = 67,
                LegislativeBody = LegislativeBodyType.EstatesAssembly,
                LegislativeIndependence = 55
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
                Stability = 74,
                LegislativeBody = LegislativeBodyType.RoyalCouncil,
                LegislativeIndependence = 40
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
                Stability = 59,
                LegislativeBody = LegislativeBodyType.GreatCouncil,
                LegislativeIndependence = 75,
                ElectionIntervalMonths = 48,
                MonthsUntilElection = 12,
                ElectionCampaignMonths = 6
            },
            Ruler = valerianDoge
        };

        valeria.PoliticalFigures.AddRange(
            [valerianDoge, valerianChancellor, valerianTreasurer, valerianMarshal, valerianSuccessor]);
        valeria.SuccessionOrder.Add(valerianSuccessor);

        falkenreich.NeighborIds.UnionWith([nordmark.Id, valeria.Id]);
        nordmark.NeighborIds.UnionWith([falkenreich.Id, valeria.Id]);
        valeria.NeighborIds.UnionWith([falkenreich.Id, nordmark.Id]);

        var falkenLineage = new PoliticalLineage
        {
            Id = "house_von_falken",
            Name = "House von Falken",
            Type = PoliticalLineageType.Dynasty
        };
        falkenLineage.Members.AddRange([king, heir]);

        var valeriaLineage = new PoliticalLineage
        {
            Id = "vieri_coalition",
            Name = "Vieri Coalition",
            Type = PoliticalLineageType.Party
        };
        valeriaLineage.Members.AddRange([valerianDoge, valerianSuccessor]);

        var scenario = ScenarioCatalog.Get(scenarioId);

        var player = scenario.Id switch
        {
            ScenarioCatalog.FalkenreichId => new PlayerState
            {
                CurrentCharacter = king,
                Country = falkenreich,
                Lineage = falkenLineage
            },

            ScenarioCatalog.ValeriaId => new PlayerState
            {
                CurrentCharacter = valerianDoge,
                Country = valeria,
                Lineage = valeriaLineage
            },

            _ => throw new InvalidOperationException(
                $"Scenario '{scenario.Id}' has no player configuration.")
        };

        var state = new GameState
        {
            Date = new GameDate(1450, 1),
            Player = player
        };

        state.Campaign = ScenarioCatalog.CreateCampaign(
            scenario.Id,
            state.Date);

        state.Countries.AddRange([falkenreich, nordmark, valeria]);

        ConfigureAdministration(
            falkenreich,
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.Chancery,
                Name = "Royal Chancery",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 58,
                Reach = 47,
                Integrity = 63,
                Workload = 38,
                PatronageDependence = 54
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.Revenue,
                Name = "Royal Exchequer",
                ResponsiblePosition = Position.Treasurer,
                Capacity = 55,
                Reach = 42,
                Integrity = 55,
                Workload = 43,
                PatronageDependence = 61
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.LocalGovernment,
                Name = "Bailiffs and Seignorial Officers",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 43,
                Reach = 38,
                Integrity = 44,
                Workload = 48,
                PatronageDependence = 79
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.MilitaryLogistics,
                Name = "Marshal's Household",
                ResponsiblePosition = Position.Marshal,
                Capacity = 49,
                Reach = 36,
                Integrity = 58,
                Workload = 33,
                PatronageDependence = 69
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.ForeignAffairs,
                Name = "Chancery Correspondence Office",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 46,
                Reach = 34,
                Integrity = 64,
                Workload = 30,
                PatronageDependence = 48
            });

        ConfigureAdministration(
            nordmark,
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.Chancery,
                Name = "Crown Chancery",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 61,
                Reach = 50,
                Integrity = 66,
                Workload = 35,
                PatronageDependence = 49
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.Revenue,
                Name = "Crown Treasury",
                ResponsiblePosition = Position.Treasurer,
                Capacity = 58,
                Reach = 46,
                Integrity = 61,
                Workload = 39,
                PatronageDependence = 55
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.LocalGovernment,
                Name = "Royal Stewards and District Courts",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 50,
                Reach = 44,
                Integrity = 53,
                Workload = 42,
                PatronageDependence = 68
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.MilitaryLogistics,
                Name = "Marshal's Secretariat",
                ResponsiblePosition = Position.Marshal,
                Capacity = 54,
                Reach = 40,
                Integrity = 62,
                Workload = 31,
                PatronageDependence = 61
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.ForeignAffairs,
                Name = "Royal Correspondence Office",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 49,
                Reach = 38,
                Integrity = 67,
                Workload = 28,
                PatronageDependence = 43
            });

        ConfigureAdministration(
            valeria,
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.Chancery,
                Name = "State Chancery",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 80,
                Reach = 72,
                Integrity = 73,
                Workload = 44,
                PatronageDependence = 31
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.Revenue,
                Name = "Fiscal Chamber",
                ResponsiblePosition = Position.Treasurer,
                Capacity = 84,
                Reach = 78,
                Integrity = 69,
                Workload = 48,
                PatronageDependence = 29
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.LocalGovernment,
                Name = "Rectors and Civic Officers",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 72,
                Reach = 74,
                Integrity = 58,
                Workload = 51,
                PatronageDependence = 44
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.MilitaryLogistics,
                Name = "War and Arsenal Boards",
                ResponsiblePosition = Position.Marshal,
                Capacity = 77,
                Reach = 64,
                Integrity = 66,
                Workload = 40,
                PatronageDependence = 34
            },
            new AdministrativeOffice
            {
                Function = AdministrativeFunction.ForeignAffairs,
                Name = "Diplomatic Secretariat",
                ResponsiblePosition = Position.Chancellor,
                Capacity = 85,
                Reach = 83,
                Integrity = 74,
                Workload = 45,
                PatronageDependence = 24
            });

        ConfigurePowerBaseStrengths(falkenreich,
            (PowerBaseType.Aristocracy, 90),
            (PowerBaseType.Military, 75),
            (PowerBaseType.Merchants, 40),
            (PowerBaseType.Clergy, 70),
            (PowerBaseType.Bureaucracy, 55),
            (PowerBaseType.Workers, 10),
            (PowerBaseType.Peasantry, 45),
            (PowerBaseType.RegionalElites, 65),
            (PowerBaseType.Party, 5),
            (PowerBaseType.RoyalFamily, 90));

        ConfigurePowerBaseStrengths(nordmark,
            (PowerBaseType.Aristocracy, 80),
            (PowerBaseType.Military, 72),
            (PowerBaseType.Merchants, 48),
            (PowerBaseType.Clergy, 58),
            (PowerBaseType.Bureaucracy, 62),
            (PowerBaseType.Workers, 12),
            (PowerBaseType.Peasantry, 42),
            (PowerBaseType.RegionalElites, 58),
            (PowerBaseType.Party, 5),
            (PowerBaseType.RoyalFamily, 88));

        ConfigurePowerBaseStrengths(valeria,
            (PowerBaseType.Aristocracy, 25),
            (PowerBaseType.Military, 55),
            (PowerBaseType.Merchants, 92),
            (PowerBaseType.Clergy, 28),
            (PowerBaseType.Bureaucracy, 82),
            (PowerBaseType.Workers, 35),
            (PowerBaseType.Peasantry, 20),
            (PowerBaseType.RegionalElites, 60),
            (PowerBaseType.Party, 72),
            (PowerBaseType.RoyalFamily, 0));

        ConfigureCharacterPowerBases(king,
            (PowerBaseType.Aristocracy, 86),
            (PowerBaseType.Military, 68),
            (PowerBaseType.Clergy, 79),
            (PowerBaseType.Bureaucracy, 64),
            (PowerBaseType.RoyalFamily, 96));
        ConfigureCharacterPowerBases(treasurer,
            (PowerBaseType.Merchants, 82),
            (PowerBaseType.Bureaucracy, 84),
            (PowerBaseType.Aristocracy, 58));
        ConfigureCharacterPowerBases(marshal,
            (PowerBaseType.Military, 95),
            (PowerBaseType.Aristocracy, 61),
            (PowerBaseType.RegionalElites, 66));
        ConfigureCharacterPowerBases(chancellor,
            (PowerBaseType.Bureaucracy, 84),
            (PowerBaseType.Aristocracy, 70),
            (PowerBaseType.Clergy, 61));
        ConfigureCharacterPowerBases(marta,
            (PowerBaseType.Clergy, 69),
            (PowerBaseType.Peasantry, 64),
            (PowerBaseType.Bureaucracy, 57));
        ConfigureCharacterPowerBases(lukas,
            (PowerBaseType.Military, 82),
            (PowerBaseType.Aristocracy, 76),
            (PowerBaseType.RegionalElites, 83),
            (PowerBaseType.RoyalFamily, 26));
        ConfigureCharacterPowerBases(heir,
            (PowerBaseType.RoyalFamily, 94),
            (PowerBaseType.Aristocracy, 78),
            (PowerBaseType.Clergy, 68));

        ConfigureCharacterPowerBases(nordmarkQueen,
            (PowerBaseType.RoyalFamily, 95),
            (PowerBaseType.Aristocracy, 82),
            (PowerBaseType.Clergy, 67));
        ConfigureCharacterPowerBases(nordmarkMarshal,
            (PowerBaseType.Military, 93));
        ConfigureCharacterPowerBases(nordmarkChancellor,
            (PowerBaseType.Bureaucracy, 83));
        ConfigureCharacterPowerBases(nordmarkTreasurer,
            (PowerBaseType.Merchants, 77),
            (PowerBaseType.Bureaucracy, 86));
        ConfigureCharacterPowerBases(nordmarkHeir,
            (PowerBaseType.RoyalFamily, 91),
            (PowerBaseType.Aristocracy, 74));

        ConfigureCharacterPowerBases(valerianDoge,
            (PowerBaseType.Merchants, 91),
            (PowerBaseType.Party, 78),
            (PowerBaseType.Bureaucracy, 69));
        ConfigureCharacterPowerBases(valerianChancellor,
            (PowerBaseType.Bureaucracy, 91),
            (PowerBaseType.Party, 73));
        ConfigureCharacterPowerBases(valerianTreasurer,
            (PowerBaseType.Merchants, 94),
            (PowerBaseType.Bureaucracy, 88));
        ConfigureCharacterPowerBases(valerianMarshal,
            (PowerBaseType.Military, 92),
            (PowerBaseType.Party, 58));
        ConfigureCharacterPowerBases(valerianSuccessor,
            (PowerBaseType.Merchants, 83),
            (PowerBaseType.Party, 84),
            (PowerBaseType.RegionalElites, 67));

        ConfigureFalkenreichPolitics(
            state,
            falkenreich,
            falkenLineage,
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

        ConfigureValeriaLineage(
            valeria,
            valeriaLineage,
            valerianDoge,
            valerianChancellor,
            valerianTreasurer,
            valerianMarshal,
            valerianSuccessor);

        // Personal relationships between rulers are distinct from state relations.
        // Succession therefore changes diplomatic chemistry without erasing treaties
        // or the accumulated history between states.
        state.Relationships.Set(
            nordmarkQueen,
            king,
            opinion: 44,
            trust: 68,
            fear: 0);
        state.Relationships.Set(
            king,
            nordmarkQueen,
            opinion: 36,
            trust: 61,
            fear: 0);

        state.Relationships.Set(
            valerianDoge,
            king,
            opinion: -34,
            trust: 26,
            fear: 4);
        state.Relationships.Set(
            king,
            valerianDoge,
            opinion: -21,
            trust: 33,
            fear: 2);

        state.Relationships.Set(
            nordmarkQueen,
            valerianDoge,
            opinion: -12,
            trust: 39,
            fear: 1);
        state.Relationships.Set(
            valerianDoge,
            nordmarkQueen,
            opinion: -18,
            trust: 35,
            fear: 2);

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

        InformationSystem.SeedInitialBriefings(state);

        return state;
    }

    private static void ConfigureAdministration(
        Country country,
        params AdministrativeOffice[] offices)
    {
        country.AdministrativeOffices.AddRange(offices);
    }

    private static void ConfigurePowerBaseStrengths(
        Country country,
        params (PowerBaseType Type, int Strength)[] values)
    {
        foreach (var (type, strength) in values)
            country.SetPowerBaseStrength(type, strength);
    }

    private static void ConfigureCharacterPowerBases(
        Character character,
        params (PowerBaseType Type, int Standing)[] values)
    {
        foreach (var (type, standing) in values)
            character.SetPowerBaseStanding(type, standing);
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

    private static void ConfigureValeriaLineage(
        Country country,
        PoliticalLineage lineage,
        Character doge,
        Character chancellor,
        Character treasurer,
        Character marshal,
        Character successor)
    {
        var countryKey = PoliticalKeys.Country(country.Id);
        var lineageKey = PoliticalKeys.Lineage(lineage.Id);

        doge.SetAllegiance(countryKey, 95);
        doge.SetAllegiance(lineageKey, 100);
        chancellor.SetAllegiance(lineageKey, 72);
        treasurer.SetAllegiance(lineageKey, 78);
        marshal.SetAllegiance(lineageKey, 55);
        successor.SetAllegiance(lineageKey, 88);
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
