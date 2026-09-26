using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Scenarios;
using LeaderGame.Simulation.Systems;

namespace LeaderGame.Tests;

public class LineageDevelopmentTests
{
    [Fact]
    public void DeadDynasty_SurfacesCadetBeforeContinuityCanEndCampaign()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var surname = state.Player.CurrentCharacter.LastName;

        foreach (var member in state.Player.Lineage.Members)
            member.IsAlive = false;

        var reports =
            LineageDevelopmentSystem.ProcessMonth(state)
                .ToList();

        var cadet = Assert.Single(
            state.Player.Lineage.Members,
            member => member.IsAlive);

        Assert.Equal(surname, cadet.LastName);
        Assert.Contains(cadet, country.PoliticalFigures);
        Assert.Contains(cadet, country.SuccessionOrder);
        Assert.Contains(
            reports,
            report => report.Title.Contains(
                "cadet branch",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DeadPartyLeadership_CanRenewFromWiderOrganisation()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;

        foreach (var member in state.Player.Lineage.Members)
            member.IsAlive = false;

        var reports =
            LineageDevelopmentSystem.ProcessMonth(state)
                .ToList();

        var recruit = Assert.Single(
            state.Player.Lineage.Members,
            member => member.IsAlive);

        Assert.Contains(recruit, country.PoliticalFigures);
        Assert.True(
            recruit.GetPowerBaseStanding(
                Politics.PowerBaseType.Party) >= 70);
        Assert.Contains(
            reports,
            report => report.Title.Contains(
                "new leadership",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExiledIrrelevantLineage_DoesNotReceiveFreeAnnualRenewal()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var outsider = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        country.Ruler = outsider;
        state.Date = new GameDate(1451, 1);

        foreach (var member in state.Player.Lineage.Members)
        {
            member.Status = PoliticalStatus.Exiled;
            member.Influence = 0;
            member.Legitimacy = 0;
        }

        var originalIds =
            state.Player.Lineage.Members
                .Select(member => member.Id)
                .ToHashSet();

        LineageDevelopmentSystem.ProcessMonth(state).ToList();

        Assert.All(
            state.Player.Lineage.Members,
            member => Assert.Contains(member.Id, originalIds));
        Assert.Empty(
            state.Player.Lineage.Members.Where(member =>
                member.IsPoliticallyActive));
    }

    [Fact]
    public void OpposingRulingHouse_DoesNotBecomePartOfPlayerDynasty()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var outsider = country.PoliticalFigures.First(character =>
            !state.Player.Lineage.Contains(character) &&
            character.IsPoliticallyActive);

        country.Ruler = outsider;
        country.SuccessionOrder.Clear();
        state.Date = new GameDate(1451, 1);

        var playerSurname =
            state.Player.Lineage.Members[0].LastName;

        // With no recognised successor, annual succession maintenance has a
        // very high chance to surface a relative of the ruling house.
        // Re-run a few January snapshots deterministically if necessary.
        for (var attempt = 0;
             attempt < 6 &&
             country.SuccessionOrder.Count == 0;
             attempt++)
        {
            LineageDevelopmentSystem.ProcessMonth(state).ToList();
        }

        Assert.All(
            state.Player.Lineage.Members,
            member => Assert.Equal(
                playerSurname,
                member.LastName));

        Assert.DoesNotContain(
            state.Player.Lineage.Members,
            member => member.LastName ==
                      outsider.LastName &&
                      outsider.LastName != playerSurname);
    }

    [Fact]
    public void DynamicallyCreatedLineageMember_RoundTripsThroughSave()
    {
        var state = DemoScenario.Create();

        foreach (var member in state.Player.Lineage.Members)
            member.IsAlive = false;

        LineageDevelopmentSystem.ProcessMonth(state).ToList();

        var created = Assert.Single(
            state.Player.Lineage.Members,
            member => member.IsAlive);

        var loaded =
            GameSaveService.Deserialize(
                GameSaveService.Serialize(state));

        var restored = Assert.Single(
            loaded.Player.Lineage.Members,
            member => member.Id == created.Id);

        Assert.Equal(created.FullName, restored.FullName);
        Assert.Contains(
            loaded.Player.Country.PoliticalFigures,
            member => member.Id == restored.Id);
    }
}
