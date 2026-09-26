using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class ElectionPromiseTests
{
    [Fact]
    public void PlayerCanMakeOnePromiseDuringCampaign_AndItChangesBacking()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        country.Government.MonthsUntilElection = 4;

        var nominee = state.Player.Lineage.Members
            .Where(character => character.IsPoliticallyActive)
            .OrderByDescending(character => character.Influence)
            .First();

        var oldMerchantBacking =
            nominee.GetPowerBaseStanding(PowerBaseType.Merchants);

        var report = Systems.ElectionSystem.MakePlayerCampaignPromise(
            state,
            ElectionPromiseType.TaxRelief);

        var promise = Assert.Single(state.ElectionPromises);

        Assert.Equal(nominee.Id, promise.Candidate.Id);
        Assert.Equal(ElectionPromiseStatus.Campaigning, promise.Status);
        Assert.Equal(
            Math.Max(0.02m, country.TaxRate - 0.02m),
            promise.TargetValue);
        Assert.True(
            nominee.GetPowerBaseStanding(PowerBaseType.Merchants) >
            oldMerchantBacking);
        Assert.Contains(
            "promise",
            report.Title,
            StringComparison.OrdinalIgnoreCase);

        var second = Systems.ElectionSystem.MakePlayerCampaignPromise(
            state,
            ElectionPromiseType.MilitaryInvestment);

        Assert.Single(state.ElectionPromises);
        Assert.Contains(
            "already",
            second.Title,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WinningCandidate_CarriesPromiseIntoGovernment()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var nominee = country.Ruler;

        MakeDominantCandidate(country, nominee);
        country.Government.MonthsUntilElection = 1;

        Systems.ElectionSystem.MakePlayerCampaignPromise(
            state,
            ElectionPromiseType.AdministrativeInvestment);

        new GameSimulation(state).AdvanceMonth();

        var promise = Assert.Single(state.ElectionPromises);

        Assert.False(state.Player.HasLost);
        Assert.Same(nominee, country.Ruler);
        Assert.Equal(
            ElectionPromiseStatus.AwaitingFulfilment,
            promise.Status);
        Assert.Equal(0, promise.MonthsSinceElection);
    }

    [Fact]
    public void DeliveringPromiseAfterElection_BuildsCredibility()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var nominee = country.Ruler;

        MakeDominantCandidate(country, nominee);
        country.Government.MonthsUntilElection = 1;

        Systems.ElectionSystem.MakePlayerCampaignPromise(
            state,
            ElectionPromiseType.MilitaryInvestment);

        new GameSimulation(state).AdvanceMonth();

        var promise = Assert.Single(state.ElectionPromises);
        var oldLegitimacy = nominee.Legitimacy;
        var oldMilitaryBacking =
            nominee.GetPowerBaseStanding(PowerBaseType.Military);

        country.ArmyFunding = promise.TargetValue;

        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(
            ElectionPromiseStatus.Fulfilled,
            promise.Status);
        Assert.True(nominee.Legitimacy > oldLegitimacy);
        Assert.True(
            nominee.GetPowerBaseStanding(PowerBaseType.Military) >
            oldMilitaryBacking);
        Assert.Contains(
            state.Reports,
            report => report.Title.Contains(
                "fulfils an election promise",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BreakingPromiseAfterSixMonths_CreatesBacklash()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;
        var nominee = country.Ruler;

        MakeDominantCandidate(country, nominee);
        country.Government.MonthsUntilElection = 1;

        Systems.ElectionSystem.MakePlayerCampaignPromise(
            state,
            ElectionPromiseType.CoalitionPatronage);

        new GameSimulation(state).AdvanceMonth();

        var promise = Assert.Single(state.ElectionPromises);
        var oldLegitimacy = nominee.Legitimacy;
        var oldUnrest = country.PublicUnrest;
        var oldPartyBacking =
            nominee.GetPowerBaseStanding(PowerBaseType.Party);

        for (var month = 0; month < 6; month++)
            new GameSimulation(state).AdvanceMonth();

        Assert.Equal(
            ElectionPromiseStatus.Broken,
            promise.Status);
        Assert.True(nominee.Legitimacy < oldLegitimacy);
        Assert.True(country.PublicUnrest > oldUnrest);
        Assert.True(
            nominee.GetPowerBaseStanding(PowerBaseType.Party) <
            oldPartyBacking);
        Assert.Contains(
            state.Reports,
            report => report.Title.Contains(
                "breaks an election promise",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AiRepublic_MakesContextSensitiveCampaignPromise()
    {
        var state = DemoScenario.Create(ScenarioCatalog.FalkenreichId);
        var valeria = state.FindCountry("valeria")!;

        valeria.Government.MonthsUntilElection = 6;
        valeria.PublicUnrest = 70;
        valeria.TaxRate = 0.12m;

        new GameSimulation(state).AdvanceMonth();

        var promise = Assert.Single(state.ElectionPromises.Where(promise =>
            ReferenceEquals(promise.Country, valeria)));

        Assert.Equal(ElectionPromiseType.TaxRelief, promise.Type);
        Assert.Equal(ElectionPromiseStatus.Campaigning, promise.Status);
    }

    [Fact]
    public void GameSession_ExposesPromiseAndDeadline()
    {
        var state = DemoScenario.Create(ScenarioCatalog.ValeriaId);
        state.Player.Country.Government.MonthsUntilElection = 4;

        var session = new GameSession(new GameSimulation(state));

        Assert.True(session.View.Government.IsElectionCampaignActive);
        Assert.True(session.View.Government.CanMakeElectionPromise);

        session.MakeElectionPromise("Tax relief");

        Assert.False(session.View.Government.CanMakeElectionPromise);
        Assert.Contains(
            "tax rate",
            session.View.Government.ElectionPromise,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "Campaign commitment",
            session.View.Government.ElectionPromiseStatus);
    }

    private static void MakeDominantCandidate(
        Simulation.Countries.Country country,
        Character candidate)
    {
        foreach (var character in country.PoliticalFigures.Where(character =>
                     character.IsPoliticallyActive))
        {
            foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            {
                character.SetPowerBaseStanding(
                    powerBase,
                    ReferenceEquals(character, candidate) ? 100 : 0);
            }

            character.Influence =
                ReferenceEquals(character, candidate) ? 100 : 0;
            character.Competence =
                ReferenceEquals(character, candidate) ? 100 : 0;
            character.Ambition =
                ReferenceEquals(character, candidate) ? 100 : 0;
            character.Legitimacy =
                ReferenceEquals(character, candidate) ? 100 : 0;
        }
    }
}
