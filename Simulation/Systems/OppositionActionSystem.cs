using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

/// <summary>
/// Gives organised opposition a political life between ordinary grievances and
/// a coup attempt. Actions are deliberately periodic and explainable rather
/// than random event spam.
/// </summary>
internal static class OppositionActionSystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var bloc in state.PoliticalBlocs.Where(bloc =>
                     bloc.IsActive &&
                     bloc.Leader.IsPoliticallyActive &&
                     bloc.MonthsActive > 0 &&
                     bloc.MonthsActive % 3 == 0))
        {
            var insiders = GetGovernmentInsiders(bloc);

            SimulationReport? report;

            if (insiders.Count > 0)
            {
                report = ApplyInstitutionalObstruction(
                    state,
                    bloc,
                    insiders);
            }
            else if (bloc.Cohesion >= 65)
            {
                report = ApplyCoordinatedPressure(
                    state,
                    bloc);
            }
            else
            {
                report = ApplyRecruitmentDrive(
                    state,
                    bloc);
            }

            if (report is not null &&
                ReferenceEquals(
                    bloc.Country,
                    state.Player.Country))
            {
                reports.Add(report);
            }
        }

        return reports;
    }

    private static List<Character> GetGovernmentInsiders(
        PoliticalBloc bloc)
    {
        return bloc.Country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                character.Position.HasValue &&
                (ReferenceEquals(character, bloc.Leader) ||
                 bloc.MemberIds.Contains(character.Id)))
            .ToList();
    }

    private static SimulationReport ApplyInstitutionalObstruction(
        GameState state,
        PoliticalBloc bloc,
        IReadOnlyList<Character> insiders)
    {
        var country = bloc.Country;

        var administrativeDamage = Math.Min(
            0.012m,
            0.004m + insiders.Count * 0.002m);

        country.AdministrativeEfficiency -=
            administrativeDamage;
        country.Government.Stability -=
            0.35 + bloc.Cohesion / 200.0;

        foreach (var powerBase in bloc.PowerBases)
        {
            country.Ruler.ChangePowerBaseStanding(
                powerBase,
                -1);
        }

        if (insiders.Any(character =>
                character.Position == Position.Marshal))
        {
            country.ArmyReadiness -= 1.0;
        }

        bloc.Leader.Influence = Math.Min(
            100,
            bloc.Leader.Influence + 1);

        foreach (var insider in insiders)
        {
            var towardLeader = state.Relationships.GetOrCreate(
                insider,
                bloc.Leader);
            var towardRuler = state.Relationships.GetOrCreate(
                insider,
                country.Ruler);

            if (!ReferenceEquals(insider, bloc.Leader))
            {
                towardLeader.ChangeTrust(1);
                towardLeader.ChangeOpinion(1);
            }

            towardRuler.ChangeTrust(-1);
        }

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"Opposition obstructs {country.Name}'s government",
            $"{bloc.Leader.FullName}'s bloc is using allies inside government to " +
            "delay, leak and frustrate administration. " +
            $"Visible insiders include {FormatNames(insiders)}. " +
            "State capacity and confidence in the government are being eroded.");
    }

    private static SimulationReport ApplyCoordinatedPressure(
        GameState state,
        PoliticalBloc bloc)
    {
        var country = bloc.Country;

        foreach (var powerBase in bloc.PowerBases)
        {
            country.Ruler.ChangePowerBaseStanding(
                powerBase,
                -2);
            bloc.Leader.ChangePowerBaseStanding(
                powerBase,
                2);
        }

        var averageStrength = bloc.PowerBases.Count == 0
            ? 50.0
            : bloc.PowerBases.Average(
                powerBase => country.GetPowerBaseStrength(powerBase));

        country.PublicUnrest += Math.Clamp(
            averageStrength / 90.0,
            0.4,
            1.4);
        country.Government.Stability -= 0.6;

        bloc.Leader.Influence = Math.Min(
            100,
            bloc.Leader.Influence + 1);

        foreach (var memberId in bloc.MemberIds)
        {
            var member = country.PoliticalFigures.FirstOrDefault(
                character => character.Id == memberId);

            if (member is null ||
                !member.IsPoliticallyActive)
            {
                continue;
            }

            state.Relationships
                .GetOrCreate(member, bloc.Leader)
                .ChangeTrust(1);
        }

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{bloc.Leader.FullName} coordinates opposition pressure",
            $"The opposition bloc publicly aligns its supporters across " +
            $"{FormatPowerBases(bloc.PowerBases)}. The campaign strengthens " +
            $"{bloc.Leader.FullName}'s position while weakening the ruler's standing " +
            "with those constituencies and increasing wider political tension.");
    }

    private static SimulationReport ApplyRecruitmentDrive(
        GameState state,
        PoliticalBloc bloc)
    {
        var country = bloc.Country;

        var recruit = country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, country.Ruler) &&
                !ReferenceEquals(character, bloc.Leader) &&
                !bloc.MemberIds.Contains(character.Id))
            .OrderByDescending(character =>
            {
                var towardLeader = state.Relationships.GetOrCreate(
                    character,
                    bloc.Leader);

                var backing = bloc.PowerBases.Count == 0
                    ? 50.0
                    : bloc.PowerBases.Average(
                        powerBase => character.GetPowerBaseStanding(powerBase));

                return towardLeader.Trust * 0.35 +
                       (towardLeader.Opinion + 100) / 2.0 * 0.20 +
                       backing * 0.25 +
                       character.Ambition * 0.20;
            })
            .FirstOrDefault();

        bloc.Leader.Influence = Math.Min(
            100,
            bloc.Leader.Influence + 1);

        foreach (var powerBase in bloc.PowerBases)
        {
            bloc.Leader.ChangePowerBaseStanding(
                powerBase,
                1);
        }

        if (recruit is null)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{bloc.Leader.FullName}'s bloc consolidates",
                $"The opposition spends the quarter strengthening its internal network " +
                $"among {FormatPowerBases(bloc.PowerBases)} rather than confronting the ruler openly.");
        }

        var towardBlocLeader = state.Relationships.GetOrCreate(
            recruit,
            bloc.Leader);
        var towardRuler = state.Relationships.GetOrCreate(
            recruit,
            country.Ruler);

        towardBlocLeader.ChangeOpinion(4);
        towardBlocLeader.ChangeTrust(3);
        towardRuler.ChangeTrust(-1);

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{bloc.Leader.FullName} courts {recruit.FullName}",
            $"{bloc.Leader.FullName}'s opposition bloc is quietly drawing " +
            $"{recruit.FullName} closer through private meetings and shared grievances. " +
            "The figure has not necessarily joined the bloc yet, but their political alignment is shifting.");
    }

    private static string FormatNames(
        IReadOnlyList<Character> characters)
    {
        return characters.Count switch
        {
            0 => "no publicly identifiable office-holders",
            1 => characters[0].FullName,
            2 => $"{characters[0].FullName} and {characters[1].FullName}",
            _ => string.Join(
                     ", ",
                     characters
                         .Take(characters.Count - 1)
                         .Select(character => character.FullName)) +
                 $" and {characters[^1].FullName}"
        };
    }

    private static string FormatPowerBases(
        IEnumerable<PowerBaseType> powerBases)
    {
        var names = powerBases
            .Select(DomesticPoliticsSystem.FormatPowerBase)
            .OrderBy(name => name)
            .ToList();

        if (names.Count == 0)
            return "its political network";

        if (names.Count == 1)
            return names[0];

        return string.Join(
                   ", ",
                   names.Take(names.Count - 1)) +
               " and " +
               names[^1];
    }
}
