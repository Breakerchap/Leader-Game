using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PoliticalResponseTests
{
    [Fact]
    public void Dismissal_RemovesOfficeAndCreatesGrievance()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var relationship = state.Relationships.GetOrCreate(marshal, country.Ruler);
        var oldOpinion = relationship.Opinion;
        var oldInfluence = marshal.Influence;

        simulation.SubmitOrder(new DismissAdvisorOrder
        {
            Issuer = country.Ruler,
            Recipient = marshal,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        Assert.Null(marshal.Position);
        Assert.True(marshal.Influence < oldInfluence);
        Assert.True(relationship.Opinion < oldOpinion);
    }

    [Fact]
    public void StrongInvestigation_RevealsAndDisruptsRealPlot()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var subject = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        var plot = new PoliticalPlot
        {
            Country = country,
            Instigator = subject,
            Progress = 60
        };
        state.Plots.Add(plot);

        chancellor.Competence = 100;
        var chancellorToRuler =
            state.Relationships.GetOrCreate(chancellor, country.Ruler);
        chancellorToRuler.Opinion = 100;
        chancellorToRuler.Trust = 100;
        chancellorToRuler.Fear = 50;
        chancellor.Ambition = 0;
        chancellor.SetAllegiance(PoliticalKeys.Country(country.Id), 100);

        var before = plot.Progress;

        simulation.SubmitOrder(new InvestigateCharacterOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            Subject = subject,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        Assert.Equal(2, plot.DiscoveryStage);
        Assert.True(plot.Progress < before);
    }

    [Fact]
    public void InvestigatingInnocentCharacter_DamagesRelationship()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var subject = country.PoliticalFigures.Single(character =>
            character.FullName == "Marta Vogel");
        var relationship = state.Relationships.GetOrCreate(subject, country.Ruler);
        var oldOpinion = relationship.Opinion;

        simulation.SubmitOrder(new InvestigateCharacterOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            Subject = subject,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        Assert.True(relationship.Opinion < oldOpinion);
        Assert.DoesNotContain(
            state.Plots,
            plot => ReferenceEquals(plot.Instigator, subject));
    }
}
