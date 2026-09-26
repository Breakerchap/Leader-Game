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
            Rationale = "Election-season diplomatic positioning.",
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
        Assert.Equal(
            "Election-season diplomatic positioning.",
            proposal.Rationale);
    }

    [Fact]
    public void SaveRoundTrip_PreservesSelectedScenarioAndObjectiveProgress()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);

        var objective = state.Campaign!.Objectives.First();
        objective.ProgressMonths = 2;
        objective.IsCompleted = true;
        objective.CompletedOn = state.Date;
        state.Player.HasWon = true;
        state.Player.WinReason = "Test victory state.";

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        Assert.Equal("valeria", loaded.Player.Country.Id);
        Assert.Equal(
            ScenarioCatalog.ValeriaId,
            loaded.Campaign!.ScenarioId);
        Assert.Equal(
            state.Player.Lineage.Name,
            loaded.Player.Lineage.Name);
        Assert.Equal(2, loaded.Campaign.Objectives[0].ProgressMonths);
        Assert.True(loaded.Campaign.Objectives[0].IsCompleted);
        Assert.Equal(state.Date, loaded.Campaign.Objectives[0].CompletedOn);
        Assert.True(loaded.Player.HasWon);
        Assert.Equal("Test victory state.", loaded.Player.WinReason);
    }

    [Fact]
    public void SaveRoundTrip_PreservesPoliticalDemandDecisionState()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var spokesperson = country.GetOfficeHolder(Position.Marshal)!;

        state.PowerBaseDemands.Add(new PowerBaseDemand
        {
            Country = country,
            PowerBase = PowerBaseType.Military,
            Type = PowerBaseDemandType.RaiseArmyFunding,
            TargetValue = 1.30m,
            Spokesperson = spokesperson,
            MonthsOpen = 4,
            EscalationLevel = 1,
            AcknowledgedByRuler = true,
            IsRejected = true,
            ResolvedOn = state.Date,
            IsResolved = true
        });

        var original = Assert.Single(state.PowerBaseDemands);

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var restored = Assert.Single(loaded.PowerBaseDemands);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.PowerBase, restored.PowerBase);
        Assert.True(restored.AcknowledgedByRuler);
        Assert.True(restored.IsRejected);
        Assert.True(restored.IsResolved);
        Assert.Equal(state.Date, restored.ResolvedOn);
        Assert.Equal(
            spokesperson.Id,
            restored.Spokesperson!.Id);
    }

    [Fact]
    public void SaveRoundTrip_PreservesRepublicanElectionClock()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);

        state.Player.Country.Government.MonthsUntilElection = 5;
        state.Player.ElectionsWon = 2;
        state.Player.MonthsOutOfPower = 7;
        state.Player.ConsecutiveLowViabilityMonths = 3;
        state.Player.PoliticalViability = 41.5;

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        Assert.True(
            loaded.Player.Country.Government.HoldsScheduledElections);
        Assert.Equal(
            48,
            loaded.Player.Country.Government.ElectionIntervalMonths);
        Assert.Equal(
            5,
            loaded.Player.Country.Government.MonthsUntilElection);
        Assert.Equal(
            6,
            loaded.Player.Country.Government.ElectionCampaignMonths);
        Assert.Equal(
            ElectionMethod.CouncilElection,
            loaded.Player.Country.Government.ElectionMethod);
        Assert.Equal(2, loaded.Player.ElectionsWon);
        Assert.Equal(7, loaded.Player.MonthsOutOfPower);
        Assert.Equal(3, loaded.Player.ConsecutiveLowViabilityMonths);
        Assert.Equal(41.5, loaded.Player.PoliticalViability);
    }

    [Fact]
    public void SaveRoundTrip_PreservesOutstandingElectionPromise()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;

        var promise = new ElectionPromise
        {
            Country = country,
            Candidate = country.Ruler,
            Type = ElectionPromiseType.AdministrativeInvestment,
            TargetValue = 1.25m,
            MadeOn = state.Date,
            Status = ElectionPromiseStatus.AwaitingFulfilment,
            MonthsSinceElection = 3
        };

        state.ElectionPromises.Add(promise);

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var restored = Assert.Single(loaded.ElectionPromises);

        Assert.Equal(promise.Id, restored.Id);
        Assert.Equal(country.Id, restored.Country.Id);
        Assert.Equal(country.Ruler.Id, restored.Candidate.Id);
        Assert.Equal(promise.Type, restored.Type);
        Assert.Equal(1.25m, restored.TargetValue);
        Assert.Equal(
            ElectionPromiseStatus.AwaitingFulfilment,
            restored.Status);
        Assert.Equal(3, restored.MonthsSinceElection);
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
