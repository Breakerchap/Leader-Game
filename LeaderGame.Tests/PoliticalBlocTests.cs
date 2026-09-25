using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PoliticalBlocTests
{
    [Fact]
    public void TwoDisaffectedPowerBases_CanOrganiseAroundSharedRival()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);
        country.SetPowerBaseStrength(PowerBaseType.Aristocracy, 100);

        ruler.SetPowerBaseStanding(PowerBaseType.Military, 10);
        ruler.SetPowerBaseStanding(PowerBaseType.Aristocracy, 10);

        foreach (var character in country.PoliticalFigures.Where(character =>
                     !ReferenceEquals(character, ruler)))
        {
            character.SetPowerBaseStanding(PowerBaseType.Military, 20);
            character.SetPowerBaseStanding(PowerBaseType.Aristocracy, 20);
        }

        lukas.SetPowerBaseStanding(PowerBaseType.Military, 95);
        lukas.SetPowerBaseStanding(PowerBaseType.Aristocracy, 95);
        lukas.Ambition = 100;
        lukas.Influence = 90;

        state.Date = new GameDate(1450, 3);

        new GameSimulation(state).AdvanceMonth();

        var bloc = Assert.Single(state.PoliticalBlocs.Where(candidate =>
            candidate.IsActive &&
            ReferenceEquals(candidate.Country, country)));

        Assert.Same(lukas, bloc.Leader);
        Assert.Contains(PowerBaseType.Military, bloc.PowerBases);
        Assert.Contains(PowerBaseType.Aristocracy, bloc.PowerBases);
        Assert.True(bloc.Cohesion > 50);
        Assert.Contains(state.Reports, report =>
            report.Title.Contains("Opposition coalesces", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OrganisedBloc_RaisesLeadersPoliticalThreat()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        var before = PoliticalCalculations.GetThreatScore(
            state,
            country,
            lukas);

        var bloc = new PoliticalBloc
        {
            Country = country,
            Leader = lukas,
            Cohesion = 80
        };
        bloc.PowerBases.UnionWith(
            [PowerBaseType.Military, PowerBaseType.Aristocracy]);
        state.PoliticalBlocs.Add(bloc);

        var after = PoliticalCalculations.GetThreatScore(
            state,
            country,
            lukas);

        Assert.True(after > before);
    }

    [Fact]
    public void OppositionBloc_ReducesLeadersWillingnessToObeyRuler()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        var before = PoliticalCalculations.GetOrderWillingness(
            state,
            country,
            lukas,
            country.Ruler);

        var bloc = new PoliticalBloc
        {
            Country = country,
            Leader = lukas,
            Cohesion = 80
        };
        bloc.PowerBases.UnionWith(
            [PowerBaseType.Military, PowerBaseType.Aristocracy]);
        state.PoliticalBlocs.Add(bloc);

        var after = PoliticalCalculations.GetOrderWillingness(
            state,
            country,
            lukas,
            country.Ruler);

        Assert.True(after < before);
    }

    [Fact]
    public void RepairedBacking_DissolvesOppositionBloc()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);
        country.SetPowerBaseStrength(PowerBaseType.Aristocracy, 100);

        ruler.SetPowerBaseStanding(PowerBaseType.Military, 10);
        ruler.SetPowerBaseStanding(PowerBaseType.Aristocracy, 10);
        lukas.SetPowerBaseStanding(PowerBaseType.Military, 95);
        lukas.SetPowerBaseStanding(PowerBaseType.Aristocracy, 95);
        lukas.Ambition = 100;
        state.Date = new GameDate(1450, 3);

        simulation.AdvanceMonth();

        var bloc = state.PoliticalBlocs.Single(candidate =>
            candidate.IsActive &&
            ReferenceEquals(candidate.Country, country));

        ruler.SetPowerBaseStanding(PowerBaseType.Military, 80);
        ruler.SetPowerBaseStanding(PowerBaseType.Aristocracy, 80);

        simulation.AdvanceMonth();

        Assert.False(bloc.IsActive);
        Assert.Contains(state.Reports, report =>
            report.Title.Contains("bloc fractures", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DifferentPowerBases_CanMaintainSimultaneousDemands()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);
        country.SetPowerBaseStrength(PowerBaseType.Bureaucracy, 100);
        ruler.SetPowerBaseStanding(PowerBaseType.Military, 10);
        ruler.SetPowerBaseStanding(PowerBaseType.Bureaucracy, 12);
        state.Date = new GameDate(1450, 3);

        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        var active = state.PowerBaseDemands.Where(demand =>
            !demand.IsResolved &&
            ReferenceEquals(demand.Country, country)).ToList();

        Assert.Equal(2, active.Count);
        Assert.Contains(active, demand => demand.PowerBase == PowerBaseType.Military);
        Assert.Contains(active, demand => demand.PowerBase == PowerBaseType.Bureaucracy);
    }

    [Fact]
    public void CohesiveOpposition_TakesPeriodicPublicAction()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);
        country.SetPowerBaseStrength(PowerBaseType.Aristocracy, 100);

        ruler.SetPowerBaseStanding(PowerBaseType.Military, 10);
        ruler.SetPowerBaseStanding(PowerBaseType.Aristocracy, 10);
        lukas.SetPowerBaseStanding(PowerBaseType.Military, 95);
        lukas.SetPowerBaseStanding(PowerBaseType.Aristocracy, 95);

        foreach (var character in country.PoliticalFigures.Where(character =>
                     !ReferenceEquals(character, ruler) &&
                     !ReferenceEquals(character, lukas)))
        {
            character.SetPowerBaseStanding(PowerBaseType.Military, 10);
            character.SetPowerBaseStanding(PowerBaseType.Aristocracy, 10);
            state.Relationships.Set(
                character,
                lukas,
                opinion: -40,
                trust: 15,
                fear: 0);
        }

        var bloc = new PoliticalBloc
        {
            Country = country,
            Leader = lukas,
            Cohesion = 80,
            MonthsActive = 2
        };
        bloc.PowerBases.UnionWith(
            [PowerBaseType.Military, PowerBaseType.Aristocracy]);
        state.PoliticalBlocs.Add(bloc);

        var oldInfluence = lukas.Influence;
        var oldUnrest = country.PublicUnrest;

        new GameSimulation(state).AdvanceMonth();

        Assert.Contains(
            state.Reports,
            report => report.Title.Contains(
                "coordinates opposition pressure",
                StringComparison.OrdinalIgnoreCase));
        Assert.True(lukas.Influence > oldInfluence);
        Assert.True(country.PublicUnrest > oldUnrest);
    }

    [Fact]
    public void OppositionWithCabinetInsider_ObstructsStateCapacity()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var chancellor = country.GetOfficeHolder(
            Position.Chancellor)!;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Bureaucracy, 100);
        country.SetPowerBaseStrength(PowerBaseType.Aristocracy, 100);

        ruler.SetPowerBaseStanding(PowerBaseType.Bureaucracy, 10);
        ruler.SetPowerBaseStanding(PowerBaseType.Aristocracy, 10);
        chancellor.SetPowerBaseStanding(PowerBaseType.Bureaucracy, 95);
        chancellor.SetPowerBaseStanding(PowerBaseType.Aristocracy, 95);

        var bloc = new PoliticalBloc
        {
            Country = country,
            Leader = chancellor,
            Cohesion = 80,
            MonthsActive = 2
        };
        bloc.PowerBases.UnionWith(
            [PowerBaseType.Bureaucracy, PowerBaseType.Aristocracy]);
        state.PoliticalBlocs.Add(bloc);

        var oldEfficiency = country.AdministrativeEfficiency;

        new GameSimulation(state).AdvanceMonth();

        Assert.True(country.AdministrativeEfficiency < oldEfficiency);
        Assert.Contains(
            state.Reports,
            report => report.Title.Contains(
                "obstructs",
                StringComparison.OrdinalIgnoreCase));
    }

}
