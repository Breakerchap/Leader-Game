using LeaderGame.Simulation;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class EconomyTests
{
    [Fact]
    public void AdvanceMonth_CollectsExpectedTaxRevenue()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var startingTreasury = country.Treasury;

        simulation.AdvanceMonth();

        Assert.Equal(531_250m, country.LastMonthlyTaxRevenue);
        Assert.Equal(startingTreasury + 531_250m, country.Treasury);
        Assert.Equal(new GameDate(1450, 2), state.Date);
    }
}
