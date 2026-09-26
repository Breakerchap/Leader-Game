using LeaderGame.Simulation;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class EconomyTests
{
    [Fact]
    public void AdvanceMonth_CollectsRevenueAndPaysRecurringCosts()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;

        simulation.AdvanceMonth();

        Assert.True(country.LastMonthlyTaxRevenue > 500_000m);
        Assert.InRange(country.LastMonthlyExpenses, 338_108m, 338_109m);
        Assert.Equal(
            country.LastMonthlyTaxRevenue +
            country.LastMonthlyTradeIncome -
            country.LastMonthlyExpenses -
            country.LastMonthlyDebtInterest,
            country.LastMonthlyBalance);
        Assert.Equal(
            320_000m + country.LastMonthlyBalance,
            country.Treasury);
        Assert.Equal(0m, country.Debt);
        Assert.Equal(new GameDate(1450, 2), state.Date);
    }

    [Fact]
    public void DeficitWithNoCash_CreatesDebt()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;

        country.TaxRate = 0m;
        country.Treasury = 0m;

        simulation.AdvanceMonth();

        Assert.Equal(0m, country.Treasury);
        Assert.True(country.Debt > 300_000m);
        Assert.True(country.LastMonthlyBalance < 0m);
    }

    [Fact]
    public void UnderfundingAdministrationAndArmy_DegradesStateCapacity()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;

        country.AdministrationFunding = 0.5m;
        country.ArmyFunding = 0.5m;

        var efficiency = country.AdministrativeEfficiency;
        var readiness = country.ArmyReadiness;

        simulation.AdvanceMonth();

        Assert.True(country.AdministrativeEfficiency < efficiency);
        Assert.True(country.ArmyReadiness < readiness);
    }
}
