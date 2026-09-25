using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class StrategicDiplomacyTests
{
    [Fact]
    public void BorderDispute_RaisesTensionWithoutPact()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var player = state.Player.Country;
        var valeria = state.Countries.Single(country => country.Id == "valeria");
        var relation = state.Diplomacy.GetOrCreate(player, valeria);

        state.Date = new GameDate(1450, 3);
        relation.Tension = 40;
        relation.BorderDisputeSeverity = 60;
        relation.HasTradeAgreement = false;
        relation.HasNonAggressionPact = false;

        simulation.AdvanceMonth();

        Assert.True(relation.Tension > 40);
    }

    [Fact]
    public void NonAggressionPact_SuppressesBorderEscalation()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var player = state.Player.Country;
        var valeria = state.Countries.Single(country => country.Id == "valeria");
        var relation = state.Diplomacy.GetOrCreate(player, valeria);

        state.Date = new GameDate(1450, 3);
        relation.Tension = 40;
        relation.BorderDisputeSeverity = 60;
        relation.HasTradeAgreement = false;
        relation.HasNonAggressionPact = true;

        simulation.AdvanceMonth();

        Assert.True(relation.Tension < 40);
        Assert.Equal(60, relation.BorderDisputeSeverity);
    }

    [Fact]
    public void FriendlyNeighbour_CanAcceptNonAggressionPact()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var nordmark = state.Countries.Single(candidate => candidate.Id == "nordmark");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(country, nordmark);

        relation.Relations = 80;
        relation.Trust = 90;
        relation.Tension = 10;

        var order = new NegotiateNonAggressionPactOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = nordmark
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(relation.HasNonAggressionPact);
        Assert.NotNull(relation.NonAggressionPactStartedOn);
    }

    [Fact]
    public void HostileNeighbour_CanRejectNonAggressionPact()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var valeria = state.Countries.Single(candidate => candidate.Id == "valeria");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(country, valeria);

        relation.Relations = -100;
        relation.Trust = 0;
        relation.Tension = 100;
        relation.BorderDisputeSeverity = 100;

        var order = new NegotiateNonAggressionPactOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = valeria
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.False(relation.HasNonAggressionPact);
    }

    [Fact]
    public void BreakingPact_DamagesTrustAndRaisesTension()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var nordmark = state.Countries.Single(candidate => candidate.Id == "nordmark");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(country, nordmark);

        relation.HasNonAggressionPact = true;
        relation.NonAggressionPactStartedOn = state.Date;

        var oldTrust = relation.Trust;
        var oldTension = relation.Tension;

        var order = new EndNonAggressionPactOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = nordmark
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.False(relation.HasNonAggressionPact);
        Assert.True(relation.Trust < oldTrust);
        Assert.True(relation.Tension > oldTension);
    }

    [Fact]
    public void AcceptedIncomingPact_BecomesActive()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var nordmark = state.Countries.Single(candidate => candidate.Id == "nordmark");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;

        var proposal = new DiplomaticProposal
        {
            Type = DiplomaticProposalType.NonAggressionPact,
            SourceCountry = nordmark,
            TargetCountry = country,
            CreatedOn = state.Date
        };
        state.DiplomaticProposals.Add(proposal);

        var order = new RespondToDiplomaticProposalOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Proposal = proposal,
            Accept = true
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        var relation = state.Diplomacy.GetOrCreate(country, nordmark);

        Assert.Equal(DiplomaticProposalStatus.Accepted, proposal.Status);
        Assert.True(relation.HasNonAggressionPact);
    }

    [Fact]
    public void DeclaringWarThroughPact_CarriesAdditionalPoliticalCost()
    {
        var breachState = DemoScenario.Create();
        var normalState = DemoScenario.Create();

        var breachSource = breachState.Player.Country;
        var normalSource = normalState.Player.Country;
        var breachTarget = breachState.Countries.Single(country => country.Id == "valeria");
        var normalTarget = normalState.Countries.Single(country => country.Id == "valeria");

        var breachRelation = breachState.Diplomacy.GetOrCreate(breachSource, breachTarget);
        var normalRelation = normalState.Diplomacy.GetOrCreate(normalSource, normalTarget);

        breachRelation.Relations = normalRelation.Relations = -40;
        breachRelation.Trust = normalRelation.Trust = 60;
        breachRelation.HasNonAggressionPact = true;
        breachRelation.NonAggressionPactStartedOn = breachState.Date;

        var breachLegitimacy = breachSource.Ruler.Legitimacy;
        var normalLegitimacy = normalSource.Ruler.Legitimacy;

        DeclareWarAndAdvance(breachState, breachSource, breachTarget);
        DeclareWarAndAdvance(normalState, normalSource, normalTarget);

        Assert.False(breachRelation.HasNonAggressionPact);
        Assert.True(
            breachLegitimacy - breachSource.Ruler.Legitimacy >
            normalLegitimacy - normalSource.Ruler.Legitimacy);
        Assert.True(breachRelation.Trust < normalRelation.Trust);
    }

    private static void DeclareWarAndAdvance(
        GameState state,
        Country source,
        Country target)
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
