using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class BudgetOrderTests
{
    [Fact]
    public void Treasurer_ImplementsBudgetAccordingToCompetenceAndWillingness()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        var order = new SetBudgetOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetArmyFunding = 1.5m,
            TargetAdministrationFunding = 0.5m,
            TargetCourtFunding = 0.8m
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        // The Treasurer and the fiscal machinery both mediate the ruler's
        // request. A capable administration should get close to the requested
        // settlement without making the slider an instant, exact command.
        Assert.InRange(country.ArmyFunding, 1.38m, 1.45m);
        Assert.InRange(country.AdministrationFunding, 0.55m, 0.62m);
        Assert.InRange(country.CourtFunding, 0.82m, 0.86m);
        Assert.NotEqual(1.5m, country.ArmyFunding);
    }

    [Fact]
    public void BudgetOrder_RejectsOutOfRangeTargets()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        var order = new SetBudgetOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetArmyFunding = 2m,
            TargetAdministrationFunding = 1m,
            TargetCourtFunding = 1m
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal(1m, country.ArmyFunding);
    }
}
