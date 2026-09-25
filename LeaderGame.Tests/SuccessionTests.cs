using LeaderGame.Simulation;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class SuccessionTests
{
    [Fact]
    public void LineageSuccessor_ContinuesPlayerControl()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var oldRuler = state.Player.Country.Ruler;
        var heir = state.Player.Lineage.Members.Single(member =>
            !ReferenceEquals(member, oldRuler));

        oldRuler.IsAlive = false;

        simulation.AdvanceMonth();

        Assert.Same(heir, state.Player.Country.Ruler);
        Assert.Same(heir, state.Player.CurrentCharacter);
        Assert.False(state.Player.HasLost);
        Assert.Null(heir.Position);
    }

    [Fact]
    public void OutsiderSuccessor_ContinuesCountryButEndsPlayerLineage()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var oldRuler = country.Ruler;
        var heir = state.Player.Lineage.Members.Single(member =>
            !ReferenceEquals(member, oldRuler));
        var outsider = country.SuccessionOrder.First(candidate =>
            !state.Player.Lineage.Contains(candidate));

        oldRuler.IsAlive = false;
        heir.IsAlive = false;

        simulation.AdvanceMonth();

        Assert.Same(outsider, country.Ruler);
        Assert.True(outsider.IsAlive);
        Assert.True(state.Player.HasLost);
        Assert.NotNull(state.Player.LossReason);
        Assert.Same(oldRuler, state.Player.CurrentCharacter);
    }

    [Fact]
    public void DeadIssuerOrder_IsCancelledWhenSuccessionOccurs()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var oldRuler = country.Ruler;
        var treasurer = country.GetOfficeHolder(Characters.Position.Treasurer)!;

        var order = new Orders.ChangeTaxOrder
        {
            Issuer = oldRuler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.14m
        };

        simulation.SubmitOrder(order);
        oldRuler.IsAlive = false;

        simulation.AdvanceMonth();

        Assert.Equal(Orders.OrderStatus.Rejected, order.Status);
        Assert.Equal(0.10m, country.TaxRate);
    }
}
