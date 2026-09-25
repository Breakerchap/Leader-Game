using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class EconomySystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        foreach (var country in state.Countries)
        {
            var revenue = CalculateMonthlyTaxRevenue(country);
            country.LastMonthlyTaxRevenue = revenue;
            country.Treasury += revenue;

            if (ReferenceEquals(country, state.Player.Country))
            {
                yield return new SimulationReport(
                    state.Date,
                    ReportCategory.Economy,
                    "Monthly revenue",
                    $"{country.Name} collected {revenue:N0} in tax revenue at a " +
                    $"{country.TaxRate:P1} tax rate and " +
                    $"{country.AdministrativeEfficiency:P0} administrative efficiency.");
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
}
