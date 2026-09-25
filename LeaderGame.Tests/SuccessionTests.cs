using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
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
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        var order = new ChangeTaxOrder
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

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal(0.10m, country.TaxRate);
    }

    [Fact]
    public void RepublicanRulerDeath_ForcesEarlyElectionForInterimSuccessor()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var oldRuler = country.Ruler;
        var successor = country.SuccessionOrder.First();

        oldRuler.IsAlive = false;
        oldRuler.Health = 0;

        new GameSimulation(state).AdvanceMonth();

        Assert.Same(successor, country.Ruler);
        Assert.Same(successor, state.Player.CurrentCharacter);
        Assert.InRange(
            country.Government.MonthsUntilElection,
            1,
            2);
        Assert.False(state.Player.HasLost);
        Assert.Contains(
            state.Reports,
            report => report.Title.Contains(
                "interim",
                StringComparison.OrdinalIgnoreCase));
    }

}
