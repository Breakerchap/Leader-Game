using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class WarDeclarationTests
{
    [Fact]
    public void DeclarationCreatesWarAndDestroysNormalDiplomacy()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(source, target);

        relation.HasTradeAgreement = true;
        relation.TradeAgreementStartedOn = state.Date;

        var order = new DeclareWarOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = source,
            TargetCountry = target
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        var war = Assert.Single(state.Wars);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(WarStatus.Active, war.Status);
        Assert.Same(source, war.Attacker);
        Assert.Same(target, war.Defender);
        Assert.False(relation.HasTradeAgreement);
        Assert.True(relation.Relations <= -70);
        Assert.Equal(100, relation.Tension);
    }

    [Fact]
    public void FriendlyAggressiveWarHasHigherDomesticPoliticalCost()
    {
        var friendlyState = DemoScenario.Create();
        var hostileState = DemoScenario.Create();

        var friendlySource = friendlyState.Player.Country;
        var friendlyTarget = friendlyState.Countries.Single(country => country.Id == "nordmark");
        var hostileSource = hostileState.Player.Country;
        var hostileTarget = hostileState.Countries.Single(country => country.Id == "valeria");

        var friendlyLegitimacy = friendlySource.Ruler.Legitimacy;
        var hostileLegitimacy = hostileSource.Ruler.Legitimacy;

        DeclareAndAdvance(friendlyState, friendlySource, friendlyTarget);
        DeclareAndAdvance(hostileState, hostileSource, hostileTarget);

        var friendlyLoss = friendlyLegitimacy - friendlySource.Ruler.Legitimacy;
        var hostileLoss = hostileLegitimacy - hostileSource.Ruler.Legitimacy;

        Assert.True(friendlyLoss > hostileLoss);
        Assert.True(friendlySource.PublicUnrest > hostileSource.PublicUnrest);
    }

    [Fact]
    public void HostileChancellorCanRefuseWarDeclaration()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;

        chancellor.Ambition = 100;
        chancellor.SetAllegiance(PoliticalKeys.Country(source.Id), 0);

        var relationship = state.Relationships.GetOrCreate(chancellor, source.Ruler);
        relationship.Opinion = -100;
        relationship.Trust = 0;
        relationship.Fear = 0;

        var order = new DeclareWarOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = source,
            TargetCountry = target
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Refused, order.Status);
        Assert.Empty(state.Wars);
    }

    [Fact]
    public void DuplicateDeclarationIsRejected()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");

        DeclareAndAdvance(state, source, target);

        var simulation = new GameSimulation(state);
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var duplicate = new DeclareWarOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = source,
            TargetCountry = target
        };

        simulation.SubmitOrder(duplicate);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Rejected, duplicate.Status);
        Assert.Single(state.Wars);
    }

    private static void DeclareAndAdvance(
        GameState state,
        Countries.Country source,
        Countries.Country target)
    {
        var simulation = new GameSimulation(state);
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;

        simulation.SubmitOrder(new DeclareWarOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = source,
            TargetCountry = target
        });

        simulation.AdvanceMonth();
    }
}
