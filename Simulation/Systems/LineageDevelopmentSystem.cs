using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

/// <summary>
/// Keeps a centuries-long political simulation generationally alive without
/// turning it into a birth-and-family-tree simulator. New people enter the
/// political layer when they become relevant: cadet relatives come of age,
/// parties promote new organisers, and states replenish their political class.
/// </summary>
internal static class LineageDevelopmentSystem
{
    private static readonly string[] FalkenFirstNames =
    [
        "Albrecht", "Matthias", "Wilhelm", "Elisabeth",
        "Clara", "Margarethe", "Ulrich", "Gerhard",
        "Johanna", "Leopold"
    ];

    private static readonly string[] ValerianFirstNames =
    [
        "Lorenzo", "Bianca", "Matteo", "Sofia",
        "Caterina", "Niccolo", "Giulia", "Andrea",
        "Francesca", "Tommaso"
    ];

    private static readonly string[] NordmarkFirstNames =
    [
        "Harald", "Sigrid", "Leif", "Karin",
        "Ragnhild", "Eirik", "Solveig", "Magnus",
        "Ingeborg", "Torsten"
    ];

    private static readonly string[] GenericFirstNames =
    [
        "Adrian", "Helena", "Martin", "Anna",
        "Julian", "Elise", "Tomas", "Mara"
    ];

    private static readonly string[] FalkenSurnames =
    [
        "Adler", "Weiss", "Reinhardt", "Brandt",
        "Eberhardt", "Kraus"
    ];

    private static readonly string[] ValerianSurnames =
    [
        "Bellini", "Doria", "Ferrante", "Grimaldi",
        "Orsini", "Moretti"
    ];

    private static readonly string[] NordmarkSurnames =
    [
        "Dahl", "Bjornsen", "Eide", "Nygaard",
        "Solberg", "Vik"
    ];

    public static IEnumerable<SimulationReport> ProcessMonth(
        GameState state)
    {
        var reports =
            new List<SimulationReport>();

        EnsurePlayerContinuity(
            state,
            reports);

        if (state.Date.Month != 1 ||
            state.Date.Year == 1450)
        {
            return reports;
        }

        foreach (var country in state.Countries)
        {
            MaintainSuccessionPool(
                state,
                country,
                reports);

            MaintainPoliticalClass(
                state,
                country);
        }

        MaintainPlayerLineage(
            state,
            reports);

        return reports;
    }

