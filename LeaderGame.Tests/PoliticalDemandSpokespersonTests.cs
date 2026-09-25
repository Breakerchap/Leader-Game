using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
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

    [Fact]
    public void ConcedingDemand_QueuesRealOrderButDoesNotInstantlySatisfyIt()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;

        var demand = new PowerBaseDemand
        {
            Country = country,
            PowerBase = PowerBaseType.Military,
            Type = PowerBaseDemandType.RaiseArmyFunding,
            TargetValue = 1.30m,
            Spokesperson = marshal
        };
        state.PowerBaseDemands.Add(demand);

        var session = new GameSession(new GameSimulation(state));
        session.RespondToPowerBaseDemand(demand.Id, concede: true);

        Assert.True(demand.AcknowledgedByRuler);
        Assert.False(demand.IsResolved);

        var order = Assert.IsType<SetBudgetOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Equal(1.30m, order.TargetArmyFunding);
        Assert.Contains(
            "remains active",
            state.Reports.Last().Details,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectingDemand_ClosesPetitionButCreatesPoliticalBacklash()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var oldStanding =
            country.Ruler.GetPowerBaseStanding(PowerBaseType.Military);
        var oldStability = country.Government.Stability;
        var oldUnrest = country.PublicUnrest;
        var relationship = state.Relationships.GetOrCreate(
            marshal,
            country.Ruler);
        var oldTrust = relationship.Trust;

        var demand = new PowerBaseDemand
        {
            Country = country,
            PowerBase = PowerBaseType.Military,
            Type = PowerBaseDemandType.RaiseArmyFunding,
            TargetValue = 1.30m,
            Spokesperson = marshal,
            EscalationLevel = 1
        };
        state.PowerBaseDemands.Add(demand);

        var session = new GameSession(new GameSimulation(state));
        session.RespondToPowerBaseDemand(demand.Id, concede: false);

        Assert.True(demand.IsResolved);
        Assert.True(demand.IsRejected);
        Assert.Equal(state.Date, demand.ResolvedOn);
        Assert.True(
            country.Ruler.GetPowerBaseStanding(PowerBaseType.Military) <
            oldStanding);
        Assert.True(country.Government.Stability < oldStability);
        Assert.True(country.PublicUnrest > oldUnrest);
        Assert.True(relationship.Trust < oldTrust);
        Assert.Empty(state.PendingOrders);
    }

    [Fact]
    public void RejectedDemand_DoesNotImmediatelyReappear()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
        {
            country.Ruler.SetPowerBaseStanding(powerBase, 80);
        }

        country.Ruler.SetPowerBaseStanding(
            PowerBaseType.Military,
            20);

        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var demand = new PowerBaseDemand
        {
            Country = country,
            PowerBase = PowerBaseType.Military,
            Type = PowerBaseDemandType.RaiseArmyFunding,
            TargetValue = 1.30m,
            Spokesperson = marshal
        };
        state.PowerBaseDemands.Add(demand);

        var session = new GameSession(new GameSimulation(state));
        session.RespondToPowerBaseDemand(demand.Id, concede: false);

        session.AdvanceMonth();
        session.AdvanceMonth();
        session.AdvanceMonth();

        Assert.DoesNotContain(
            state.PowerBaseDemands,
            candidate =>
                !candidate.IsResolved &&
                candidate.PowerBase == PowerBaseType.Military);
    }

}
