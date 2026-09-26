using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class InstitutionSystemTests
{
    [Fact]
    public void ValerianFiscalOrder_BecomesAssemblyProposalBeforeTakingEffect()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;
        var simulation = new GameSimulation(state);
        var originalTax = country.TaxRate;

        var order = new ChangeTaxOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.12m
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(originalTax, country.TaxRate);

        var proposal = Assert.Single(state.LegislativeProposals);
        Assert.Equal(LegislativeProposalStatus.Pending, proposal.Status);
        Assert.Equal(LegislativeProposalType.TaxRate, proposal.Type);

        simulation.AdvanceMonth();

        Assert.Equal(LegislativeProposalStatus.Passed, proposal.Status);
        Assert.Equal(0.12m, country.TaxRate);
    }

    [Fact]
    public void Assembly_CanDefeatGovernmentWithWeakInstitutionalBacking()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        foreach (var powerBase in new[]
                 {
                     PowerBaseType.Party,
                     PowerBaseType.Merchants,
                     PowerBaseType.Bureaucracy,
                     PowerBaseType.RegionalElites,
                     PowerBaseType.Workers,
                     PowerBaseType.Peasantry
                 })
        {
            ruler.SetPowerBaseStanding(powerBase, 0);
        }

        var simulation = new GameSimulation(state);
        simulation.SubmitOrder(new ChangeTaxOrder
        {
            Issuer = ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.18m
        });

        simulation.AdvanceMonth();
        var proposal = Assert.Single(state.LegislativeProposals);

        simulation.AdvanceMonth();

        Assert.Equal(LegislativeProposalStatus.Rejected, proposal.Status);
        Assert.Equal(0.09m, country.TaxRate);
        Assert.Contains(
            state.Reports,
            report => report.Title.Contains(
                "Assembly rejects",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FeudalCrown_OnlyNeedsCouncilForExtraordinaryFiscalChange()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;
        var simulation = new GameSimulation(state);

        simulation.SubmitOrder(new ChangeTaxOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.14m
        });

        simulation.AdvanceMonth();

        Assert.Empty(state.LegislativeProposals);
        Assert.True(country.TaxRate > 0.10m);

        simulation.SubmitOrder(new ChangeTaxOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.25m
        });

        var beforeExtraordinaryChange = country.TaxRate;
        simulation.AdvanceMonth();

        var proposal = Assert.Single(state.LegislativeProposals);
        Assert.Equal(LegislativeProposalStatus.Pending, proposal.Status);
        Assert.Equal(beforeExtraordinaryChange, country.TaxRate);
        Assert.Equal(LegislativeBodyType.RoyalCouncil, country.Government.LegislativeBody);
    }

    [Fact]
    public void LegislativeProposal_RoundTripsThroughSaveWithReferences()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;
        var simulation = new GameSimulation(state);

        simulation.SubmitOrder(new SetBudgetOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetArmyFunding = 1.20m,
            TargetAdministrationFunding = 1.15m,
            TargetCourtFunding = 0.90m
        });

        simulation.AdvanceMonth();

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var proposal = Assert.Single(loaded.LegislativeProposals);

        Assert.Equal(LegislativeProposalStatus.Pending, proposal.Status);
        Assert.Equal(LegislativeProposalType.Budget, proposal.Type);
        Assert.Same(loaded.Player.Country, proposal.Country);
        Assert.Same(loaded.Player.Country.Ruler, proposal.Sponsor);
        Assert.Same(
            loaded.Player.Country.GetOfficeHolder(Position.Treasurer),
            proposal.Drafter);
        Assert.Equal(
            LegislativeBodyType.Assembly,
            loaded.Player.Country.Government.LegislativeBody);
        Assert.Equal(
            75,
            loaded.Player.Country.Government.LegislativeIndependence);
    }

    [Fact]
    public void GovernmentView_ShowsInstitutionWithoutHiddenVoteScore()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        var session = new GameSession(new GameSimulation(state));

        Assert.Equal("Assembly", session.View.Government.LegislativeBody);
        Assert.Contains(
            "require Assembly approval",
            session.View.Government.LegislativeAuthority,
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(session.View.Government.Legislation);
    }
}
