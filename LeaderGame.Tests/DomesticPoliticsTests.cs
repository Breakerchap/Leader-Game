using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class DomesticPoliticsTests
{
    [Fact]
    public void DisaffectedImportantPowerBase_CreatesConcreteDemand()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);
        country.Ruler.SetPowerBaseStanding(PowerBaseType.Military, 15);
        state.Date = new GameDate(1450, 3);

        new GameSimulation(state).AdvanceMonth();

        var demand = Assert.Single(state.PowerBaseDemands.Where(candidate =>
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Country, country)));

        Assert.Equal(PowerBaseType.Military, demand.PowerBase);
        Assert.Equal(PowerBaseDemandType.RaiseArmyFunding, demand.Type);
        Assert.True(demand.TargetValue > country.ArmyFunding);
        Assert.Contains(state.Reports, report =>
            report.Title.Contains("Military", StringComparison.OrdinalIgnoreCase) &&
            report.Title.Contains("demands", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MeetingDemand_ResolvesItAndRecoversBacking()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);
        ruler.SetPowerBaseStanding(PowerBaseType.Military, 15);
        state.Date = new GameDate(1450, 3);

        simulation.AdvanceMonth();

        var demand = state.PowerBaseDemands.Single(candidate =>
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Country, country));

        var backingBeforeConcession =
            ruler.GetPowerBaseStanding(PowerBaseType.Military);

        simulation.SubmitOrder(new SetBudgetOrder
        {
            Issuer = ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetArmyFunding = 1.5m,
            TargetAdministrationFunding = country.AdministrationFunding,
            TargetCourtFunding = country.CourtFunding
        });

        simulation.AdvanceMonth();

        Assert.True(demand.IsResolved);
        Assert.True(country.ArmyFunding >= demand.TargetValue);
        Assert.True(
            ruler.GetPowerBaseStanding(PowerBaseType.Military) >
            backingBeforeConcession);
        Assert.Contains(state.Reports, report =>
            report.Title.Contains("demand satisfied", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IgnoredDemand_EscalatesAndBenefitsPoliticalRival()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Bureaucracy, 100);
        ruler.SetPowerBaseStanding(PowerBaseType.Bureaucracy, 10);
        chancellor.SetPowerBaseStanding(PowerBaseType.Bureaucracy, 70);
        state.Date = new GameDate(1450, 3);

        simulation.AdvanceMonth();

        var demand = state.PowerBaseDemands.Single(candidate =>
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Country, country));

        var rulerBacking = ruler.GetPowerBaseStanding(PowerBaseType.Bureaucracy);
        var rival = country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, ruler))
            .OrderByDescending(character =>
                character.GetPowerBaseStanding(PowerBaseType.Bureaucracy))
            .ThenByDescending(character => character.Ambition)
            .First();
        var rivalBacking = rival.GetPowerBaseStanding(PowerBaseType.Bureaucracy);

        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        Assert.Equal(1, demand.EscalationLevel);
        Assert.False(demand.IsResolved);
        Assert.True(
            ruler.GetPowerBaseStanding(PowerBaseType.Bureaucracy) <
            rulerBacking);
        Assert.True(
            rival.GetPowerBaseStanding(PowerBaseType.Bureaucracy) >
            rivalBacking);
        Assert.Contains(state.Reports, report =>
            report.Title.Contains("pressure escalates", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ChronicUnderfunding_ErodesInstitutionalBacking()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var ruler = country.Ruler;

        state.Date = new GameDate(1450, 3);
        country.ArmyFunding = 0.5m;
        ruler.SetPowerBaseStanding(PowerBaseType.Military, 50);

        new GameSimulation(state).AdvanceMonth();

        Assert.True(
            ruler.GetPowerBaseStanding(PowerBaseType.Military) < 50);
    }

    [Fact]
    public void ContentPowerBases_DoNotManufactureDemands()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
        {
            country.SetPowerBaseStrength(powerBase, 100);
            country.Ruler.SetPowerBaseStanding(powerBase, 60);
        }

        state.Date = new GameDate(1450, 3);

        new GameSimulation(state).AdvanceMonth();

        Assert.DoesNotContain(
            state.PowerBaseDemands,
            demand => ReferenceEquals(demand.Country, country) && !demand.IsResolved);
    }
}
