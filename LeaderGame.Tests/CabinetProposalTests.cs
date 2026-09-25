using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class CabinetProposalTests
{
    [Fact]
    public void DemoScenario_ProducesConditionDrivenCabinetRecommendation()
    {
        var state = DemoScenario.Create();

        new GameSimulation(state).AdvanceMonth();

        var proposal = Assert.Single(state.CabinetProposals);

        Assert.Equal(CabinetProposalStatus.Pending, proposal.Status);
        Assert.Same(state.Player.Country, proposal.Country);
        Assert.True(proposal.Advisor.Position.HasValue);
    }

    [Fact]
    public void AcceptingTaxRecommendation_QueuesNormalTreasuryOrder()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;
        var towardRuler = state.Relationships.GetOrCreate(
            treasurer,
            country.Ruler);
        var oldOpinion = towardRuler.Opinion;
        var oldTrust = towardRuler.Trust;

        var proposal = new CabinetProposal
        {
            Country = country,
            Advisor = treasurer,
            Type = CabinetProposalType.LowerTaxes,
            TargetValue = 0.08m,
            CreatedOn = state.Date
        };
        state.CabinetProposals.Add(proposal);

        var session = new GameSession(new GameSimulation(state));
        session.RespondToCabinetProposal(proposal.Id, accept: true);

        Assert.Equal(CabinetProposalStatus.Accepted, proposal.Status);

        var order = Assert.IsType<ChangeTaxOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Same(treasurer, order.Recipient);
        Assert.Equal(0.08m, order.TargetTaxRate);
        Assert.Equal(oldOpinion + 3, towardRuler.Opinion);
        Assert.Equal(oldTrust + 2, towardRuler.Trust);
    }

    [Fact]
    public void RejectingRecommendation_CostsAdvisorTrust()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var target = state.FindCountry("valeria")!;
        var towardRuler = state.Relationships.GetOrCreate(
            chancellor,
            country.Ruler);
        var oldOpinion = towardRuler.Opinion;
        var oldTrust = towardRuler.Trust;

        var proposal = new CabinetProposal
        {
            Country = country,
            Advisor = chancellor,
            Type = CabinetProposalType.ImproveRelations,
            TargetCountry = target,
            CreatedOn = state.Date
        };
        state.CabinetProposals.Add(proposal);

        var session = new GameSession(new GameSimulation(state));
        session.RespondToCabinetProposal(proposal.Id, accept: false);

        Assert.Equal(CabinetProposalStatus.Rejected, proposal.Status);
        Assert.Empty(state.PendingOrders);
        Assert.True(towardRuler.Opinion < oldOpinion);
        Assert.Equal(oldTrust - 1, towardRuler.Trust);
    }

    [Fact]
    public void AcceptedRecommendation_AppearsAsPendingOrderInDesktopView()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        var proposal = new CabinetProposal
        {
            Country = country,
            Advisor = treasurer,
            Type = CabinetProposalType.RaiseAdministrationFunding,
            TargetValue = 1.15m,
            CreatedOn = state.Date
        };
        state.CabinetProposals.Add(proposal);

        var session = new GameSession(new GameSimulation(state));
        session.RespondToCabinetProposal(proposal.Id, accept: true);

        var pending = Assert.Single(session.View.PendingOrderDetails);
        Assert.Equal("Budget", pending.Type);
        Assert.Contains("administration", pending.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ElectionSeason_CanGeneratePoliticallyMotivatedTaxAdvice()
    {
        var state = DemoScenario.Create(
            Simulation.Scenarios.ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;

        country.Government.MonthsUntilElection = 4;
        country.TaxRate = 0.12m;
        country.LastMonthlyBalance = 50_000m;
        state.Date = new GameDate(1450, 4);

        new GameSimulation(state).AdvanceMonth();

        var proposal = Assert.Single(state.CabinetProposals.Where(candidate =>
            candidate.Status == CabinetProposalStatus.Pending));

        Assert.Equal(CabinetProposalType.LowerTaxes, proposal.Type);
        Assert.NotNull(proposal.Rationale);
        Assert.Contains(
            "election",
            proposal.Rationale!,
            StringComparison.OrdinalIgnoreCase);
    }

}
