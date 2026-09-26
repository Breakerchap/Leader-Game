using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class SuccessionSystem
{
    public static IEnumerable<SimulationReport> Process(GameState state)
    {
        foreach (var country in state.Countries)
        {
            if (country.Ruler.IsAlive)
                continue;

            var deadRuler = country.Ruler;
            var successor = FindSuccessor(country, deadRuler);

            if (successor is null)
            {
                country.Government.Stability -= 8;

                yield return new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"The throne of {country.Name} is vacant",
                    $"{deadRuler.FullName} has died, and no politically active figure can yet " +
                    "establish a recognised succession. The resulting vacuum sharply damages stability.");

                continue;
            }

            successor.Position = null;
            country.Ruler = successor;

            var isRepublicanInterim =
                country.Government.HoldsScheduledElections;

            if (isRepublicanInterim)
            {
                country.Government.MonthsUntilElection = Math.Min(
                    country.Government.MonthsUntilElection > 0
                        ? country.Government.MonthsUntilElection
                        : 2,
                    2);
            }

            var isPlayerCountry = ReferenceEquals(
                country,
                state.Player.Country);
            var successorContinuesPlayerLineage =
                isPlayerCountry &&
                state.Player.Lineage.Contains(successor);

            if (successorContinuesPlayerLineage)
            {
                state.Player.CurrentCharacter = successor;

                yield return new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    isRepublicanInterim
                        ? $"{successor.FullName} becomes interim ruler of {country.Name}"
                        : $"{successor.FullName} succeeds {deadRuler.FullName}",
                    isRepublicanInterim
                        ? $"{successor.FullName} assumes office after {deadRuler.FullName}'s death. " +
                          $"The {state.Player.Lineage.Name} remains in control for now, but a " +
                          "constitutional election will be held within two months."
                        : $"{successor.FullName} becomes ruler of {country.Name}. " +
                          $"The {state.Player.Lineage.Name} remains in power, so play continues " +
                          $"as {successor.FullName}.");
            }
            else
            {
                yield return new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    isRepublicanInterim
                        ? $"{successor.FullName} becomes interim ruler of {country.Name}"
                        : $"{successor.FullName} succeeds {deadRuler.FullName}",
                    isPlayerCountry
                        ? $"{successor.FullName} takes control of {country.Name}, forcing " +
                          $"{state.Player.Lineage.Name} out of government. The lineage survives, " +
                          "so the campaign continues from opposition unless its political position later collapses."
                        : isRepublicanInterim
                            ? $"{successor.FullName} assumes interim office after {deadRuler.FullName}'s death. " +
                              "A constitutional election will follow shortly."
                            : $"{successor.FullName} becomes ruler of {country.Name}.");
            }
        }
    }

    private static Character? FindSuccessor(
        Country country,
        Character deadRuler)
    {
        var designated = country.SuccessionOrder.FirstOrDefault(candidate =>
            candidate.IsPoliticallyActive &&
            !ReferenceEquals(candidate, deadRuler));

        if (designated is not null)
            return designated;

        // A depleted formal succession list should create a legitimacy crisis,
        // not leave the simulation permanently attached to a dead ruler.
        return country.PoliticalFigures
            .Where(candidate =>
                candidate.IsPoliticallyActive &&
                !ReferenceEquals(candidate, deadRuler))
            .OrderByDescending(candidate =>
                candidate.Legitimacy * 0.45 +
                candidate.Influence * 0.35 +
                candidate.Competence * 0.20)
            .FirstOrDefault();
    }
}
