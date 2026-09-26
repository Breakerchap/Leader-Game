using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class AdministrativeReformTests
{
    [Fact]
    public void AuditAccounts_ImprovesIntegrityButAddsWork()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var office = country.GetAdministrativeOffice(
            AdministrativeFunction.Revenue)!;
        var treasurer = country.GetOfficeHolder(
            Position.Treasurer)!;

        var integrity = office.Integrity;
        var workload = office.Workload;

        var order = new AdministrativeReformOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            Function = AdministrativeFunction.Revenue,
            ReformType = AdministrativeReformType.AuditAccounts
        };

        var simulation = new GameSimulation(state);
        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(office.Integrity > integrity);
        Assert.True(office.Workload > workload);
    }

    [Fact]
    public void DelegatingLocally_RelievesCentreButDeepensPatronage()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var office = country.GetAdministrativeOffice(
            AdministrativeFunction.LocalGovernment)!;
        var chancellor = country.GetOfficeHolder(
            Position.Chancellor)!;

        office.Workload = 80;
        var patronage = office.PatronageDependence;

        var order = new AdministrativeReformOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Function = AdministrativeFunction.LocalGovernment,
            ReformType = AdministrativeReformType.DelegateToNotables
        };

        var simulation = new GameSimulation(state);
        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(office.PatronageDependence > patronage);
        Assert.True(office.Workload < 80);
    }

    [Fact]
    public void GameSession_QueuesOnlyOneMajorAdministrativeInitiativePerMonth()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));

        session.TakeAdministrativeAction(
            "AuditAccounts",
            "Royal Exchequer");
        session.TakeAdministrativeAction(
            "StrengthenClerks",
            "Royal Chancery");

        var order = Assert.IsType<AdministrativeReformOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Equal(
            AdministrativeReformType.AuditAccounts,
            order.ReformType);
        Assert.False(
            session.View.Government.CanDirectAdministration);
        Assert.Contains(
            "already",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PendingAdministrativeReform_RoundTripsThroughSave()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var office = country.GetAdministrativeOffice(
            AdministrativeFunction.Chancery)!;
        var chancellor = country.GetOfficeHolder(
            Position.Chancellor)!;

        state.PendingOrders.Add(new AdministrativeReformOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Function = office.Function,
            ReformType = AdministrativeReformType.ExtendCommissions
        });

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var restored = Assert.IsType<AdministrativeReformOrder>(
            Assert.Single(loaded.PendingOrders));

        Assert.Equal(
            AdministrativeFunction.Chancery,
            restored.Function);
        Assert.Equal(
            AdministrativeReformType.ExtendCommissions,
            restored.ReformType);
        Assert.Same(
            loaded.Player.Country,
            restored.Country);
        Assert.Same(
            loaded.Player.Country.GetOfficeHolder(
                Position.Chancellor),
            restored.Recipient);
    }
}
