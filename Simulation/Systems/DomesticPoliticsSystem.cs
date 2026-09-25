using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class DomesticPoliticsSystem
{
    private const int DemandStandingThreshold = 40;
    private const int MinimumStructuralStrength = 40;

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var country in state.Countries)
        {
            var activeDemand = state.PowerBaseDemands.FirstOrDefault(demand =>
                !demand.IsResolved &&
                ReferenceEquals(demand.Country, country));

            if (activeDemand is not null)
            {
                ProcessDemand(state, activeDemand, reports);
                continue;
            }

            if (state.Date.Month % 3 != 0 || !country.Ruler.IsAlive)
                continue;

            var candidate = Enum.GetValues<PowerBaseType>()
                .Select(powerBase => new
                {
                    PowerBase = powerBase,
                    Strength = country.GetPowerBaseStrength(powerBase),
                    Standing = country.Ruler.GetPowerBaseStanding(powerBase)
                })
                .Where(entry =>
                    entry.Strength >= MinimumStructuralStrength &&
                    entry.Standing < DemandStandingThreshold)
                .OrderBy(entry => entry.Standing)
                .ThenByDescending(entry => entry.Strength)
                .FirstOrDefault();

            if (candidate is null)
                continue;

            var demand = CreateDemand(state, country, candidate.PowerBase);
            state.PowerBaseDemands.Add(demand);

            if (ReferenceEquals(country, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{FormatPowerBase(candidate.PowerBase)} demands action",
                    DescribeDemand(demand, initial: true)));
            }
        }

        return reports;
    }

    private static void ProcessDemand(
        GameState state,
        PowerBaseDemand demand,
        List<SimulationReport> reports)
    {
        if (IsSatisfied(state, demand))
        {
            demand.IsResolved = true;
            demand.Country.Ruler.ChangePowerBaseStanding(demand.PowerBase, 10);
            demand.Country.Government.Stability += 1.5;

            if (ReferenceEquals(demand.Country, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{FormatPowerBase(demand.PowerBase)} demand satisfied",
                    $"{DescribeDemand(demand, initial: false)} The concession improves " +
                    $"the ruler's standing with the {FormatPowerBase(demand.PowerBase).ToLowerInvariant()} " +
                    "and eases political pressure."));
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

        var rival = FindBestRival(demand.Country, demand.PowerBase);
        if (rival is not null)
        {
            rival.ChangePowerBaseStanding(
                demand.PowerBase,
                3 + severity * 2);
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
        Countries.Country country,
        PowerBaseType powerBase)
    {
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
}