    private static void EnsurePlayerContinuity(
        GameState state,
        List<SimulationReport> reports)
    {
        var player =
            state.Player;

        if (player.Lineage.Members.Any(member =>
                member.IsAlive))
        {
            return;
        }

        var newcomer =
            player.Lineage.Type switch
            {
                PoliticalLineageType.Dynasty =>
                    CreateDynasticRelative(
                        state,
                        player.Country,
                        legitimacyFloor: 35,
                        surname: PlayerDynastySurname(player)),

                PoliticalLineageType.Party or
                PoliticalLineageType.Faction =>
                    CreatePartyFigure(
                        state,
                        player.Country),

                _ => null
            };

        if (newcomer is null)
            return;

        AddPoliticalFigure(
            player.Country,
            newcomer);
        player.Lineage.Members.Add(
            newcomer);

        if (player.Lineage.Type ==
            PoliticalLineageType.Dynasty)
        {
            player.Country.SuccessionOrder.Add(
                newcomer);

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Personal,
                $"A cadet branch of {player.Lineage.Name} emerges",
                $"{newcomer.FullName}, a previously peripheral relative of the ruling house, becomes politically relevant as the senior branch disappears. The lineage survives, but the new claimant begins with limited legitimacy and must rebuild its position."));
        }
        else
        {
            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{player.Lineage.Name} finds new leadership",
                $"{newcomer.FullName} emerges from the movement's wider organisation as its old leadership disappears. The political lineage survives, but continuity of name does not guarantee continuity of influence."));
        }
    }

    private static void MaintainPlayerLineage(
        GameState state,
        List<SimulationReport> reports)
    {
        var player =
            state.Player;
        var active =
            player.Lineage.Members
                .Count(member =>
                    member.IsPoliticallyActive);

        if (player.Lineage.Type ==
            PoliticalLineageType.Dynasty)
        {
            if (active >= 4)
                return;

            // A politically dead but biologically surviving house should not
            // manufacture new active claimants and escape irrelevance for free.
            // Emergency cadet continuity is reserved for actual extinction.
            if (!player.IsInPower &&
                active == 0)
            {
                return;
            }

            var chance =
                active switch
                {
                    <= 1 => 0.85,
                    2 => 0.55,
                    _ => 0.28
                };

            if (state.Random.NextDouble() >
                chance)
            {
                return;
            }

            var relative =
                CreateDynasticRelative(
                    state,
                    player.Country,
                    legitimacyFloor: 50,
                    surname: PlayerDynastySurname(player));

            AddPoliticalFigure(
                player.Country,
                relative);

            player.Lineage.Members.Add(
                relative);

            if (!player.Country.SuccessionOrder.Contains(
                    relative))
            {
                player.Country.SuccessionOrder.Add(
                    relative);
            }

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Personal,
                $"{relative.FullName} comes of age within {player.Lineage.Name}",
                $"{relative.FullName}, age {relative.Age}, enters national politics from a cadet branch of the dynasty. The house gains another possible future leader, but the newcomer has ambitions and political relationships of their own."));
            return;
        }

        if (player.Lineage.Type is not
            (PoliticalLineageType.Party or
             PoliticalLineageType.Faction))
        {
            return;
        }

        if (active >= 5)
            return;

        // An entirely inactive movement does not receive free annual recruits
        // while drifting into irrelevance. If every member actually dies,
        // EnsurePlayerContinuity can still surface a successor organisation.
        if (!player.IsInPower &&
            active == 0)
        {
            return;
        }

        var recruitmentChance =
            active switch
            {
                <= 1 => 0.90,
                2 => 0.70,
                3 => 0.45,
                _ => 0.25
            };

        if (state.Random.NextDouble() >
            recruitmentChance)
        {
            return;
        }

        var recruit =
            CreatePartyFigure(
                state,
                player.Country);

        AddPoliticalFigure(
            player.Country,
            recruit);
        player.Lineage.Members.Add(
            recruit);

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{recruit.FullName} rises within {player.Lineage.Name}",
            $"{recruit.FullName}, age {recruit.Age}, becomes a nationally relevant figure inside the political lineage. A party can outlive its founders, but each new generation changes the coalition that carries its name."));
    }

    private static void MaintainSuccessionPool(
        GameState state,
        Country country,
        List<SimulationReport> reports)
    {
        if (country.Government.Type ==
            GovernmentType.Republic)
        {
            return;
        }

        var candidates =
            country.SuccessionOrder
                .Where(character =>
                    character.IsPoliticallyActive &&
                    !ReferenceEquals(
                        character,
                        country.Ruler))
                .DistinctBy(character =>
                    character.Id)
                .Count();

        if (candidates >= 2)
            return;

        var chance =
            candidates == 0
                ? 0.90
                : 0.55;

        if (state.Random.NextDouble() >
            chance)
        {
            return;
        }

        var relative =
            CreateDynasticRelative(
                state,
                country,
                legitimacyFloor:
                    candidates == 0
                        ? 45
                        : 38);

        AddPoliticalFigure(
            country,
            relative);
        country.SuccessionOrder.Add(
            relative);

        if (ReferenceEquals(
                country,
                state.Player.Country) &&
            state.Player.IsInPower &&
            state.Player.Lineage.Type ==
                PoliticalLineageType.Dynasty &&
            !state.Player.Lineage.Contains(
                relative))
        {
            state.Player.Lineage.Members.Add(
                relative);

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Personal,
                $"{relative.FullName} enters the succession",
                $"A younger cadet relative becomes a recognised part of the succession. This makes dynastic extinction less immediate, while also adding another person around whom future factions could organise."));
        }
    }

    private static void MaintainPoliticalClass(
        GameState state,
        Country country)
    {
        var active =
            country.PoliticalFigures.Count(character =>
                character.IsPoliticallyActive);

        var younger =
            country.PoliticalFigures.Count(character =>
                character.IsPoliticallyActive &&
                character.Age < 55);

        if (active >= 8 &&
            younger >= 4)
        {
            return;
        }

        var chance =
            active < 5
                ? 0.75
                : younger < 3
                    ? 0.45
                    : 0.22;

        if (state.Random.NextDouble() >
            chance)
        {
            return;
        }

        var newcomer =
            CreateGeneralFigure(
                state,
                country);

        AddPoliticalFigure(
            country,
            newcomer);
    }

    private static Character CreateDynasticRelative(
        GameState state,
        Country country,
        int legitimacyFloor,
        string? surname = null)
    {
        surname ??=
            country.Ruler.LastName;

        var character = new Character
        {
            Id = NextCharacterId(state),
            FirstName = PickFirstName(
                state.Random,
                country),
            LastName = surname,
            Age = NextInt(
                state.Random,
                17,
                27),
            Health = 100,
            Competence = NextInt(
                state.Random,
                45,
                81),
            Ambition = NextInt(
                state.Random,
                35,
                86),
            Legitimacy = NextInt(
                state.Random,
                legitimacyFloor,
                Math.Max(
                    legitimacyFloor + 1,
                    79)),
            Influence = NextInt(
                state.Random,
                18,
                43)
        };

        character.SetAllegiance(
            PoliticalKeys.Country(
                country.Id),
            NextInt(
                state.Random,
                70,
                91));

        character.SetPowerBaseStanding(
            PowerBaseType.RoyalFamily,
            NextInt(
                state.Random,
                80,
                96));
        character.SetPowerBaseStanding(
            PowerBaseType.Aristocracy,
            NextInt(
                state.Random,
                55,
                81));
        character.SetPowerBaseStanding(
            PowerBaseType.Clergy,
            NextInt(
                state.Random,
                45,
                76));

        return character;
    }

    private static Character CreatePartyFigure(
        GameState state,
        Country country)
    {
        var character =
            CreateGeneralFigure(
                state,
                country);

        character.SetPowerBaseStanding(
            PowerBaseType.Party,
            NextInt(
                state.Random,
                70,
                93));
        character.SetPowerBaseStanding(
            PowerBaseType.Merchants,
            NextInt(
                state.Random,
                45,
                86));
        character.SetPowerBaseStanding(
            PowerBaseType.Bureaucracy,
            NextInt(
                state.Random,
                45,
                86));
        character.Influence =
            Math.Max(
                character.Influence,
                NextInt(
                    state.Random,
                    35,
                    61));

        return character;
    }

    private static Character CreateGeneralFigure(
        GameState state,
        Country country)
    {
        var character = new Character
        {
            Id = NextCharacterId(state),
            FirstName = PickFirstName(
                state.Random,
                country),
            LastName = PickSurname(
                state.Random,
                country),
            Age = NextInt(
                state.Random,
                27,
                52),
            Health = 100,
            Competence = NextInt(
                state.Random,
                42,
                88),
            Ambition = NextInt(
                state.Random,
                28,
                90),
            Legitimacy = NextInt(
                state.Random,
                25,
                66),
            Influence = NextInt(
                state.Random,
                22,
                58)
        };

        character.SetAllegiance(
            PoliticalKeys.Country(
                country.Id),
            NextInt(
                state.Random,
                55,
                86));

        var primary =
            Enum.GetValues<PowerBaseType>()
                .Where(type =>
                    country.GetPowerBaseStrength(type) >
                    15)
                .OrderByDescending(type =>
                    country.GetPowerBaseStrength(type) *
                    (0.8 +
                     state.Random.NextDouble() *
                     0.4))
                .FirstOrDefault();

        character.SetPowerBaseStanding(
            primary,
            NextInt(
                state.Random,
                65,
                91));

        return character;
    }

    private static void AddPoliticalFigure(
        Country country,
        Character character)
    {
        if (!country.PoliticalFigures.Contains(
                character))
        {
            country.PoliticalFigures.Add(
                character);
        }
    }

    private static string PlayerDynastySurname(
        PlayerState player) =>
        player.Lineage.Members
            .Select(member => member.LastName)
            .FirstOrDefault(name =>
                !string.IsNullOrWhiteSpace(name)) ??
        player.CurrentCharacter.LastName;

    private static int NextCharacterId(
        GameState state) =>
        state.Countries
            .SelectMany(country =>
                country.PoliticalFigures)
            .Concat(
                state.Player.Lineage.Members)
            .Select(character =>
                character.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;

    private static string PickFirstName(
        IRandomSource random,
        Country country)
    {
        var names = country.Id switch
        {
            "falkenreich" => FalkenFirstNames,
            "valeria" => ValerianFirstNames,
            "nordmark" => NordmarkFirstNames,
            _ => GenericFirstNames
        };

        return names[
            NextInt(
                random,
                0,
                names.Length)];
    }

    private static string PickSurname(
        IRandomSource random,
        Country country)
    {
        var names = country.Id switch
        {
            "falkenreich" => FalkenSurnames,
            "valeria" => ValerianSurnames,
            "nordmark" => NordmarkSurnames,
            _ => FalkenSurnames
        };

        return names[
            NextInt(
                random,
                0,
                names.Length)];
    }

    private static int NextInt(
        IRandomSource random,
        int minimum,
        int maximumExclusive)
    {
        if (maximumExclusive <= minimum)
            return minimum;

        return
            minimum +
            (int)Math.Floor(
                random.NextDouble() *
                (maximumExclusive - minimum));
    }
}
