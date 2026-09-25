using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class AppointmentOrderTests
{
    [Fact]
    public void Appointment_ReplacesExistingOfficeHolderAndCreatesGrievance()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var oldTreasurer = country.GetOfficeHolder(Position.Treasurer)!;
        var candidate = country.AvailableAdvisors.First();

        var oldRelationship = state.Relationships.GetOrCreate(oldTreasurer, ruler);
        var oldOpinion = oldRelationship.Opinion;
        var oldTrust = oldRelationship.Trust;

        var candidateRelationship = state.Relationships.GetOrCreate(candidate, ruler);
        var candidateOpinion = candidateRelationship.Opinion;

        var order = new AppointAdvisorOrder
        {
            Issuer = ruler,
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
        Assert.Equal(oldOpinion - 25, oldRelationship.Opinion);
        Assert.Equal(oldTrust - 15, oldRelationship.Trust);
        Assert.Equal(candidateOpinion + 10, candidateRelationship.Opinion);
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
            Influence = 50
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
