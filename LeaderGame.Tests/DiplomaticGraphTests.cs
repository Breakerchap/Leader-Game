using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class DiplomaticGraphTests
{
    [Fact]
    public void Relation_IsSameObjectRegardlessOfCountryOrder()
    {
        var state = DemoScenario.Create();
        var falkenreich = state.Player.Country;
        var nordmark = state.Countries.Single(country => country.Id == "nordmark");

        var forward = state.Diplomacy.GetOrCreate(falkenreich, nordmark);
        var reverse = state.Diplomacy.GetOrCreate(nordmark, falkenreich);

        Assert.Same(forward, reverse);
    }

    [Fact]
    public void DemoScenario_HasThreeSimulatedCountriesAndBorders()
    {
        var state = DemoScenario.Create();
        var falkenreich = state.Player.Country;
        var nordmark = state.Countries.Single(country => country.Id == "nordmark");
        var valeria = state.Countries.Single(country => country.Id == "valeria");

        Assert.Equal(3, state.Countries.Count);
        Assert.True(falkenreich.IsNeighbor(nordmark));
        Assert.True(falkenreich.IsNeighbor(valeria));
        Assert.True(nordmark.IsNeighbor(falkenreich));
        Assert.True(valeria.IsNeighbor(falkenreich));

        var nordmarkRelation = state.Diplomacy.GetOrCreate(falkenreich, nordmark);
        var valeriaRelation = state.Diplomacy.GetOrCreate(falkenreich, valeria);

        Assert.True(nordmarkRelation.Relations > 0);
        Assert.True(valeriaRelation.Relations < 0);
    }
}
