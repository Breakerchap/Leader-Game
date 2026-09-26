using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class OppositionStrategyTests
{
    [Fact]
    public void OppositionLeader_CanOrganiseSupportThroughRealOrder()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var leader = state.Player.CurrentCharacter;
        var outsider = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        country.Ruler = outsider;

        var before = leader.GetPowerBaseStanding(PowerBaseType.Merchants);

        var order = new OppositionActionOrder
        {
            Issuer = leader,
            Recipient = leader,
            IssuedOn = state.Date,
            Country = country,
            ActionType = OppositionActionType.OrganiseSupport,
            TargetPowerBase = PowerBaseType.Merchants
        };

        var simulation = new GameSimulation(state);
        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(
            leader.GetPowerBaseStanding(PowerBaseType.Merchants) > before);
        Assert.False(state.Player.IsInPower);
        Assert.False(state.Player.HasLost);
    }

    [Fact]
    public void GovernmentLeader_CannotUseOppositionStrategy()
    {
        var state = DemoScenario.Create();
        var leader = state.Player.CurrentCharacter;

        var order = new OppositionActionOrder
        {
            Issuer = leader,
            Recipient = leader,
            IssuedOn = state.Date,
            Country = state.Player.Country,
            ActionType = OppositionActionType.PublicPressure,
            TargetPowerBase = PowerBaseType.Merchants
        };

        var simulation = new GameSimulation(state);
        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Rejected, order.Status);
    }

    [Fact]
    public void DesktopQueuesOnlyOneOppositionActionPerMonth()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        country.Ruler = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        var session = new GameSession(new GameSimulation(state));

        Assert.True(session.View.Court.IsPlayerInOpposition);
        Assert.True(session.View.Court.CanTakeOppositionAction);

        session.TakeOppositionAction(
            "OrganiseSupport",
            "Merchants");
        session.TakeOppositionAction(
            "PublicPressure",
            "Merchants");

        var order = Assert.IsType<OppositionActionOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Equal(
            OppositionActionType.OrganiseSupport,
            order.ActionType);
        Assert.False(session.View.Court.CanTakeOppositionAction);
        Assert.Contains(
            "already committed",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PendingOppositionAction_RoundTripsThroughSave()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var leader = state.Player.CurrentCharacter;
        country.Ruler = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        state.PendingOrders.Add(new OppositionActionOrder
        {
            Issuer = leader,
            Recipient = leader,
            IssuedOn = state.Date,
            Country = country,
            ActionType = OppositionActionType.BuildCoalition,
            TargetPowerBase = PowerBaseType.RegionalElites
        });

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var restored = Assert.IsType<OppositionActionOrder>(
            Assert.Single(loaded.PendingOrders));

        Assert.Equal(
            OppositionActionType.BuildCoalition,
            restored.ActionType);
        Assert.Equal(
            PowerBaseType.RegionalElites,
            restored.TargetPowerBase);
        Assert.Same(
            loaded.Player.CurrentCharacter,
            restored.Issuer);
    }
}
