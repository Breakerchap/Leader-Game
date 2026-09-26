using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
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
    public static SimulationReport ProcessPlayerAction(
        GameState state,
        OppositionActionOrder order)
    {
        var player = state.Player;
        var country = order.Country;
        var leader = order.Issuer;

        if (!ReferenceEquals(country, player.Country) ||
            player.IsInPower ||
            !player.Lineage.Contains(leader) ||
            !ReferenceEquals(leader, player.CurrentCharacter))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Opposition action rejected",
                "Only the controlled player lineage can organise opposition while outside government.");
        }

        if (!leader.IsPoliticallyActive)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Opposition action unavailable",
                $"{leader.FullName} cannot organise openly while {leader.Status.ToString().ToLowerInvariant()}.");
        }

        if (country.GetPowerBaseStrength(order.TargetPowerBase) <= 0)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Opposition action rejected",
                $"{DomesticPoliticsSystem.FormatPowerBase(order.TargetPowerBase)} has no meaningful political weight in {country.Name}.");
        }

        return order.ActionType switch
        {
            OppositionActionType.OrganiseSupport =>
                OrganiseSupport(state, order),
            OppositionActionType.BuildCoalition =>
                BuildCoalition(state, order),
            OppositionActionType.PublicPressure =>
                ApplyPlayerPressure(state, order),
            _ => RejectUnknownAction(state, order)
        };
    }

    private static SimulationReport OrganiseSupport(
        GameState state,
        OppositionActionOrder order)
    {
        var country = order.Country;
        var leader = order.Issuer;
        var powerBase = order.TargetPowerBase;

        var backing = leader.GetPowerBaseStanding(powerBase);
        var structuralStrength = country.GetPowerBaseStrength(powerBase);

        var organisationScore =
            leader.Competence * 0.30 +
            leader.Influence * 0.25 +
            leader.Ambition * 0.20 +
            backing * 0.15 +
            structuralStrength * 0.10;

        var gain = Math.Clamp(
            (int)Math.Round((organisationScore - 25) / 15.0),
            1,
            6);

        leader.ChangePowerBaseStanding(powerBase, gain);
        leader.Influence = Math.Min(
            100,
            leader.Influence + Math.Max(1, gain / 2));

        var bloc = FindPlayerBloc(state);
        if (bloc is not null)
        {
            bloc.PowerBases.Add(powerBase);
            bloc.Cohesion = Math.Min(100, bloc.Cohesion + 2);
        }

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{leader.FullName} organises {DomesticPoliticsSystem.FormatPowerBase(powerBase).ToLowerInvariant()} support",
            $"{playerLineage(state)} spends the month building meetings, organisers, patrons and local contacts among " +
            $"{DomesticPoliticsSystem.FormatPowerBase(powerBase).ToLowerInvariant()}. The effort strengthens " +
            $"{leader.FullName}'s position with that constituency and modestly raises their political profile.");
    }

    private static SimulationReport BuildCoalition(
        GameState state,
        OppositionActionOrder order)
    {
        var country = order.Country;
        var leader = order.Issuer;
        var powerBase = order.TargetPowerBase;

        var recruit = country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, country.Ruler) &&
                !ReferenceEquals(character, leader) &&
                !state.Player.Lineage.Contains(character))
            .Select(character => new
            {
                Character = character,
                TowardLeader = state.Relationships.GetOrCreate(
                    character,
                    leader),
                TowardRuler = state.Relationships.GetOrCreate(
                    character,
                    country.Ruler)
            })
            .OrderByDescending(entry =>
                entry.Character.GetPowerBaseStanding(powerBase) * 0.35 +
                entry.TowardLeader.Trust * 0.20 +
                (entry.TowardLeader.Opinion + 100) / 2.0 * 0.15 +
                (100 - entry.TowardRuler.Trust) * 0.15 +
                entry.Character.Ambition * 0.15)
            .Select(entry => entry.Character)
            .FirstOrDefault();

        if (recruit is null)
        {
            leader.ChangePowerBaseStanding(powerBase, 1);
            leader.Influence = Math.Min(100, leader.Influence + 1);
            order.Status = OrderStatus.Completed;

            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                $"{leader.FullName} strengthens the opposition network",
                $"No useful new political partner is available, so {playerLineage(state)} spends the month consolidating its existing contacts.");
        }

        var towardLeader = state.Relationships.GetOrCreate(
            recruit,
            leader);
        var towardRuler = state.Relationships.GetOrCreate(
            recruit,
            country.Ruler);

        var approachStrength =
            leader.Competence * 0.30 +
            leader.Influence * 0.25 +
            leader.GetPowerBaseStanding(powerBase) * 0.25 +
            recruit.GetPowerBaseStanding(powerBase) * 0.20;

        var relationshipGain = Math.Clamp(
            (int)Math.Round((approachStrength - 20) / 18.0),
            2,
            7);

        towardLeader.ChangeOpinion(relationshipGain);
        towardLeader.ChangeTrust(Math.Max(2, relationshipGain - 1));
        towardRuler.ChangeTrust(-1);
        leader.ChangePowerBaseStanding(powerBase, 1);
        leader.Influence = Math.Min(100, leader.Influence + 1);

        var bloc = FindPlayerBloc(state);
        if (bloc is null)
        {
            var supportiveBases = Enum.GetValues<PowerBaseType>()
                .Where(candidate =>
                    country.GetPowerBaseStrength(candidate) >= 20 &&
                    leader.GetPowerBaseStanding(candidate) >=
                        country.Ruler.GetPowerBaseStanding(candidate))
                .OrderByDescending(candidate =>
                    leader.GetPowerBaseStanding(candidate) -
                    country.Ruler.GetPowerBaseStanding(candidate))
                .Take(2)
                .ToList();

            if (!supportiveBases.Contains(powerBase) &&
                country.GetPowerBaseStrength(powerBase) >= 20)
            {
                supportiveBases.Insert(0, powerBase);
                supportiveBases = supportiveBases.Take(2).ToList();
            }

            if (supportiveBases.Count >= 2)
            {
                bloc = new PoliticalBloc
                {
                    Country = country,
                    Leader = leader,
                    Cohesion = 45
                };
                bloc.PowerBases.UnionWith(supportiveBases);
                state.PoliticalBlocs.Add(bloc);
            }
        }

        if (bloc is not null)
        {
            bloc.PowerBases.Add(powerBase);
            if (towardLeader.Trust >= 55 &&
                towardLeader.Opinion >= 0)
            {
                bloc.MemberIds.Add(recruit.Id);
            }

            bloc.Cohesion = Math.Min(
                100,
                bloc.Cohesion + 3);
        }

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{leader.FullName} courts {recruit.FullName}",
            $"{leader.FullName} spends the month building a working relationship with {recruit.FullName}, " +
            $"using shared interests among {DomesticPoliticsSystem.FormatPowerBase(powerBase).ToLowerInvariant()} as common ground. " +
            "The approach strengthens the opposition network, but does not guarantee lasting loyalty.");
    }

    private static SimulationReport ApplyPlayerPressure(
        GameState state,
        OppositionActionOrder order)
    {
        var country = order.Country;
        var leader = order.Issuer;
        var ruler = country.Ruler;
        var powerBase = order.TargetPowerBase;

        var leaderBacking = leader.GetPowerBaseStanding(powerBase);
        var rulerBacking = ruler.GetPowerBaseStanding(powerBase);
        var structuralStrength = country.GetPowerBaseStrength(powerBase);

        var pressureStrength =
            leaderBacking * 0.45 +
            structuralStrength * 0.30 +
            leader.Influence * 0.25;

        if (pressureStrength < 42 ||
            leaderBacking + 15 < rulerBacking)
        {
            leader.ChangePowerBaseStanding(powerBase, -2);
            leader.Influence = Math.Max(0, leader.Influence - 1);
            ruler.ChangePowerBaseStanding(powerBase, 1);
            order.Status = OrderStatus.Failed;

            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                $"{leader.FullName}'s pressure campaign falters",
                $"{playerLineage(state)} tries to mobilise {DomesticPoliticsSystem.FormatPowerBase(powerBase).ToLowerInvariant()} against the government, " +
                "but the network is not strong enough. The failed show of strength costs the opposition some credibility.");
        }

        var impact = Math.Clamp(
            (int)Math.Round((pressureStrength - 35) / 18.0),
            1,
            4);

        ruler.ChangePowerBaseStanding(powerBase, -impact);
        leader.ChangePowerBaseStanding(powerBase, 1);
        leader.Influence = Math.Min(100, leader.Influence + 1);
        country.Government.Stability -= 0.20 + impact * 0.18;
        country.PublicUnrest += 0.15 + impact * 0.15;

        var bloc = FindPlayerBloc(state);
        if (bloc is not null)
        {
            bloc.PowerBases.Add(powerBase);
            bloc.Cohesion = Math.Min(100, bloc.Cohesion + 2);
        }

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{leader.FullName} applies public pressure",
            $"{playerLineage(state)} mobilises sympathetic {DomesticPoliticsSystem.FormatPowerBase(powerBase).ToLowerInvariant()} against the government. " +
            "The campaign damages the ruler's standing with that constituency and adds visible political strain, while also raising wider tension.");
    }

    private static PoliticalBloc? FindPlayerBloc(GameState state) =>
        state.PoliticalBlocs.FirstOrDefault(bloc =>
            bloc.IsActive &&
            ReferenceEquals(bloc.Country, state.Player.Country) &&
            state.Player.Lineage.Contains(bloc.Leader));

    private static string playerLineage(GameState state) =>
        state.Player.Lineage.Name;

    private static SimulationReport RejectUnknownAction(
        GameState state,
        OppositionActionOrder order)
    {
        order.Status = OrderStatus.Rejected;
        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            "Opposition action rejected",
            "The requested opposition strategy is not recognised.");
    }

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
            var towardRuler = state.Relationships.GetOrCreate(
                insider,
                country.Ruler);

            if (!ReferenceEquals(insider, bloc.Leader))
            {
                var towardLeader = state.Relationships.GetOrCreate(
                    insider,
                    bloc.Leader);
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
