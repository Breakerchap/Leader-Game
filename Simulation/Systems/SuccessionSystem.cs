using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Politics;
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

            ResolveSuccessionCrisisThreads(
                state,
                country);

            var accessionReport =
                ApplyAccessionPolitics(
                    state,
                    country,
                    deadRuler,
                    successor);

            if (accessionReport is not null)
                yield return accessionReport;

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

    private static void ResolveSuccessionCrisisThreads(
        GameState state,
        Country country)
    {
        foreach (var crisis in state.PoliticalCrises.Where(crisis =>
                     crisis.Status ==
                         PoliticalCrisisStatus.Active &&
                     crisis.Type ==
                         PoliticalCrisisType.SuccessionDispute &&
                     ReferenceEquals(
                         crisis.Country,
                         country)))
        {
            crisis.Status =
                PoliticalCrisisStatus.Resolved;
            crisis.ResolvedOn = state.Date;
            crisis.AwaitingDecision = false;
        }
    }

    private static SimulationReport? ApplyAccessionPolitics(
        GameState state,
        Country country,
        Character deadRuler,
        Character successor)
    {
        if (country.Government.HoldsScheduledElections)
            return null;

        var stabilityChange =
            successor.Legitimacy switch
            {
                >= 80 => 3.0,
                >= 68 => 1.0,
                >= 55 => -1.5,
                >= 40 => -3.5,
                _ => -6.0
            };

        country.Government.Stability +=
            stabilityChange;

        var rivalBloc = state.PoliticalBlocs
            .Where(bloc =>
                bloc.IsActive &&
                ReferenceEquals(
                    bloc.Country,
                    country) &&
                !ReferenceEquals(
                    bloc.Leader,
                    successor) &&
                bloc.Leader.IsPoliticallyActive &&
                country.SuccessionOrder.Contains(
                    bloc.Leader))
            .OrderByDescending(bloc =>
                bloc.Cohesion)
            .FirstOrDefault();

        if (rivalBloc is null ||
            rivalBloc.Cohesion < 50)
        {
            if (!ReferenceEquals(
                    country,
                    state.Player.Country))
            {
                return null;
            }

            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{successor.FullName}'s accession begins",
                stabilityChange >= 0
                    ? $"{successor.FullName} inherits with enough recognised legitimacy that the first transfer of authority is comparatively orderly."
                    : $"{successor.FullName} inherits the throne, but the new ruler's limited legitimacy immediately makes the regime less secure.");
        }

        var challenge =
            Math.Clamp(
                rivalBloc.Cohesion / 8.0 +
                rivalBloc.Leader.Influence / 12.0 -
                successor.Legitimacy / 15.0,
                3.0,
                14.0);

        country.Government.Stability -=
            challenge;
        successor.Legitimacy -=
            (int)Math.Round(
                challenge / 3.0);
        rivalBloc.Cohesion =
            Math.Min(
                100,
                rivalBloc.Cohesion + 5);

        if (ReferenceEquals(
                country,
                state.Player.Country) &&
            state.Player.Lineage.Contains(
                successor) &&
            !state.PoliticalCrises.Any(crisis =>
                crisis.Status ==
                    PoliticalCrisisStatus.Active &&
                crisis.Type ==
                    PoliticalCrisisType.PoliticalStandoff &&
                crisis.RelatedBlocId ==
                    rivalBloc.Id))
        {
            state.PoliticalCrises.Add(
                new PoliticalCrisis
                {
                    Country = country,
                    Type =
                        PoliticalCrisisType.PoliticalStandoff,
                    RelatedBlocId = rivalBloc.Id,
                    StartedOn = state.Date,
                    Stage = rivalBloc.Cohesion >= 75
                        ? 2
                        : 1,
                    AwaitingDecision = true
                });
        }

        if (!ReferenceEquals(
                country,
                state.Player.Country))
        {
            return null;
        }

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{successor.FullName}'s accession is contested",
            $"{successor.FullName} succeeds {deadRuler.FullName}, but {rivalBloc.Leader.FullName}'s succession faction refuses to disappear. " +
            "The transfer itself succeeds; the danger is what happens next, as the new ruler must either divide, compromise with or defeat the rival coalition.");
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
