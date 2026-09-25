using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class LifeSystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var processed = new HashSet<Character>();

        foreach (var country in state.Countries)
        {
            foreach (var character in country.PoliticalFigures)
            {
                if (!processed.Add(character) || !character.IsAlive)
                    continue;

                if (state.Date.Month == 12)
                    character.Age++;

                var deathRisk = CalculateMonthlyDeathRisk(character);

                if (state.Random.NextDouble() < deathRisk)
                {
                    Kill(character);
                    yield return DeathReport(state, character, country.Name);
                    continue;
                }

                if (character.Health >= 90)
                {
                    var illnessRisk = CalculateMonthlyIllnessRisk(character.Age);

                    if (state.Random.NextDouble() < illnessRisk)
                    {
                        var severity = 8 + (int)Math.Floor(state.Random.NextDouble() * 18);
                        character.Health -= severity;

                        yield return new SimulationReport(
                            state.Date,
                            ReportCategory.Personal,
                            $"{character.FullName} falls ill",
                            $"{character.FullName}, age {character.Age}, falls ill. " +
                            $"Health falls to {character.Health}/100.");
                    }

                    continue;
                }

                var oldHealth = character.Health;
                var recoveryChance = Math.Clamp(
                    0.50 - Math.Max(0, character.Age - 40) * 0.005,
                    0.20,
                    0.50);

                if (state.Random.NextDouble() < recoveryChance)
                {
                    character.Health +=
                        4 + (int)Math.Floor(state.Random.NextDouble() * 9);

                    if (oldHealth < 90 && character.Health >= 90)
                    {
                        yield return new SimulationReport(
                            state.Date,
                            ReportCategory.Personal,
                            $"{character.FullName} recovers",
                            $"{character.FullName} recovers from illness. Health is now " +
                            $"{character.Health}/100.");
                    }
                }
                else
                {
                    character.Health -=
                        2 + (int)Math.Floor(state.Random.NextDouble() * 7);

                    if (character.Health <= 0)
                    {
                        Kill(character);
                        yield return DeathReport(state, character, country.Name);
                    }
                }
            }
        }
    }

    private static double CalculateMonthlyIllnessRisk(int age)
    {
        return age switch
        {
            < 40 => 0.001,
            < 55 => 0.002,
            < 70 => 0.005,
            < 80 => 0.012,
            _ => 0.030
        };
    }

    private static double CalculateMonthlyDeathRisk(Character character)
    {
        var ageRisk = character.Age switch
        {
            < 50 => 0.0002,
            < 65 => 0.0008,
            < 75 => 0.0030,
            < 85 => 0.0100,
            _ => 0.0300
        };

        if (character.Health >= 50)
            return ageRisk;

        var healthMultiplier =
            1.0 + (50 - character.Health) / 10.0;

        return Math.Clamp(ageRisk * healthMultiplier, 0, 0.75);
    }

    private static void Kill(Character character)
    {
        character.IsAlive = false;
        character.Health = 0;
        character.Position = null;
    }

    private static SimulationReport DeathReport(
        GameState state,
        Character character,
        string countryName)
    {
        return new SimulationReport(
            state.Date,
            ReportCategory.Personal,
            $"{character.FullName} dies",
            $"{character.FullName} dies at age {character.Age} in {countryName}.");
    }
}
