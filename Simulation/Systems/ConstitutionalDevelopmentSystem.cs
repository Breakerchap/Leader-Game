using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

/// <summary>
/// Slowly changes the structural leverage of representative institutions.
/// There is deliberately no automatic historical march toward one regime:
/// institutions grow stronger when rulers repeatedly depend on them during
/// fiscal pressure, while some crown institutions can wither when a capable
/// centre goes decades without needing to bargain through them.
/// </summary>
internal static class ConstitutionalDevelopmentSystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        if (state.Date.Month != 1)
            return [];

        var reports = new List<SimulationReport>();

        foreach (var country in state.Countries)
        {
            var body = country.Government.LegislativeBody;

            if (body == LegislativeBodyType.None)
                continue;

            var oldIndependence =
                country.Government.LegislativeIndependence;

            var recent = state.LegislativeProposals
                .Where(proposal =>
                    ReferenceEquals(proposal.Country, country) &&
                    MonthsBetween(proposal.CreatedOn, state.Date) is >= 0 and < 12)
                .ToList();

            var rejected = recent.Count(proposal =>
                proposal.Status == LegislativeProposalStatus.Rejected);

            var activeWar = state.Wars.Any(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(country));

            var debtRatio = country.Gdp <= 0
                ? 0m
                : country.Debt / country.Gdp;

            var fiscalPressure =
                activeWar ||
                debtRatio >= 0.15m ||
                country.LastMonthlyBalance < 0;

            if (rejected > 0 ||
                (recent.Count > 0 && fiscalPressure))
            {
                var gain = 1;

                if (rejected > 0)
                    gain++;

                if (recent.Count >= 3 && fiscalPressure)
                    gain++;

                country.Government.LegislativeIndependence += gain;
            }
            else if (CanInstitutionErode(country, state))
            {
                country.Government.LegislativeIndependence -= 1;
            }

            var newIndependence =
                country.Government.LegislativeIndependence;

            if (newIndependence == oldIndependence ||
                !ReferenceEquals(country, state.Player.Country))
            {
                continue;
            }

            var oldBand = IndependenceBand(oldIndependence);
            var newBand = IndependenceBand(newIndependence);
            var significant =
                oldBand != newBand ||
                Math.Abs(newIndependence - oldIndependence) >= 2;

            if (!significant)
                continue;

            var bodyName =
                InstitutionSystem.BodyName(body);

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                newIndependence > oldIndependence
                    ? $"{bodyName} gains constitutional leverage"
                    : $"{bodyName} loses constitutional leverage",
                newIndependence > oldIndependence
                    ? $"Repeated fiscal bargaining has strengthened the practical precedent that the government must negotiate through the {bodyName}. " +
                      $"Its structural independence is now best described as {newBand.ToLowerInvariant()}."
                    : $"A long period of effective central government without meaningful fiscal bargaining through the {bodyName} has weakened its practical leverage. " +
                      $"Its structural independence is now best described as {newBand.ToLowerInvariant()}."));
        }

        return reports;
    }

    public static string DescribeIndependence(int independence) =>
        IndependenceBand(independence);

    private static bool CanInstitutionErode(
        Countries.Country country,
        GameState state)
    {
        var body = country.Government.LegislativeBody;

        if (body is not
            (LegislativeBodyType.EstatesAssembly or
             LegislativeBodyType.RoyalCouncil))
        {
            return false;
        }

        if (country.AdministrativeDevelopment <
            AdministrativeDevelopment.CentralisingBureaucracy)
        {
            return false;
        }

        if (country.Government.Stability < 70)
            return false;

        var minimum = body switch
        {
            LegislativeBodyType.EstatesAssembly => 20,
            LegislativeBodyType.RoyalCouncil => 15,
            _ => 0
        };

        if (country.Government.LegislativeIndependence <= minimum)
            return false;

        var usedRecently = state.LegislativeProposals.Any(proposal =>
            ReferenceEquals(proposal.Country, country) &&
            MonthsBetween(proposal.CreatedOn, state.Date) is >= 0 and < 60);

        if (usedRecently)
            return false;

        return RelevantRulerBacking(country) >= 65;
    }

    private static double RelevantRulerBacking(
        Countries.Country country)
    {
        var bases = country.Government.LegislativeBody switch
        {
            LegislativeBodyType.EstatesAssembly =>
                new (PowerBaseType Type, double Weight)[]
                {
                    (PowerBaseType.Aristocracy, 2.0),
                    (PowerBaseType.Clergy, 1.5),
                    (PowerBaseType.RegionalElites, 1.3),
                    (PowerBaseType.Merchants, 0.7)
                },

            LegislativeBodyType.RoyalCouncil =>
                new (PowerBaseType Type, double Weight)[]
                {
                    (PowerBaseType.Aristocracy, 2.0),
                    (PowerBaseType.Clergy, 1.2),
                    (PowerBaseType.RegionalElites, 1.2),
                    (PowerBaseType.RoyalFamily, 0.8)
                },

            _ => Array.Empty<(PowerBaseType, double)>()
        };

        if (bases.Length == 0)
            return 50;

        var weighted = 0.0;
        var total = 0.0;

        foreach (var (type, baseWeight) in bases)
        {
            var structuralWeight =
                Math.Max(
                    0.05,
                    country.GetPowerBaseStrength(type) / 100.0);
            var weight = baseWeight * structuralWeight;

            weighted +=
                country.Ruler.GetPowerBaseStanding(type) *
                weight;
            total += weight;
        }

        return total > 0
            ? weighted / total
            : 50;
    }

    private static string IndependenceBand(int independence) =>
        independence switch
        {
            >= 75 => "Dominant",
            >= 55 => "Powerful",
            >= 35 => "Established",
            >= 20 => "Limited",
            _ => "Consultative"
        };

    private static int MonthsBetween(
        GameDate from,
        GameDate to) =>
        (to.Year - from.Year) * 12 +
        (to.Month - from.Month);
}
