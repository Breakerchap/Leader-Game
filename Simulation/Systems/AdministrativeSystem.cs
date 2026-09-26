using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class AdministrativeSystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var country in state.Countries)
        {
            foreach (var office in country.AdministrativeOffices)
            {
                UpdateWorkload(state, country, office);
                UpdateInstitution(country, office);
            }

            UpdateAdministrativeEfficiency(country);
            ApplyLocalAdministrationConsequences(country);

            var developmentReport =
                TryAdvanceAdministrativeDevelopment(
                    state,
                    country);

            if (developmentReport is not null &&
                ReferenceEquals(country, state.Player.Country))
            {
                reports.Add(developmentReport);
            }

            if (ReferenceEquals(country, state.Player.Country) &&
                state.Date.Month % 3 == 0)
            {
                var worst = country.AdministrativeOffices
                    .OrderBy(office => GetPerformance(country, office.Function))
                    .FirstOrDefault();

                if (worst is not null &&
                    GetPerformance(country, worst.Function) < 32)
                {
                    reports.Add(new SimulationReport(
                        state.Date,
                        ReportCategory.Politics,
                        $"{worst.Name} is struggling to carry the government's business",
                        $"{worst.Name} is visibly overextended. Orders are moving slowly, " +
                        "local execution is uneven, and the centre is increasingly dependent " +
                        "on intermediaries to get business done."));
                }
            }
        }

        return reports;
    }

    public static double GetPerformance(
        Country country,
        AdministrativeFunction function)
    {
        var office = country.GetAdministrativeOffice(function);

        if (office is null)
            return (double)country.AdministrativeEfficiency * 100.0;

        var head = office.ResponsiblePosition is { } position
            ? country.GetOfficeHolder(position)
            : null;

        var headCompetence = head?.Competence ?? country.Ruler.Competence;

        var score =
            office.Capacity * 0.35 +
            office.Reach * 0.25 +
            office.Integrity * 0.15 +
            headCompetence * 0.25 -
            office.Workload * 0.20;

        score += ((double)country.AdministrationFunding - 1.0) * 25.0;

        if (office.PatronageDependence > 50)
        {
            score -=
                (office.PatronageDependence - 50) * 0.08;
        }

        return Math.Clamp(score, 0, 100);
    }

    public static decimal GetImplementationModifier(
        Country country,
        AdministrativeFunction function)
    {
        var performance = GetPerformance(country, function);

        return Math.Clamp(
            0.70m + (decimal)performance / 200m,
            0.70m,
            1.10m);
    }

    public static string DescribePerformance(double performance) =>
        performance switch
        {
            >= 75 => "Strong",
            >= 60 => "Capable",
            >= 45 => "Uneven",
            >= 30 => "Strained",
            _ => "Weak"
        };

    public static string DescribeReach(int reach) =>
        reach switch
        {
            >= 75 => "Extensive",
            >= 60 => "Broad",
            >= 45 => "Patchy",
            >= 30 => "Limited",
            _ => "Very limited"
        };

    public static string DescribeIntegrity(int integrity) =>
        integrity switch
        {
            >= 75 => "Disciplined",
            >= 60 => "Generally reliable",
            >= 45 => "Leakage common",
            >= 30 => "Patronage-heavy",
            _ => "Highly compromised"
        };

    private static void UpdateWorkload(
        GameState state,
        Country country,
        AdministrativeOffice office)
    {
        var activeWars = state.Wars.Count(war =>
            war.Status == WarStatus.Active &&
            war.IsParticipant(country));

        var diplomacyLoad = state.Diplomacy
            .ForCountry(country)
            .Count();

        var pendingLegislation = state.LegislativeProposals.Count(proposal =>
            ReferenceEquals(proposal.Country, country) &&
            proposal.Status == Politics.LegislativeProposalStatus.Pending);

        var target = office.Function switch
        {
            AdministrativeFunction.Chancery =>
                28 +
                country.PoliticalFigures.Count * 2 +
                pendingLegislation * 6,

            AdministrativeFunction.Revenue =>
                32 +
                (int)Math.Round((double)country.TaxRate * 120) +
                (country.Debt > country.Gdp * 0.20m ? 10 : 0),

            AdministrativeFunction.LocalGovernment =>
                25 +
                (int)Math.Round(country.PublicUnrest * 0.45) +
                (country.Population > 1_000_000 ? 8 : 0),

            AdministrativeFunction.MilitaryLogistics =>
                22 +
                activeWars * 38 +
                (country.ArmySize > 8_000 ? 8 : 0),

            AdministrativeFunction.ForeignAffairs =>
                20 +
                diplomacyLoad * 5 +
                activeWars * 12,

            _ => 35
        };

        office.Workload = MoveToward(
            office.Workload,
            Math.Clamp(target, 0, 100),
            8);
    }

    private static void UpdateInstitution(
        Country country,
        AdministrativeOffice office)
    {
        var funding = country.AdministrationFunding;
        var head = office.ResponsiblePosition is { } position
            ? country.GetOfficeHolder(position)
            : null;
        var competence = head?.Competence ?? country.Ruler.Competence;

        if (funding < 0.75m)
            office.Capacity -= 1;
        else if (funding > 1.15m && competence >= 60)
            office.Capacity += 1;

        if (office.Workload >= 85 &&
            office.Workload > office.Capacity + 15)
        {
            office.Capacity -= 1;
        }

        if (funding > 1.20m &&
            office.Capacity >= 60 &&
            office.Reach < office.Capacity)
        {
            office.Reach += 1;
        }

        if (office.PatronageDependence >= 75 &&
            office.Integrity > 25)
        {
            office.Integrity -= 1;
        }
        else if (office.PatronageDependence <= 35 &&
                 competence >= 75 &&
                 funding >= 1.0m)
        {
            office.Integrity += 1;
        }
    }

    private static void ApplyLocalAdministrationConsequences(
        Country country)
    {
        var localPerformance =
            GetPerformance(
                country,
                AdministrativeFunction.LocalGovernment);

        // Weak local government does not immediately produce revolt; it makes
        // ordinary disorder, evasion and non-compliance harder to contain.
        // The monthly effect is deliberately modest so long-term institutional
        // weakness matters without creating a death spiral in a few turns.
        if (localPerformance < 30)
        {
            country.PublicUnrest +=
                0.12 + (30 - localPerformance) * 0.012;
            country.Government.Stability -=
                0.06 + (30 - localPerformance) * 0.007;
            return;
        }

        if (localPerformance < 45 &&
            country.PublicUnrest >= 45)
        {
            country.PublicUnrest +=
                (45 - localPerformance) * 0.006;
            return;
        }

        if (localPerformance >= 75 &&
            country.PublicUnrest > 15)
        {
            country.PublicUnrest -= 0.06;
        }
    }

    public static string DescribeDevelopment(
        AdministrativeDevelopment development) =>
        development switch
        {
            AdministrativeDevelopment.PatrimonialCourt =>
                "Patrimonial court administration",
            AdministrativeDevelopment.CollegiateCivic =>
                "Collegiate civic administration",
            AdministrativeDevelopment.CentralisingBureaucracy =>
                "Centralising bureaucracy",
            AdministrativeDevelopment.FiscalMilitaryState =>
                "Fiscal-military administration",
            AdministrativeDevelopment.ProfessionalCivilService =>
                "Professional civil service",
            AdministrativeDevelopment.MassAdministrativeState =>
                "Mass administrative state",
            _ => development.ToString()
        };

    private static SimulationReport? TryAdvanceAdministrativeDevelopment(
        GameState state,
        Country country)
    {
        var current = country.AdministrativeDevelopment;
        var next = current switch
        {
            AdministrativeDevelopment.PatrimonialCourt
                when state.Date.Year >= 1500 &&
                     CorePerformance(country) >= 58 &&
                     CoreReach(country) >= 48 &&
                     AveragePatronage(country) <= 68 =>
                AdministrativeDevelopment.CentralisingBureaucracy,

            AdministrativeDevelopment.CollegiateCivic
                when state.Date.Year >= 1500 &&
                     CorePerformance(country) >= 62 &&
                     CoreReach(country) >= 55 &&
                     AveragePatronage(country) <= 55 =>
                AdministrativeDevelopment.CentralisingBureaucracy,

            AdministrativeDevelopment.CentralisingBureaucracy
                when state.Date.Year >= 1600 &&
                     GetPerformance(
                         country,
                         AdministrativeFunction.Revenue) >= 64 &&
                     GetPerformance(
                         country,
                         AdministrativeFunction.MilitaryLogistics) >= 60 &&
                     CoreReach(country) >= 58 =>
                AdministrativeDevelopment.FiscalMilitaryState,

            AdministrativeDevelopment.FiscalMilitaryState
                when state.Date.Year >= 1800 &&
                     CorePerformance(country) >= 70 &&
                     AverageIntegrity(country) >= 64 &&
                     AveragePatronage(country) <= 48 &&
                     CoreReach(country) >= 66 =>
                AdministrativeDevelopment.ProfessionalCivilService,

            AdministrativeDevelopment.ProfessionalCivilService
                when state.Date.Year >= 1900 &&
                     CorePerformance(country) >= 78 &&
                     AverageIntegrity(country) >= 72 &&
                     AveragePatronage(country) <= 38 &&
                     CoreReach(country) >= 76 =>
                AdministrativeDevelopment.MassAdministrativeState,

            _ => current
        };

        if (next == current)
            return null;

        country.AdministrativeDevelopment = next;

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{country.Name}'s state administration changes character",
            $"{DescribeDevelopment(current)} has developed into " +
            $"{DescribeDevelopment(next).ToLowerInvariant()}. " +
            "This reflects accumulated administrative capacity rather than a reform granted automatically by the calendar.");
    }

    private static double CorePerformance(Country country) =>
        new[]
        {
            AdministrativeFunction.Chancery,
            AdministrativeFunction.Revenue,
            AdministrativeFunction.LocalGovernment
        }
        .Average(function => GetPerformance(country, function));

    private static double CoreReach(Country country)
    {
        var offices = country.AdministrativeOffices
            .Where(office =>
                office.Function is
                    AdministrativeFunction.Chancery or
                    AdministrativeFunction.Revenue or
                    AdministrativeFunction.LocalGovernment)
            .ToList();

        return offices.Count == 0
            ? (double)country.AdministrativeEfficiency * 100.0
            : offices.Average(office => office.Reach);
    }

    private static double AverageIntegrity(Country country) =>
        country.AdministrativeOffices.Count == 0
            ? 50
            : country.AdministrativeOffices.Average(office =>
                office.Integrity);

    private static double AveragePatronage(Country country) =>
        country.AdministrativeOffices.Count == 0
            ? 100
            : country.AdministrativeOffices.Average(office =>
                office.PatronageDependence);

    private static void UpdateAdministrativeEfficiency(Country country)
    {
        if (country.AdministrativeOffices.Count == 0)
            return;

        var chancery =
            GetPerformance(country, AdministrativeFunction.Chancery);
        var revenue =
            GetPerformance(country, AdministrativeFunction.Revenue);
        var local =
            GetPerformance(country, AdministrativeFunction.LocalGovernment);

        var administrativePerformance =
            chancery * 0.25 +
            revenue * 0.35 +
            local * 0.40;

        var target =
            0.35m +
            (decimal)(administrativePerformance / 100.0) * 0.65m;

        var difference =
            target - country.AdministrativeEfficiency;

        country.AdministrativeEfficiency +=
            Math.Clamp(difference, -0.006m, 0.006m);
    }

    private static int MoveToward(
        int current,
        int target,
        int step)
    {
        if (current < target)
            return Math.Min(target, current + step);

        if (current > target)
            return Math.Max(target, current - step);

        return current;
    }
}
