using LeaderGame.Simulation;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class DiplomacySystemTests
{
    [Fact]
    public void TradeAgreement_SlowlyBuildsTrustAndReducesTension()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var target = state.Countries.Single(candidate => candidate.Id == "nordmark");
        var relation = state.Diplomacy.GetOrCreate(country, target);

        relation.HasTradeAgreement = true;
        relation.Relations = 20;
        relation.Trust = 50;
        relation.Tension = 30;
        state.Date = new GameDate(1450, 6);

        simulation.AdvanceMonth();

        Assert.Equal(21, relation.Relations);
        Assert.Equal(51, relation.Trust);
        Assert.Equal(29, relation.Tension);
    }

    [Fact]
    public void SevereUnresolvedTension_CanDegradeRelations()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var target = state.Countries.Single(candidate => candidate.Id == "valeria");
        var relation = state.Diplomacy.GetOrCreate(country, target);

        relation.HasTradeAgreement = false;
        relation.BorderDisputeSeverity = 0;
        relation.Relations = -60;
        relation.Trust = 25;
        relation.Tension = 80;
        state.Date = new GameDate(1450, 3);

        simulation.AdvanceMonth();

        Assert.Equal(-61, relation.Relations);
        Assert.Equal(24, relation.Trust);
        Assert.Equal(80, relation.Tension);
    }
}
