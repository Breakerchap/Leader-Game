using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PlotSystemTests
{
    [Fact]
    public void HighThreatCharacter_StartsCoupPlot()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        lukas.Influence = 95;
        lukas.Ambition = 100;
        lukas.SetAllegiance(PoliticalKeys.Country(country.Id), 20);

        var relationship = state.Relationships.GetOrCreate(lukas, country.Ruler);
        relationship.Opinion = -90;
        relationship.Trust = 5;
        relationship.Fear = 0;

        simulation.AdvanceMonth();

        Assert.Contains(
            state.Plots,
            plot => !plot.IsResolved &&
                    ReferenceEquals(plot.Instigator, lukas) &&
                    ReferenceEquals(plot.Country, country));
    }

    [Fact]
    public void LoyalLowThreatCourtier_DoesNotStartPlot()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marta = country.PoliticalFigures.Single(character =>
            character.FullName == "Marta Vogel");

        simulation.AdvanceMonth();

        Assert.DoesNotContain(
            state.Plots,
            plot => ReferenceEquals(plot.Instigator, marta));
    }

    [Fact]
    public void SuccessfulUsurpation_IsPlayerLossEvenWithinSameLineage()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var oldRuler = country.Ruler;
        var heir = state.Player.Lineage.Members.Single(character =>
            !ReferenceEquals(character, oldRuler));

        heir.Influence = 100;
        heir.Ambition = 100;
        heir.SetAllegiance(PoliticalKeys.Country(country.Id), 0);

        var relationship = state.Relationships.GetOrCreate(heir, oldRuler);
        relationship.Opinion = -100;
        relationship.Trust = 0;
        relationship.Fear = 0;

        country.Government.Stability = 0;
        country.PublicUnrest = 100;

        state.Plots.Add(new PoliticalPlot
        {
            Country = country,
            Instigator = heir,
            Progress = 99
        });

        simulation.AdvanceMonth();

        Assert.Same(heir, country.Ruler);
        Assert.True(state.Player.HasLost);
        Assert.Same(oldRuler, state.Player.CurrentCharacter);
        Assert.Contains("overthrew", state.Player.LossReason);
    }

    [Fact]
    public void FailedCoup_StripsOfficeAndInfluence()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var oldRuler = country.Ruler;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var startingInfluence = marshal.Influence;

        state.Plots.Add(new PoliticalPlot
        {
            Country = country,
            Instigator = marshal,
            Progress = 99
        });

        simulation.AdvanceMonth();

        var plot = state.Plots.Single(candidate =>
            ReferenceEquals(candidate.Instigator, marshal));

        Assert.True(plot.IsResolved);
        Assert.False(plot.Succeeded);
        Assert.Same(oldRuler, country.Ruler);
        Assert.Null(marshal.Position);
        Assert.True(marshal.Influence < startingInfluence);
        Assert.False(state.Player.HasLost);
    }
}
