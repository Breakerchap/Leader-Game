using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PlayerContinuityTests
{
    [Fact]
    public void OutOfPowerLeaderDeath_PassesLeadershipWithinLineage()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var oldLeader = state.Player.CurrentCharacter;
        var heir = state.Player.Lineage.Members.Single(member =>
            !ReferenceEquals(member, oldLeader));
        var outsider = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        country.Ruler = outsider;
        oldLeader.IsAlive = false;

        new GameSimulation(state).AdvanceMonth();

        Assert.False(state.Player.HasLost);
        Assert.False(state.Player.IsInPower);
        Assert.Same(heir, state.Player.CurrentCharacter);
    }

    [Fact]
    public void PoliticallyMarginalLineage_LosesOnlyAfterSustainedIrrelevance()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var outsider = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        country.Ruler = outsider;
        country.Government.Type = GovernmentType.AbsoluteMonarchy;
        country.SuccessionOrder.Clear();

        foreach (var member in state.Player.Lineage.Members)
        {
            member.Status = PoliticalStatus.Exiled;
            member.Influence = 0;
            member.Legitimacy = 0;

            foreach (var powerBase in Enum.GetValues<PowerBaseType>())
                member.SetPowerBaseStanding(powerBase, 0);
        }

        var simulation = new GameSimulation(state);

        for (var month = 0; month < 17; month++)
            simulation.AdvanceMonth();

        Assert.False(state.Player.HasLost);
        Assert.Equal(17, state.Player.MonthsOutOfPower);
        Assert.Equal(
            17,
            state.Player.ConsecutiveLowViabilityMonths);

        simulation.AdvanceMonth();

        Assert.True(state.Player.HasLost);
        Assert.Contains(
            "without meaningful political support",
            state.Player.LossReason!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CredibleDynasticRouteBackToPower_PreventsIrrelevanceLoss()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var outsider = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);
        var heir = state.Player.Lineage.Members.Single(member =>
            !ReferenceEquals(member, state.Player.CurrentCharacter));

        country.Ruler = outsider;
        country.SuccessionOrder.Clear();
        country.SuccessionOrder.Add(heir);

        heir.Influence = 75;
        heir.Legitimacy = 80;

        var simulation = new GameSimulation(state);

        for (var month = 0;
             month < 24 &&
             !state.Player.HasLost &&
             !state.Player.HasWon;
             month++)
        {
            simulation.AdvanceMonth();
        }

        Assert.False(state.Player.HasLost);
    }
}
