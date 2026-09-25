using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class CoalitionTests
{
    [Fact]
    public void DismissalCanPushFormerMarshalIntoRivalsPlot()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        var before = PoliticalCalculations.GetConspiracyAffinity(
            state,
            country,
            marshal,
            lukas);

        Assert.True(before < 65);

        var plot = new PoliticalPlot
        {
            Country = country,
            Instigator = lukas,
            Progress = 20
        };
        state.Plots.Add(plot);

        simulation.SubmitOrder(new DismissAdvisorOrder
        {
            Issuer = country.Ruler,
            Recipient = marshal,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        var after = PoliticalCalculations.GetConspiracyAffinity(
            state,
            country,
            marshal,
            lukas);

        Assert.True(after >= 65);
        Assert.Contains(marshal.Id, plot.SupporterIds);
    }

    [Fact]
    public void SupporterLeavesWhenAffinityCollapses()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        var plot = new PoliticalPlot
        {
            Country = country,
            Instigator = lukas,
            Progress = 20
        };
        plot.SupporterIds.Add(marshal.Id);
        state.Plots.Add(plot);

        var relationship = state.Relationships.GetOrCreate(marshal, lukas);
        relationship.Opinion = -100;
        relationship.Trust = 0;

        simulation.AdvanceMonth();

        Assert.DoesNotContain(marshal.Id, plot.SupporterIds);
    }
}
