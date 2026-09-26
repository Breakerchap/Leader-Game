using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Campaign;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class ElectionSystemTests
{
    [Fact]
    public void Valeria_StartsWithElectionOneYearAway()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);

        Assert.True(
            state.Player.Country.Government.HoldsScheduledElections);
        Assert.Equal(
            12,
            state.Player.Country.Government.MonthsUntilElection);
        Assert.Equal(
            48,
            state.Player.Country.Government.ElectionIntervalMonths);
    }

    [Fact]
    public void PartyCandidateWinningElection_KeepsPlayerInPower()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var doge = country.Ruler;

        MakeDominantCandidate(country, doge);
        country.Government.MonthsUntilElection = 1;

        new GameSimulation(state).AdvanceMonth();

        Assert.False(state.Player.HasLost);
        Assert.Same(doge, country.Ruler);
        Assert.Same(doge, state.Player.CurrentCharacter);
        Assert.Equal(1, state.Player.ElectionsWon);
        Assert.Equal(
            country.Government.ElectionIntervalMonths,
            country.Government.MonthsUntilElection);
        Assert.Contains(
            state.Reports,
            report =>
                report.Title.Contains(
                    "wins the Valeria election",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RivalWinningElection_MovesCurrentPartyIntoOpposition()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var rival = country.GetOfficeHolder(Position.Chancellor)!;

        foreach (var candidate in country.PoliticalFigures.Where(
                     character => character.IsPoliticallyActive))
        {
            MakeWeakCandidate(country, candidate);
        }

        MakeDominantCandidate(country, rival);
        country.Government.MonthsUntilElection = 1;

        new GameSimulation(state).AdvanceMonth();

        Assert.Same(rival, country.Ruler);
        Assert.False(state.Player.HasLost);
        Assert.True(state.Player.MonthsOutOfPower >= 1);
        Assert.Contains(
            state.Player.CurrentCharacter,
            state.Player.Lineage.Members);
        Assert.DoesNotContain(
            rival,
            state.Player.Lineage.Members);
        Assert.Contains(
            state.Campaign!.Objectives,
            objective =>
                objective.Type ==
                CampaignObjectiveType.RestorePoliticalControl &&
                !objective.IsCompleted);
    }

    [Fact]
    public void ForeignRepublic_CanChangeGovernmentWithoutPlayerInvolvement()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.FalkenreichId);
        var valeria = state.FindCountry("valeria")!;
        var oldDoge = valeria.Ruler;
        var rival = valeria.GetOfficeHolder(Position.Chancellor)!;

        foreach (var candidate in valeria.PoliticalFigures.Where(
                     character => character.IsPoliticallyActive))
        {
            MakeWeakCandidate(valeria, candidate);
        }

        MakeDominantCandidate(valeria, rival);
        valeria.Government.MonthsUntilElection = 1;

        new GameSimulation(state).AdvanceMonth();

        Assert.Same(rival, valeria.Ruler);
        Assert.False(state.Player.HasLost);
        Assert.NotSame(oldDoge, valeria.Ruler);
    }

    [Fact]
    public void ElectoralVictory_CompletesValeriaMandateObjective()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);
        var objective = state.Campaign!.Objectives.Single(candidate =>
            candidate.Type == CampaignObjectiveType.ElectoralMandate);

        state.Player.ElectionsWon = 1;
        state.Player.Country.Government.MonthsUntilElection = 40;

        new GameSimulation(state).AdvanceMonth();

        Assert.True(objective.IsCompleted);
    }

    private static void MakeDominantCandidate(
        Simulation.Countries.Country country,
        Character candidate)
    {
        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            candidate.SetPowerBaseStanding(powerBase, 100);

        candidate.Influence = 100;
        candidate.Competence = 100;
        candidate.Ambition = 100;
        candidate.Legitimacy = 100;
    }

    private static void MakeWeakCandidate(
        Simulation.Countries.Country country,
        Character candidate)
    {
        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            candidate.SetPowerBaseStanding(powerBase, 0);

        candidate.Influence = 0;
        candidate.Competence = 0;
        candidate.Ambition = 0;
        candidate.Legitimacy = 0;
    }
}
