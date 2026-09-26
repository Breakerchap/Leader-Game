using LeaderGame.Simulation;
using LeaderGame.Simulation.Campaign;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class NordmarkScenarioTests
{
    [Fact]
    public void ScenarioCatalog_OffersThreeDistinctPlayableStarts()
    {
        Assert.Equal(3, ScenarioCatalog.All.Count);

        Assert.Contains(
            ScenarioCatalog.All,
            scenario => scenario.Id ==
                ScenarioCatalog.FalkenreichId);
        Assert.Contains(
            ScenarioCatalog.All,
            scenario => scenario.Id ==
                ScenarioCatalog.NordmarkId);
        Assert.Contains(
            ScenarioCatalog.All,
            scenario => scenario.Id ==
                ScenarioCatalog.ValeriaId);
    }

    [Fact]
    public void Nordmark_StartsAsHouseAfSkeld()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.NordmarkId);

        Assert.Equal(
            "nordmark",
            state.Player.Country.Id);
        Assert.Equal(
            "Ingrid af Skeld",
            state.Player.CurrentCharacter.FullName);
        Assert.Equal(
            "House af Skeld",
            state.Player.Lineage.Name);
        Assert.True(state.Player.IsInPower);
        Assert.Contains(
            state.Player.Lineage.Members,
            member => member.FullName ==
                "Astrid af Skeld");
    }

    [Fact]
    public void NordmarkCampaign_UsesMilitaryAndRegionalGoals()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.NordmarkId);

        Assert.NotNull(state.Campaign);
        Assert.Contains(
            state.Campaign!.Objectives,
            objective => objective.Type ==
                CampaignObjectiveType.MilitaryPreparedness);
        Assert.Contains(
            state.Campaign.Objectives,
            objective => objective.Type ==
                CampaignObjectiveType.RegionalAuthority);
    }

    [Fact]
    public void Nordmark_RegionalGoalRequiresActualCentralisation()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.NordmarkId);
        var simulation =
            new GameSimulation(state);

        var objective =
            state.Campaign!.Objectives.Single(item =>
                item.Type ==
                    CampaignObjectiveType.RegionalAuthority);

        simulation.AdvanceMonth();

        Assert.False(objective.IsCompleted);
        Assert.Equal(0, objective.ProgressMonths);

        foreach (var region in state.Player.Country.Regions)
        {
            region.CrownControl = 70;
            region.Unrest = 20;
        }

        for (var month = 0;
             month < objective.RequiredMonths;
             month++)
        {
            simulation.AdvanceMonth();
        }

        Assert.True(objective.IsCompleted);
    }

    [Fact]
    public void Nordmark_MilitaryPreparednessCanBeBuiltAndHeld()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.NordmarkId);
        var simulation =
            new GameSimulation(state);
        var country =
            state.Player.Country;

        var objective =
            state.Campaign!.Objectives.Single(item =>
                item.Type ==
                    CampaignObjectiveType.MilitaryPreparedness);

        country.ArmyReadiness = 90;
        country.ArmyFunding = 1.20m;

        for (var month = 0;
             month < objective.RequiredMonths;
             month++)
        {
            simulation.AdvanceMonth();
        }

        Assert.True(objective.IsCompleted);
    }
}
