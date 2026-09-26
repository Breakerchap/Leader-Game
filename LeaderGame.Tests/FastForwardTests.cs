using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class FastForwardTests
{
    [Fact]
    public void ExistingDecision_PreventsFastForward()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("hochwald")!;

        state.PoliticalCrises.Add(new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.RegionalBreakdown,
            Region = region,
            StartedOn = state.Date,
            AwaitingDecision = true
        });

        var session = new GameSession(
            new GameSimulation(state));
        var before = state.Date;

        session.AdvanceUntilDecision();

        Assert.Equal(before, state.Date);
        Assert.Contains(
            "decision already needs",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FastForward_StopsWhenNewCrisisAppears()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("hochwald")!;

        region.Unrest = 84;
        region.CrownControl = 25;
        region.LocalElitePower = 90;

        var session = new GameSession(
            new GameSimulation(state));

        session.AdvanceUntilDecision(12);

        Assert.Equal(
            new GameDate(1450, 2),
            state.Date);
        Assert.Contains(
            state.PoliticalCrises,
            crisis =>
                crisis.Status ==
                    PoliticalCrisisStatus.Active &&
                crisis.AwaitingDecision);
        Assert.Contains(
            "new crisis",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FastForward_CanPassThroughQuietMonths()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        state.Date = new GameDate(1450, 2);

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
        {
            country.Ruler.SetPowerBaseStanding(
                powerBase,
                95);
        }

        foreach (var relation in state.Diplomacy.ForCountry(country))
        {
            relation.HasTradeAgreement = true;
            relation.TradeAgreementStartedOn = state.Date;
        }

        var session = new GameSession(
            new GameSimulation(state));

        session.AdvanceUntilDecision(2);

        Assert.Equal(
            new GameDate(1450, 4),
            state.Date);
        Assert.Contains(
            "Advanced 2 month",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FastForward_StopsWhenCouncilElectionCampaignBegins()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;

        state.Date = new GameDate(1450, 2);
        country.Government.MonthsUntilElection = 7;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
        {
            country.Ruler.SetPowerBaseStanding(
                powerBase,
                95);
        }

        foreach (var relation in state.Diplomacy.ForCountry(country))
        {
            relation.HasTradeAgreement = true;
            relation.TradeAgreementStartedOn = state.Date;
        }

        var session = new GameSession(
            new GameSimulation(state));

        session.AdvanceUntilDecision(12);

        Assert.Equal(
            6,
            country.Government.MonthsUntilElection);
        Assert.Equal(
            new GameDate(1450, 3),
            state.Date);
        Assert.Contains(
            "council-election",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }
}
