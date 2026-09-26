using LeaderGame.Simulation;
using LeaderGame.Simulation.Campaign;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class CampaignAndDynamicsTests
{
    [Fact]
    public void ScenarioCatalog_ProvidesDistinctPlayableStarts()
    {
        Assert.True(ScenarioCatalog.All.Count >= 2);

        var falkenreich = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);
        var valeria = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);

        Assert.Equal("falkenreich", falkenreich.Player.Country.Id);
        Assert.Equal("valeria", valeria.Player.Country.Id);
        Assert.NotEqual(
            falkenreich.Player.CurrentCharacter.Id,
            valeria.Player.CurrentCharacter.Id);
        Assert.NotEqual(
            falkenreich.Player.Lineage.Type,
            valeria.Player.Lineage.Type);
        Assert.NotEqual(
            falkenreich.Campaign!.ScenarioId,
            valeria.Campaign!.ScenarioId);
        Assert.NotEqual(
            falkenreich.Campaign.Objectives.Select(objective => objective.Id),
            valeria.Campaign.Objectives.Select(objective => objective.Id));
    }

    [Fact]
    public void CampaignObjective_RequiresConditionToRemainSatisfied()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);
        var simulation = new GameSimulation(state);
        var objective = state.Campaign!.Objectives.Single(candidate =>
            candidate.Id == "realm-at-peace");

        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        Assert.Equal(2, objective.ProgressMonths);
        Assert.False(objective.IsCompleted);

        state.Player.Country.PublicUnrest = 80;
        simulation.AdvanceMonth();

        Assert.Equal(0, objective.ProgressMonths);
        Assert.False(objective.IsCompleted);
    }

    [Fact]
    public void CompletingAllObjectives_ProducesActualCampaignVictory()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);

        state.Campaign!.Objectives.Clear();
        state.Campaign.Objectives.Add(new CampaignObjective
        {
            Id = "test-victory",
            Title = "Test victory",
            Description = "A deliberately immediate test objective.",
            Type = CampaignObjectiveType.TradeNetwork,
            TargetValue = 0,
            RequiredMonths = 1
        });

        new GameSimulation(state).AdvanceMonth();

        Assert.True(state.Player.HasWon);
        Assert.NotNull(state.Player.WinReason);
        Assert.True(state.Campaign.Objectives[0].IsCompleted);
        Assert.Contains(
            state.Reports,
            report => report.Title == "Campaign victory");
    }

    [Fact]
    public void LosingPower_AddsRestorationObjectiveAndBlocksVictory()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);
        var country = state.Player.Country;
        var outsider = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        state.Campaign!.Objectives.Clear();
        state.Campaign.Objectives.Add(new CampaignObjective
        {
            Id = "test-victory",
            Title = "Test victory",
            Description = "A deliberately immediate test objective.",
            Type = CampaignObjectiveType.TradeNetwork,
            TargetValue = 0,
            RequiredMonths = 1
        });

        country.Ruler = outsider;
        new GameSimulation(state).AdvanceMonth();

        Assert.False(state.Player.HasWon);
        Assert.False(state.Player.HasLost);
        Assert.Contains(
            state.Campaign.Objectives,
            objective =>
                objective.Type ==
                CampaignObjectiveType.RestorePoliticalControl &&
                !objective.IsCompleted);
    }

    [Fact]
    public void RegainingPower_AddsConsolidationObjective()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);
        var country = state.Player.Country;
        var originalRuler = state.Player.CurrentCharacter;
        var outsider = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        state.Campaign!.Objectives.Clear();
        country.Ruler = outsider;

        var simulation = new GameSimulation(state);
        simulation.AdvanceMonth();

        country.Ruler = originalRuler;
        simulation.AdvanceMonth();

        Assert.True(state.Player.IsInPower);
        Assert.Contains(
            state.Campaign.Objectives,
            objective =>
                objective.Type ==
                    CampaignObjectiveType.RestorePoliticalControl &&
                objective.IsCompleted);
        Assert.Contains(
            state.Campaign.Objectives,
            objective =>
                objective.Type ==
                    CampaignObjectiveType.ConsolidateRestoration &&
                !objective.IsCompleted);
        Assert.False(state.Player.HasWon);
    }

    [Fact]
    public void WorldDynamics_ChangesEconomyAndPopulationOverTime()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var initialGdp = country.Gdp;
        var initialPopulation = country.Population;

        new GameSimulation(state).AdvanceMonth();

        Assert.NotEqual(initialGdp, country.Gdp);
        Assert.NotEqual(initialPopulation, country.Population);
    }

    [Fact]
    public void SustainedMilitaryFunding_GrowsMilitaryStructuralPower()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        country.ArmyFunding = 1.30m;

        var initialStrength =
            country.GetPowerBaseStrength(PowerBaseType.Military);

        var simulation = new GameSimulation(state);
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        Assert.True(
            country.GetPowerBaseStrength(PowerBaseType.Military) >
            initialStrength);
    }

    [Fact]
    public void ForeignGovernment_AdaptsTaxesUnderHeavyDebt()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);
        var valeria = state.FindCountry("valeria")!;
        valeria.Debt = valeria.Gdp * 0.30m;
        var initialTaxRate = valeria.TaxRate;

        var simulation = new GameSimulation(state);
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        Assert.True(valeria.TaxRate > initialTaxRate);
    }

    [Fact]
    public void HostileAiRivals_CanStartWarWithoutPlayerInvolvement()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);
        var nordmark = state.FindCountry("nordmark")!;
        var valeria = state.FindCountry("valeria")!;
        var relation = state.Diplomacy.GetOrCreate(
            nordmark,
            valeria);

        relation.Relations = -80;
        relation.Trust = 10;
        relation.Tension = 90;

        nordmark.Ruler.Ambition = 85;
        nordmark.ArmySize = 18_000;
        nordmark.ArmyReadiness = 85;

        var simulation = new GameSimulation(state);

        for (var month = 0; month < 6; month++)
            simulation.AdvanceMonth();

        Assert.Contains(
            state.Wars,
            war =>
                war.Status == Simulation.Military.WarStatus.Active &&
                war.IsParticipant(nordmark) &&
                war.IsParticipant(valeria));
    }

    [Fact]
    public void AiCountries_CanDevelopTradeWithoutPlayerIntervention()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);
        var nordmark = state.FindCountry("nordmark")!;
        var valeria = state.FindCountry("valeria")!;
        var relation = state.Diplomacy.GetOrCreate(
            nordmark,
            valeria);

        relation.Relations = 35;
        relation.Trust = 60;
        relation.Tension = 10;
        relation.HasTradeAgreement = false;

        var simulation = new GameSimulation(state);

        for (var month = 0; month < 6; month++)
            simulation.AdvanceMonth();

        Assert.True(relation.HasTradeAgreement);
        Assert.NotNull(relation.TradeAgreementStartedOn);
    }
}
