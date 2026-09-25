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
                if (ReferenceEquals(country, state.Player.Country) &&
                    state.Player.Lineage.Contains(deadRuler))
                {
                    state.Player.HasLost = true;
                    state.Player.LossReason =
                        $"{deadRuler.FullName} died and no living successor could take power.";
                }

                yield return new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"The throne of {country.Name} is vacant",
                    $"{deadRuler.FullName} has died, but no living successor is available.");

                continue;
            }

            // An office-holder who becomes ruler vacates their former advisory office.
            successor.Position = null;
            country.Ruler = successor;

            var isPlayerCountry = ReferenceEquals(country, state.Player.Country);
            var successorContinuesPlayerLineage =
                isPlayerCountry && state.Player.Lineage.Contains(successor);

            if (successorContinuesPlayerLineage)
            {
                state.Player.CurrentCharacter = successor;

                yield return new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{successor.FullName} succeeds {deadRuler.FullName}",
                    $"{successor.FullName} becomes ruler of {country.Name}. " +
                    $"The {state.Player.Lineage.Name} remains in power, so play continues " +
                    $"as {successor.FullName}.");
            }
            else
            {
                if (isPlayerCountry)
                {
                    state.Player.HasLost = true;
                    state.Player.LossReason =
                        $"{successor.FullName} succeeded {deadRuler.FullName}, ending " +
                        $"{state.Player.Lineage.Name}'s control of {country.Name}.";
                }

                yield return new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{successor.FullName} succeeds {deadRuler.FullName}",
                    isPlayerCountry
                        ? $"{successor.FullName} becomes ruler of {country.Name}. " +
                          $"{state.Player.Lineage.Name} has lost power."
                        : $"{successor.FullName} becomes ruler of {country.Name}.");
            }
        }
    }

    private static Character? FindSuccessor(Country country, Character deadRuler)
    {
        return country.SuccessionOrder.FirstOrDefault(candidate =>
            candidate.IsAlive &&
            !ReferenceEquals(candidate, deadRuler));
    }
}
