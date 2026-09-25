using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PersonalDiplomacyTests
{
    [Fact]
    public void ForeignRulerHostility_LowersTradeAcceptance()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "nordmark");
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var relationship =
            state.Relationships.GetOrCreate(target.Ruler, source.Ruler);

        relationship.Opinion = 100;
        relationship.Trust = 100;
        var friendly = DiplomaticCalculations.GetTradeAcceptanceScore(
            state,
            source,
            target,
            chancellor);

        relationship.Opinion = -100;
        relationship.Trust = 0;
        var hostile = DiplomaticCalculations.GetTradeAcceptanceScore(
            state,
            source,
            target,
            chancellor);

        Assert.True(friendly > hostile + 30);
    }

    [Fact]
    public void DiplomaticMission_ImprovesForeignRulersViewOfIssuer()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var relationship =
            state.Relationships.GetOrCreate(target.Ruler, source.Ruler);
        var oldOpinion = relationship.Opinion;
        var oldTrust = relationship.Trust;

        simulation.SubmitOrder(new ImproveRelationsOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = source,
            TargetCountry = target
        });

        simulation.AdvanceMonth();

        Assert.True(relationship.Opinion > oldOpinion);
        Assert.True(relationship.Trust > oldTrust);
    }

    [Fact]
    public void SuccessionCanChangeDiplomaticOutlookWithoutResettingStateRelations()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "nordmark");
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(source, target);
        var oldStateRelations = relation.Relations;

        var oldRuler = target.Ruler;
        var oldPersonal = state.Relationships.GetOrCreate(oldRuler, source.Ruler);
        oldPersonal.Opinion = 100;
        oldPersonal.Trust = 100;

        var before = DiplomaticCalculations.GetTradeAcceptanceScore(
            state,
            source,
            target,
            chancellor);

        target.Ruler.IsAlive = false;
        var simulation = new GameSimulation(state);
        simulation.AdvanceMonth();

        var after = DiplomaticCalculations.GetTradeAcceptanceScore(
            state,
            source,
            target,
            chancellor);

        Assert.NotSame(oldRuler, target.Ruler);
        Assert.Equal(oldStateRelations, relation.Relations);
        Assert.True(after < before);
    }
}
