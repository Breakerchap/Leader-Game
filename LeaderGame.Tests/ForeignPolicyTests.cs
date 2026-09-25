using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class ForeignPolicyTests
{
    [Fact]
    public void FriendlyForeignState_CanInitiateTradeProposal()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var nordmark = state.Countries.Single(country => country.Id == "nordmark");

        simulation.AdvanceMonth();

        Assert.Contains(
            state.DiplomaticProposals,
            proposal =>
                proposal.Status == DiplomaticProposalStatus.Pending &&
                ReferenceEquals(proposal.SourceCountry, nordmark) &&
                ReferenceEquals(proposal.TargetCountry, state.Player.Country));
    }

    [Fact]
    public void PlayerCanAcceptIncomingProposalThroughChancellor()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var nordmark = state.Countries.Single(candidate => candidate.Id == "nordmark");

        simulation.AdvanceMonth();

        var proposal = state.DiplomaticProposals.Single(candidate =>
            candidate.Status == DiplomaticProposalStatus.Pending &&
            ReferenceEquals(candidate.SourceCountry, nordmark));

        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;

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

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(DiplomaticProposalStatus.Accepted, proposal.Status);
        Assert.True(relation.HasTradeAgreement);
        Assert.True(country.LastMonthlyTradeIncome > 0);
    }

    [Fact]
    public void RejectingIncomingProposalDamagesTrust()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var nordmark = state.Countries.Single(candidate => candidate.Id == "nordmark");

        simulation.AdvanceMonth();

        var proposal = state.DiplomaticProposals.Single(candidate =>
            candidate.Status == DiplomaticProposalStatus.Pending &&
            ReferenceEquals(candidate.SourceCountry, nordmark));
        var relation = state.Diplomacy.GetOrCreate(country, nordmark);
        var oldTrust = relation.Trust;
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;

        simulation.SubmitOrder(new RespondToDiplomaticProposalOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Proposal = proposal,
            Accept = false
        });

        simulation.AdvanceMonth();

        Assert.Equal(DiplomaticProposalStatus.Rejected, proposal.Status);
        Assert.False(relation.HasTradeAgreement);
        Assert.True(relation.Trust < oldTrust);
    }

    [Fact]
    public void IgnoredProposalEventuallyExpires()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);

        simulation.AdvanceMonth();

        var proposal = state.DiplomaticProposals.Single(candidate =>
            candidate.Status == DiplomaticProposalStatus.Pending);

        simulation.AdvanceMonth();
        Assert.Equal(DiplomaticProposalStatus.Pending, proposal.Status);

        simulation.AdvanceMonth();
        Assert.Equal(DiplomaticProposalStatus.Pending, proposal.Status);

        simulation.AdvanceMonth();
        Assert.Equal(DiplomaticProposalStatus.Expired, proposal.Status);
    }
}
