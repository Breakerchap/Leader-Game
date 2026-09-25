using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class EconomySystem
{
    private const decimal MonthlyDebtInterestRate = 0.004m;

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        foreach (var country in state.Countries)
        {
            var revenue = CalculateMonthlyTaxRevenue(country);
            var administrationCost = CalculateAdministrationCost(country);
            var armyCost = CalculateArmyCost(country);
            var courtCost = CalculateCourtCost(country);
            var interest = country.Debt * MonthlyDebtInterestRate;
            var expenses = administrationCost + armyCost + courtCost + interest;
            var balance = revenue - expenses;

            country.LastMonthlyTaxRevenue = revenue;
            country.LastMonthlyExpenses = expenses;
            country.LastMonthlyDebtInterest = interest;
            country.LastMonthlyBalance = balance;

            ApplyCashFlow(country, balance);
            ApplyFundingConsequences(country);
            ApplyDebtPressure(country);

            if (ReferenceEquals(country, state.Player.Country))
            {
                var debtText = country.Debt > 0
                    ? $" Debt now stands at {country.Debt:N0}."
                    : string.Empty;

                yield return new SimulationReport(
                    state.Date,
                    ReportCategory.Economy,
                    balance >= 0 ? "Monthly budget surplus" : "Monthly budget deficit",
                    $"{country.Name} collected {revenue:N0} in tax revenue and spent " +
                    $"{expenses:N0}: administration {administrationCost:N0}, army " +
                    $"{armyCost:N0}, court and patronage {courtCost:N0}, debt interest " +
                    $"{interest:N0}. Monthly balance: {balance:N0}.{debtText}");
            }
        }
    }

    public static decimal CalculateMonthlyTaxRevenue(Country country)
    {
        return country.Gdp *
               country.TaxRate *
               country.AdministrativeEfficiency /
               12m;
    }

    private static decimal CalculateAdministrationCost(Country country)
    {
        return country.Gdp *
               0.015m /
               12m *
               country.AdministrationFunding;
    }

    private static decimal CalculateArmyCost(Country country)
    {
        return country.ArmySize *
               22m *
               country.ArmyFunding;
    }

    private static decimal CalculateCourtCost(Country country)
    {
        var baseline =
            country.Population * 0.35m / 12m +
            15_000m;

        return baseline * country.CourtFunding;
    }

    private static void ApplyCashFlow(Country country, decimal balance)
    {
        country.Treasury += balance;

        if (country.Treasury >= 0)
            return;

        country.Debt += -country.Treasury;
        country.Treasury = 0;
    }

    private static void ApplyFundingConsequences(Country country)
    {
        country.AdministrativeEfficiency +=
            (country.AdministrationFunding - 1m) * 0.003m;

        country.ArmyReadiness +=
            ((double)country.ArmyFunding - 1.0) * 4.0;

        if (country.CourtFunding < 0.75m)
        {
            var shortfall = (double)(0.75m - country.CourtFunding);
            country.Government.Stability -= shortfall * 2.0;
        }
        else if (country.CourtFunding > 1.25m)
        {
            var excess = (double)(country.CourtFunding - 1.25m);
            country.PublicUnrest -= excess * 0.5;
        }
    }

    private static void ApplyDebtPressure(Country country)
    {
        if (country.Gdp <= 0 || country.Debt <= 0)
            return;

        var debtToGdp = (double)(country.Debt / country.Gdp);

        if (debtToGdp <= 0.25)
            return;

        var monthlyPressure = Math.Min(2.0, (debtToGdp - 0.25) * 4.0);
        country.Government.Stability -= monthlyPressure;
        country.PublicUnrest += monthlyPressure * 0.5;
    }
}
