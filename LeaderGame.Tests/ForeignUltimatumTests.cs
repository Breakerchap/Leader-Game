using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class ForeignUltimatumTests
{
    [Fact]
    public void HostileStrongerStateCanIssueUltimatum()
    {
        var state = CreateThreatenedState();
        var simulation = new GameSimulation(state);
        var valeria = state.Countries.Single(country => country.Id == "valeria");

        simulation.AdvanceMonth();

        var proposal = state.DiplomaticProposals.Single(candidate =>
            candidate.Type == DiplomaticProposalType.TributeUltimatum &&
            ReferenceEquals(candidate.SourceCountry, valeria));

        Assert.Equal(DiplomaticProposalStatus.Pending, proposal.Status);
        Assert.True(proposal.DemandedPayment > 0);
    }

    [Fact]
    public void AcceptingUltimatumPaysDemandAndAvoidsWar()
    {
        var state = CreateThreatenedState();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var valeria = state.Countries.Single(candidate => candidate.Id == "valeria");
        var originalLegitimacy = country.Ruler.Legitimacy;

        simulation.AdvanceMonth();

        var proposal = state.DiplomaticProposals.Single(candidate =>
            candidate.Type == DiplomaticProposalType.TributeUltimatum &&
            ReferenceEquals(candidate.SourceCountry, valeria));

        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;

        simulation.SubmitOrder(new RespondToDiplomaticProposalOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Proposal = proposal,
            Accept = true
        });

        simulation.AdvanceMonth();

        Assert.Equal(DiplomaticProposalStatus.Accepted, proposal.Status);
        Assert.DoesNotContain(
            state.Wars,
            war => war.Status == WarStatus.Active &&
                   war.IsParticipant(country) &&
                   war.IsParticipant(valeria));
        Assert.True(country.Ruler.Legitimacy < originalLegitimacy);
        Assert.True(state.Diplomacy.GetOrCreate(country, valeria).Tension < 100);
    }

    [Fact]
    public void RejectingUltimatumCanEscalateDirectlyToWar()
    {
        var state = CreateThreatenedState();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var valeria = state.Countries.Single(candidate => candidate.Id == "valeria");

        simulation.AdvanceMonth();

        var proposal = state.DiplomaticProposals.Single(candidate =>
            candidate.Type == DiplomaticProposalType.TributeUltimatum &&
            ReferenceEquals(candidate.SourceCountry, valeria));

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

        var war = state.Wars.Single(candidate =>
            candidate.Status == WarStatus.Active &&
            candidate.IsParticipant(country) &&
            candidate.IsParticipant(valeria));

        Assert.Equal(DiplomaticProposalStatus.EscalatedToWar, proposal.Status);
        Assert.Same(valeria, war.Attacker);
        Assert.Equal(0, war.MonthsActive);
    }

    [Fact]
    public void IgnoringUltimatumCanAlsoEscalateToWar()
    {
        var state = CreateThreatenedState();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var valeria = state.Countries.Single(candidate => candidate.Id == "valeria");

        simulation.AdvanceMonth();

        var proposal = state.DiplomaticProposals.Single(candidate =>
            candidate.Type == DiplomaticProposalType.TributeUltimatum &&
            ReferenceEquals(candidate.SourceCountry, valeria));

        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        Assert.Equal(DiplomaticProposalStatus.EscalatedToWar, proposal.Status);
        Assert.Contains(
            state.Wars,
            war => war.Status == WarStatus.Active &&
                   ReferenceEquals(war.Attacker, valeria) &&
                   ReferenceEquals(war.Defender, country));
    }

    private static GameState CreateThreatenedState()
    {
        var state = DemoScenario.Create();
        state.Random = new ConstantRandom(0.5);

        var country = state.Player.Country;
        var valeria = state.Countries.Single(candidate => candidate.Id == "valeria");
        var relation = state.Diplomacy.GetOrCreate(country, valeria);

        relation.Relations = -70;
        relation.Trust = 15;
        relation.Tension = 90;

        valeria.ArmySize = 14_000;
        valeria.ArmyReadiness = 90;
        valeria.Ruler.Ambition = 90;

        return state;
    }

    private sealed class ConstantRandom : IRandomSource
    {
        private readonly double _value;

        public ConstantRandom(double value) => _value = value;

        public double NextDouble() => _value;
    }
}
