using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Reports;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class TaxOrderTests
{
    [Fact]
    public void Treasurer_ImplementsTaxOrderAccordingToCompetenceAndLoyalty()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetAdvisor(Position.Treasurer)!;

        var order = new ChangeTaxOrder
        {
            Issuer = state.Player.CurrentCharacter,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.14m
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(0.13684m, country.TaxRate);
        Assert.True(country.PublicUnrest > 20);
        Assert.True(country.Government.Stability < 67);
        Assert.Empty(state.PendingOrders);
        Assert.Contains(
            state.Reports,
            report => report.Category == ReportCategory.Order &&
                      report.Title.Contains(treasurer.FullName));
    }

    [Fact]
    public void TaxOrder_RejectsOutOfRangeRate()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetAdvisor(Position.Treasurer)!;

        var order = new ChangeTaxOrder
        {
            Issuer = state.Player.CurrentCharacter,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.75m
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal(0.10m, country.TaxRate);
    }

    [Fact]
    public void TaxOrder_RejectsNonTreasurerRecipient()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetAdvisor(Position.Marshal)!;

        var order = new ChangeTaxOrder
        {
            Issuer = state.Player.CurrentCharacter,
            Recipient = marshal,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.14m
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal(0.10m, country.TaxRate);
    }
}
