using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Reports;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class TaxOrderTests
{
    [Fact]
    public void Treasurer_ImplementsTaxOrderAccordingToCompetenceAndRelationship()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

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
        Assert.InRange(country.TaxRate, 0.13m, 0.14m);
        Assert.True(country.PublicUnrest > 20);
        Assert.True(country.Government.Stability < 67);
        Assert.Empty(state.PendingOrders);
        Assert.Contains(
            state.Reports,
            report => report.Category == ReportCategory.Order &&
                      report.Title.Contains(treasurer.FullName));
    }

    [Fact]
    public void HostileTreasurer_CanRefuseTaxOrder()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;
        var ruler = country.Ruler;

        treasurer.Ambition = 100;
        treasurer.SetAllegiance($"country:{country.Id}", 0);

        var relationship = state.Relationships.GetOrCreate(treasurer, ruler);
        relationship.Opinion = -100;
        relationship.Trust = 0;
        relationship.Fear = 0;

        var order = new ChangeTaxOrder
        {
            Issuer = ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.14m
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Refused, order.Status);
        Assert.Equal(0.10m, country.TaxRate);
    }

    [Fact]
    public void TaxOrder_RejectsOutOfRangeRate()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

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
        var marshal = country.GetOfficeHolder(Position.Marshal)!;

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
