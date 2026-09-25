using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PoliticalDemandSpokespersonTests
{
    [Fact]
    public void MilitaryDiscontent_SelectsMarshalAsNaturalSpokesperson()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;

        country.Ruler.SetPowerBaseStanding(
            PowerBaseType.Military,
            20);

        var simulation = new GameSimulation(state);

        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        var demand = Assert.Single(state.PowerBaseDemands.Where(demand =>
            !demand.IsResolved &&
            demand.PowerBase == PowerBaseType.Military));

        Assert.Same(marshal, demand.Spokesperson);
    }

    [Fact]
    public void SatisfyingDemand_RewardsSpokespersonRelationship()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;

        country.Ruler.SetPowerBaseStanding(
            PowerBaseType.Military,
            20);

        var simulation = new GameSimulation(state);

        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        var demand = Assert.Single(state.PowerBaseDemands.Where(demand =>
            !demand.IsResolved &&
            demand.PowerBase == PowerBaseType.Military));

        Assert.Same(marshal, demand.Spokesperson);

        var relationship = state.Relationships.GetOrCreate(
            marshal,
            country.Ruler);
        var oldOpinion = relationship.Opinion;
        var oldTrust = relationship.Trust;

        country.ArmyFunding = Math.Max(
            country.ArmyFunding,
            demand.TargetValue);

        simulation.AdvanceMonth();

        Assert.True(demand.IsResolved);
        Assert.True(relationship.Opinion > oldOpinion);
        Assert.True(relationship.Trust > oldTrust);
    }

    [Fact]
    public void IgnoredDemand_StrengthensSpokespersonAsAlternative()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;

        country.Ruler.SetPowerBaseStanding(
            PowerBaseType.Military,
            20);

        var simulation = new GameSimulation(state);

        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        var demand = Assert.Single(state.PowerBaseDemands.Where(demand =>
            !demand.IsResolved &&
            demand.PowerBase == PowerBaseType.Military));

        var oldBacking =
            marshal.GetPowerBaseStanding(PowerBaseType.Military);
        var relationship = state.Relationships.GetOrCreate(
            marshal,
            country.Ruler);
        var oldTrust = relationship.Trust;

        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        Assert.True(demand.EscalationLevel >= 1);
        Assert.True(
            marshal.GetPowerBaseStanding(PowerBaseType.Military) >
            oldBacking);
        Assert.True(relationship.Trust < oldTrust);
    }
}
