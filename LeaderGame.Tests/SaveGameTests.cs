using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class SaveGameTests
{
    [Fact]
    public void SaveRoundTrip_PreservesCoreWorldAndPlayerKnowledge()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var treasurer = country.ActiveAdvisors.Single(advisor =>
            advisor.Position == Position.Treasurer);

        simulation.SubmitOrder(new ChangeTaxOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.14m
        });

        simulation.AdvanceMonth();

        var json = GameSaveService.Serialize(state);
        var loaded = GameSaveService.Deserialize(json);

        Assert.Equal(state.Date, loaded.Date);
        Assert.Equal(state.Player.Country.Id, loaded.Player.Country.Id);
        Assert.Equal(
            state.Player.CurrentCharacter.Id,
            loaded.Player.CurrentCharacter.Id);
        Assert.Equal(
            state.Player.Lineage.Name,
            loaded.Player.Lineage.Name);

        var originalCountry = state.Player.Country;
        var loadedCountry = loaded.Player.Country;

        Assert.Equal(originalCountry.Treasury, loadedCountry.Treasury);
        Assert.Equal(originalCountry.Debt, loadedCountry.Debt);
        Assert.Equal(originalCountry.TaxRate, loadedCountry.TaxRate);
        Assert.Equal(
            originalCountry.Government.Stability,
            loadedCountry.Government.Stability);

        Assert.Equal(
            state.AdvisorReports.Count,
            loaded.AdvisorReports.Count);
        Assert.Equal(
            state.InformationHistory.Count,
            loaded.InformationHistory.Count);
        Assert.Equal(
            state.Knowledge.Latest.Count,
            loaded.Knowledge.Latest.Count);

        var treasury = state.Knowledge.Get(
            InformationMetric.Treasury,
            originalCountry.Id);
        var loadedTreasury = loaded.Knowledge.Get(
            InformationMetric.Treasury,
            loadedCountry.Id);

        Assert.NotNull(treasury);
        Assert.NotNull(loadedTreasury);
        Assert.Equal(treasury!.Estimate, loadedTreasury!.Estimate);
        Assert.Equal(treasury.AsOf, loadedTreasury.AsOf);
        Assert.Equal(
            treasury.SourceAdvisorId,
            loadedTreasury.SourceAdvisorId);
    }

    [Fact]
    public void SaveRoundTrip_PreservesPendingOrdersAndObjectReferences()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var treasurer = country.ActiveAdvisors.Single(advisor =>
            advisor.Position == Position.Treasurer);

        state.PendingOrders.Add(new SetBudgetOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetArmyFunding = 1.25m,
            TargetAdministrationFunding = 0.90m,
            TargetCourtFunding = 1.10m
        });

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var order = Assert.IsType<SetBudgetOrder>(
            Assert.Single(loaded.PendingOrders));

        Assert.Same(loaded.Player.Country, order.Country);
        Assert.Same(
            loaded.Player.Country.Ruler,
            order.Issuer);
        Assert.Contains(
            order.Recipient,
            loaded.Player.Country.PoliticalFigures);
        Assert.Equal(1.25m, order.TargetArmyFunding);
    }

    [Fact]
    public void SaveRoundTrip_PreservesFutureRandomSequence()
    {
        var state = DemoScenario.Create();

        for (var i = 0; i < 7; i++)
            state.Random.NextDouble();

        for (var i = 0; i < 11; i++)
            state.InformationRandom.NextDouble();

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(
                state.Random.NextDouble(),
                loaded.Random.NextDouble());

            Assert.Equal(
                state.InformationRandom.NextDouble(),
                loaded.InformationRandom.NextDouble());
        }
    }

    [Fact]
    public void SaveRoundTrip_PreservesPendingCabinetRecommendationReferences()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var target = state.FindCountry("valeria")!;

        state.CabinetProposals.Add(new CabinetProposal
        {
            Country = country,
            Advisor = chancellor,
            Type = CabinetProposalType.ImproveRelations,
            TargetCountry = target,
            CreatedOn = state.Date,
            MonthsOpen = 2
        });

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var proposal = Assert.Single(loaded.CabinetProposals);

        Assert.Equal(CabinetProposalStatus.Pending, proposal.Status);
        Assert.Equal(2, proposal.MonthsOpen);
        Assert.Same(loaded.Player.Country, proposal.Country);
        Assert.Same(
            loaded.Player.Country.GetOfficeHolder(Position.Chancellor),
            proposal.Advisor);
        Assert.Same(loaded.FindCountry("valeria"), proposal.TargetCountry);
    }

    [Fact]
    public void SaveToFile_WritesAndReloadsAtomicSlot()
    {
        var state = DemoScenario.Create();
        var directory = Path.Combine(
            Path.GetTempPath(),
            "leader-game-tests",
            Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "campaign.json");

        try
        {
            GameSaveService.SaveToFile(state, path);

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"));

            var loaded = GameSaveService.LoadFromFile(path);
            Assert.Equal(state.Date, loaded.Date);
            Assert.Equal(
                state.Player.Country.Id,
                loaded.Player.Country.Id);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
