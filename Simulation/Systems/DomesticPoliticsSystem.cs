using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class DomesticPoliticsSystem
{
    private const int DemandStandingThreshold = 40;
    private const int BlocStandingThreshold = 35;
    private const int MinimumStructuralStrength = 40;

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var country in state.Countries)
        {
            var activeDemands = state.PowerBaseDemands
                .Where(demand =>
                    !demand.IsResolved &&
                    ReferenceEquals(demand.Country, country))
                .ToList();

            foreach (var demand in activeDemands)
                ProcessDemand(state, demand, reports);

            if (state.Date.Month % 3 == 0 && country.Ruler.IsAlive)
                CreateNextDemandIfNeeded(state, country, reports);

            UpdatePoliticalBloc(state, country, reports);
        }

        return reports;
    }

    private static void CreateNextDemandIfNeeded(
        GameState state,
        Countries.Country country,
        List<SimulationReport> reports)
    {
        var activeDemandBases = state.PowerBaseDemands
            .Where(demand =>
                !demand.IsResolved &&
                ReferenceEquals(demand.Country, country))
            .Select(demand => demand.PowerBase)
            .ToHashSet();

        var candidate = Enum.GetValues<PowerBaseType>()
            .Select(powerBase => new
            {
                PowerBase = powerBase,
                Strength = country.GetPowerBaseStrength(powerBase),
                Standing = country.Ruler.GetPowerBaseStanding(powerBase)
            })
            .Where(entry =>
                entry.Strength >= MinimumStructuralStrength &&
                entry.Standing < DemandStandingThreshold &&
                !activeDemandBases.Contains(entry.PowerBase))
            .OrderBy(entry => entry.Standing)
            .ThenByDescending(entry => entry.Strength)
            .FirstOrDefault();

        if (candidate is null)
            return;

        var demand = CreateDemand(state, country, candidate.PowerBase);
        demand.Spokesperson = FindSpokesperson(
            state,
            country,
            candidate.PowerBase);
        state.PowerBaseDemands.Add(demand);

        if (!ReferenceEquals(country, state.Player.Country))
            return;

        var spokespersonText = demand.Spokesperson is null
            ? $"{FormatPowerBase(candidate.PowerBase)} demands action"
            : $"{demand.Spokesperson.FullName} presents " +
              $"{FormatPowerBase(candidate.PowerBase).ToLowerInvariant()} demands";

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            spokespersonText,
            DescribeDemand(demand, initial: true) +
            (demand.Spokesperson is null
                ? string.Empty
                : $" {demand.Spokesperson.FullName} has become the visible advocate for the demand.")));
    }

    private static void ProcessDemand(
        GameState state,
        PowerBaseDemand demand,
        List<SimulationReport> reports)
    {
        if (demand.Spokesperson is not null &&
            !demand.Spokesperson.IsPoliticallyActive)
        {
            demand.Spokesperson = FindSpokesperson(
                state,
                demand.Country,
                demand.PowerBase);
        }

        if (IsSatisfied(state, demand))
        {
            demand.IsResolved = true;
            demand.Country.Ruler.ChangePowerBaseStanding(demand.PowerBase, 10);
            demand.Country.Government.Stability += 1.5;

            if (demand.Spokesperson is not null)
            {
                demand.Spokesperson.ChangePowerBaseStanding(
                    demand.PowerBase,
                    2);
                demand.Spokesperson.Influence = Math.Min(
                    100,
                    demand.Spokesperson.Influence + 1);

                var towardRuler = state.Relationships.GetOrCreate(
                    demand.Spokesperson,
                    demand.Country.Ruler);
                towardRuler.ChangeOpinion(5);
                towardRuler.ChangeTrust(4);
            }

            if (ReferenceEquals(demand.Country, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{FormatPowerBase(demand.PowerBase)} demand satisfied",
                    $"{DescribeDemand(demand, initial: false)} The concession improves " +
                    $"the ruler's standing with the {FormatPowerBase(demand.PowerBase).ToLowerInvariant()} " +
                    "and eases political pressure." +
                    (demand.Spokesperson is null
                        ? string.Empty
                        : $" {demand.Spokesperson.FullName} receives political credit for securing it.")));
            }

            return;
        }

        demand.MonthsOpen++;

        if (demand.MonthsOpen % 3 != 0)
            return;

        demand.EscalationLevel++;

        var strength = demand.Country.GetPowerBaseStrength(demand.PowerBase);
        var severity = Math.Max(1, demand.EscalationLevel);

        demand.Country.Ruler.ChangePowerBaseStanding(
            demand.PowerBase,
            -(4 + severity * 2));
        demand.Country.PublicUnrest += strength / 40.0 * severity;
        demand.Country.Government.Stability -= strength / 50.0 * severity;

        var rival = demand.Spokesperson is { IsPoliticallyActive: true }
            ? demand.Spokesperson
            : FindBestRival(state, demand.Country, demand.PowerBase);

        if (rival is not null)
        {
            rival.ChangePowerBaseStanding(
                demand.PowerBase,
                3 + severity * 2);
            rival.Influence = Math.Min(
                100,
                rival.Influence + 1);

            var towardRuler = state.Relationships.GetOrCreate(
                rival,
                demand.Country.Ruler);
            towardRuler.ChangeOpinion(-(3 + severity));
            towardRuler.ChangeTrust(-2);
        }

        if (!ReferenceEquals(demand.Country, state.Player.Country))
            return;

        var rivalText = rival is null
            ? string.Empty
            : $" {rival.FullName} is increasingly benefiting from the group's frustration.";

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{FormatPowerBase(demand.PowerBase)} pressure escalates",
            $"{DescribeDemand(demand, initial: false)} It has now gone unanswered for " +
            $"{demand.MonthsOpen} months. Political stability is deteriorating.{rivalText}"));
    }

    private static void UpdatePoliticalBloc(
        GameState state,
        Countries.Country country,
        List<SimulationReport> reports)
    {
        var bloc = state.PoliticalBlocs.FirstOrDefault(candidate =>
            candidate.IsActive &&
            ReferenceEquals(candidate.Country, country));

        if (bloc is not null &&
            (!bloc.Leader.IsPoliticallyActive ||
             ReferenceEquals(bloc.Leader, country.Ruler)))
        {
            DissolveBloc(state, bloc, reports, "its leader can no longer organise opposition");
            bloc = null;
        }

        var disaffectedBases = GetDisaffectedPowerBases(state, country);

        if (bloc is null)
        {
            if (disaffectedBases.Count < 2)
                return;

            var leader = FindBlocLeader(state, country, disaffectedBases);
            if (leader is null)
                return;

            var supportiveBases = disaffectedBases
                .Where(powerBase =>
                    leader.GetPowerBaseStanding(powerBase) >=
                    country.Ruler.GetPowerBaseStanding(powerBase) + 10)
                .ToList();

            if (supportiveBases.Count < 2)
                return;

            bloc = new PoliticalBloc
            {
                Country = country,
                Leader = leader
            };

            bloc.PowerBases.UnionWith(supportiveBases);
            UpdateBlocMembership(state, bloc);
            bloc.Cohesion = CalculateBlocCohesion(bloc);
            state.PoliticalBlocs.Add(bloc);

            if (ReferenceEquals(country, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"Opposition coalesces around {leader.FullName}",
                    $"{leader.FullName} is now coordinating an organised opposition bloc " +
                    $"supported by {FormatPowerBaseList(bloc.PowerBases)}. This is not " +
                    "yet a coup, but coordinated opposition makes the government more vulnerable."));
            }

            return;
        }

        bloc.MonthsActive++;

        var qualifyingBases = disaffectedBases
            .Where(powerBase =>
                bloc.Leader.GetPowerBaseStanding(powerBase) >=
                country.Ruler.GetPowerBaseStanding(powerBase) + 5)
            .ToList();

        if (qualifyingBases.Count < 2)
        {
            DissolveBloc(state, bloc, reports, "too few important groups remain committed to it");
            return;
        }

        bloc.PowerBases.Clear();
        bloc.PowerBases.UnionWith(qualifyingBases);
        UpdateBlocMembership(state, bloc);
        bloc.Cohesion = CalculateBlocCohesion(bloc);
    }

    private static List<PowerBaseType> GetDisaffectedPowerBases(
        GameState state,
        Countries.Country country)
    {
        return Enum.GetValues<PowerBaseType>()
            .Where(powerBase =>
                country.GetPowerBaseStrength(powerBase) >= MinimumStructuralStrength &&
                IsPowerBaseDisaffected(state, country, powerBase))
            .ToList();
    }

    private static bool IsPowerBaseDisaffected(
        GameState state,
        Countries.Country country,
        PowerBaseType powerBase)
    {
        if (country.Ruler.GetPowerBaseStanding(powerBase) < BlocStandingThreshold)
            return true;

        return state.PowerBaseDemands.Any(demand =>
            !demand.IsResolved &&
            demand.EscalationLevel > 0 &&
            ReferenceEquals(demand.Country, country) &&
            demand.PowerBase == powerBase);
    }

    private static Character? FindBlocLeader(
        GameState state,
        Countries.Country country,
        IReadOnlyCollection<PowerBaseType> disaffectedBases)
    {
        var rulerAverage = disaffectedBases.Average(powerBase =>
            country.Ruler.GetPowerBaseStanding(powerBase));

        return country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, country.Ruler) &&
                character.Ambition >= 40)
            .Select(character => new
            {
                Character = character,
                AverageBacking = disaffectedBases.Average(powerBase =>
                    character.GetPowerBaseStanding(powerBase)),
                Willingness = PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    character,
                    country.Ruler)
            })
            .Where(entry =>
                entry.AverageBacking >= rulerAverage + 10)
            .OrderByDescending(entry =>
                entry.AverageBacking * 0.45 +
                entry.Character.Ambition * 0.20 +
                entry.Character.Influence * 0.20 +
                (100 - entry.Willingness) * 0.15)
            .Select(entry => entry.Character)
            .FirstOrDefault();
    }

    private static void UpdateBlocMembership(
        GameState state,
        PoliticalBloc bloc)
    {
        bloc.MemberIds.Clear();

        foreach (var character in bloc.Country.PoliticalFigures)
        {
            if (!character.IsPoliticallyActive ||
                ReferenceEquals(character, bloc.Country.Ruler) ||
                ReferenceEquals(character, bloc.Leader))
            {
                continue;
            }

            var towardLeader = state.Relationships.GetOrCreate(
                character,
                bloc.Leader);
            var towardRuler = state.Relationships.GetOrCreate(
                character,
                bloc.Country.Ruler);

            var normalisedLeaderOpinion = (towardLeader.Opinion + 100) / 2.0;
            var backing = bloc.PowerBases.Average(powerBase =>
                character.GetPowerBaseStanding(powerBase));

            var membershipScore =
                towardLeader.Trust * 0.25 +
                normalisedLeaderOpinion * 0.20 +
                (100 - towardRuler.Trust) * 0.15 +
                backing * 0.25 +
                character.Ambition * 0.15;

            if (membershipScore >= 60)
                bloc.MemberIds.Add(character.Id);
        }
    }

    private static double CalculateBlocCohesion(PoliticalBloc bloc)
    {
        var ruler = bloc.Country.Ruler;

        var grievance = bloc.PowerBases.Average(powerBase =>
            100 - ruler.GetPowerBaseStanding(powerBase));
        var leaderBacking = bloc.PowerBases.Average(powerBase =>
            bloc.Leader.GetPowerBaseStanding(powerBase));

        return Math.Clamp(
            grievance * 0.55 +
            leaderBacking * 0.45,
            0,
            100);
    }

    private static void DissolveBloc(
        GameState state,
        PoliticalBloc bloc,
        List<SimulationReport> reports,
        string reason)
    {
        bloc.IsActive = false;

        if (!ReferenceEquals(bloc.Country, state.Player.Country))
            return;

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{bloc.Leader.FullName}'s opposition bloc fractures",
            $"The organised opposition around {bloc.Leader.FullName} has broken apart because {reason}."));
    }

    private static Character? FindSpokesperson(
        GameState state,
        Countries.Country country,
        PowerBaseType powerBase)
    {
        return country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, country.Ruler))
            .Select(character => new
            {
                Character = character,
                Standing = character.GetPowerBaseStanding(powerBase),
                OfficeBonus = RelevantOfficeBonus(
                    character.Position,
                    powerBase),
                Willingness = PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    character,
                    country.Ruler)
            })
            .Where(entry => entry.Standing >= 55)
            .OrderByDescending(entry =>
                entry.Standing * 0.50 +
                entry.Character.Influence * 0.20 +
                entry.Character.Ambition * 0.15 +
                (100 - entry.Willingness) * 0.10 +
                entry.OfficeBonus)
            .Select(entry => entry.Character)
            .FirstOrDefault();
    }

    private static int RelevantOfficeBonus(
        Position? position,
        PowerBaseType powerBase)
    {
        return (position, powerBase) switch
        {
            (Position.Marshal, PowerBaseType.Military) => 12,
            (Position.Treasurer, PowerBaseType.Merchants) => 10,
            (Position.Treasurer, PowerBaseType.Bureaucracy) => 8,
            (Position.Chancellor, PowerBaseType.Bureaucracy) => 10,
            (Position.Chancellor, PowerBaseType.Party) => 10,
            (Position.Chancellor, PowerBaseType.RegionalElites) => 8,
            (Position.Chancellor, PowerBaseType.Aristocracy) => 6,
            _ => 0
        };
    }

    private static PowerBaseDemand CreateDemand(
        GameState state,
        Countries.Country country,
        PowerBaseType powerBase)
    {
        var activeWar = state.Wars.Any(war =>
            war.Status == WarStatus.Active &&
            war.IsParticipant(country));

        if (powerBase == PowerBaseType.Military &&
            activeWar &&
            country.WarExhaustion >= 45)
        {
            return NewDemand(country, powerBase, PowerBaseDemandType.EndWar);
        }

        return powerBase switch
        {
            PowerBaseType.Military =>
                NewDemand(
                    country,
                    powerBase,
                    PowerBaseDemandType.RaiseArmyFunding,
                    Math.Min(1.5m, Math.Max(1.2m, country.ArmyFunding + 0.20m))),

            PowerBaseType.Bureaucracy =>
                NewDemand(
                    country,
                    powerBase,
                    PowerBaseDemandType.RaiseAdministrationFunding,
                    Math.Min(1.5m, Math.Max(1.15m, country.AdministrationFunding + 0.15m))),

            PowerBaseType.Aristocracy or
            PowerBaseType.Clergy or
            PowerBaseType.RegionalElites or
            PowerBaseType.RoyalFamily =>
                NewDemand(
                    country,
                    powerBase,
                    PowerBaseDemandType.RaiseCourtFunding,
                    Math.Min(1.5m, Math.Max(1.15m, country.CourtFunding + 0.15m))),

            _ =>
                NewDemand(
                    country,
                    powerBase,
                    PowerBaseDemandType.LowerTaxes,
                    Math.Max(0.02m, country.TaxRate - 0.02m))
        };
    }

    private static PowerBaseDemand NewDemand(
        Countries.Country country,
        PowerBaseType powerBase,
        PowerBaseDemandType type,
        decimal targetValue = 0m)
    {
        return new PowerBaseDemand
        {
            Country = country,
            PowerBase = powerBase,
            Type = type,
            TargetValue = targetValue
        };
    }

    private static bool IsSatisfied(GameState state, PowerBaseDemand demand)
    {
        var country = demand.Country;

        return demand.Type switch
        {
            PowerBaseDemandType.LowerTaxes =>
                country.TaxRate <= demand.TargetValue,

            PowerBaseDemandType.RaiseArmyFunding =>
                country.ArmyFunding >= demand.TargetValue,

            PowerBaseDemandType.RaiseAdministrationFunding =>
                country.AdministrationFunding >= demand.TargetValue,

            PowerBaseDemandType.RaiseCourtFunding =>
                country.CourtFunding >= demand.TargetValue,

            PowerBaseDemandType.EndWar =>
                !state.Wars.Any(war =>
                    war.Status == WarStatus.Active &&
                    war.IsParticipant(country)),

            _ => false
        };
    }

    private static Character? FindBestRival(
        GameState state,
        Countries.Country country,
        PowerBaseType powerBase)
    {
        var blocLeader = state.PoliticalBlocs
            .Where(bloc =>
                bloc.IsActive &&
                ReferenceEquals(bloc.Country, country) &&
                bloc.PowerBases.Contains(powerBase) &&
                bloc.Leader.IsPoliticallyActive)
            .OrderByDescending(bloc => bloc.Cohesion)
            .Select(bloc => bloc.Leader)
            .FirstOrDefault();

        if (blocLeader is not null)
            return blocLeader;

        return country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, country.Ruler))
            .OrderByDescending(character =>
                character.GetPowerBaseStanding(powerBase))
            .ThenByDescending(character => character.Ambition)
            .FirstOrDefault();
    }

    public static string DescribeDemand(
        PowerBaseDemand demand,
        bool initial)
    {
        var prefix = initial
            ? $"The {FormatPowerBase(demand.PowerBase).ToLowerInvariant()} are dissatisfied with the government and "
            : $"The {FormatPowerBase(demand.PowerBase).ToLowerInvariant()} demand remains unresolved: ";

        var request = demand.Type switch
        {
            PowerBaseDemandType.LowerTaxes =>
                $"want the tax rate reduced to {demand.TargetValue:P0} or lower.",

            PowerBaseDemandType.RaiseArmyFunding =>
                $"want army funding raised to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.RaiseAdministrationFunding =>
                $"want administration funding raised to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.RaiseCourtFunding =>
                $"want court and patronage funding raised to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.EndWar =>
                "want the government to end its current war.",

            _ => "want a political concession."
        };

        return prefix + request;
    }

    public static string FormatPowerBase(PowerBaseType powerBase)
    {
        return powerBase switch
        {
            PowerBaseType.RegionalElites => "Regional elites",
            PowerBaseType.RoyalFamily => "Royal family",
            _ => powerBase.ToString()
        };
    }

    private static string FormatPowerBaseList(IEnumerable<PowerBaseType> powerBases)
    {
        var names = powerBases
            .Select(FormatPowerBase)
            .OrderBy(name => name)
            .ToList();

        if (names.Count == 0)
            return "no major groups";

        if (names.Count == 1)
            return names[0];

        return string.Join(", ", names.Take(names.Count - 1)) +
               " and " + names[^1];
    }
}
