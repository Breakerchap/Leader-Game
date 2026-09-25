using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class AppointmentOrderTests
{
    [Fact]
    public void Appointment_ReplacesExistingOfficeHolder()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var oldTreasurer = country.GetOfficeHolder(Position.Treasurer)!;
        var candidate = country.AvailableAdvisors.First();
        var oldLoyalty = oldTreasurer.Loyalty;
        var candidateLoyalty = candidate.Loyalty;

        var order = new AppointAdvisorOrder
        {
            Issuer = state.Player.CurrentCharacter,
            Recipient = candidate,
            IssuedOn = state.Date,
            Country = country,
            Position = Position.Treasurer
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Same(candidate, country.GetOfficeHolder(Position.Treasurer));
        Assert.Null(oldTreasurer.Position);
        Assert.Equal(oldLoyalty - 15, oldTreasurer.Loyalty);
        Assert.Equal(candidateLoyalty + 5, candidate.Loyalty);
    }

    [Fact]
    public void Appointment_RejectsOutsider()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;

        var outsider = new Character
        {
            Id = 99,
            FirstName = "Foreign",
            LastName = "Courtier",
            Age = 40,
            Competence = 80,
            Ambition = 50,
            Loyalty = 50
        };

        var order = new AppointAdvisorOrder
        {
            Issuer = state.Player.CurrentCharacter,
            Recipient = outsider,
            IssuedOn = state.Date,
            Country = country,
            Position = Position.Chancellor
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.NotSame(outsider, country.GetOfficeHolder(Position.Chancellor));
    }

    [Fact]
    public void Appointment_RejectsCharacterWhoAlreadyHoldsOffice()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;

        var order = new AppointAdvisorOrder
        {
            Issuer = state.Player.CurrentCharacter,
            Recipient = marshal,
            IssuedOn = state.Date,
            Country = country,
            Position = Position.Chancellor
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Same(marshal, country.GetOfficeHolder(Position.Marshal));
    }
}
