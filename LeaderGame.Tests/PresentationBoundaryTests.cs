using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PresentationBoundaryTests
{
    [Fact]
    public void DashboardMetric_UsesPlayerKnowledgeInsteadOfHiddenTreasury()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        country.Treasury = 9_999_999m;

        state.Knowledge.Update(new KnownInformation
        {
            Key = new InformationKey(
                InformationMetric.Treasury,
                country.Id),
            Estimate = 123_000,
            Margin = 12_000,
            ReportedConfidence = 70,
            AsOf = state.Date,
            ReceivedOn = state.Date,
            SourceAdvisorId = country.Ruler.Id,
            SourceAdvisorName = "Test source",
            WasRequested = false
        });

        var session = new GameSession(new GameSimulation(state));

        var treasury = session.View.Metrics.Single(metric =>
            metric.Label == "Treasury");

        Assert.Contains("123,000", treasury.Value);
        Assert.DoesNotContain("9,999,999", treasury.Value);
        Assert.Contains("Test source", treasury.Detail);
    }

    [Fact]
    public void RequestingReport_QueuesAnOrderWithoutInstantKnowledge()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));
        var oldReportCount = session.View.IntelligenceReports.Count;

        session.RequestEconomyReport();

        Assert.Equal(1, session.View.PendingOrders);
        Assert.Equal(oldReportCount, session.View.IntelligenceReports.Count);
        Assert.Contains(
            "queued",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GovernmentPolicyActions_QueueRealSimulationOrders()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));

        session.SetTaxRate(18);
        session.SetBudget(125, 90, 110);

        Assert.Equal(2, session.View.PendingOrders);
        Assert.Contains(state.PendingOrders, order => order is ChangeTaxOrder);
        Assert.Contains(state.PendingOrders, order => order is SetBudgetOrder);
    }

    [Fact]
    public void CourtAppointment_QueuesOrderForEligibleCandidate()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));
        var candidate = session.View.Court.Figures.First(figure =>
            figure.IsAvailableForOffice);

        session.AppointAdvisor(candidate.Id, "Marshal");

        var order = Assert.IsType<AppointAdvisorOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Equal(candidate.Id, order.Recipient.Id);
        Assert.Equal(Position.Marshal, order.Position);
    }

    [Fact]
    public void CourtView_DoesNotOfferExistingOfficeHoldersForAppointment()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));

        Assert.DoesNotContain(
            session.View.Court.Figures,
            figure =>
                (figure.Role is "Marshal" or "Treasurer" or "Chancellor") &&
                figure.IsAvailableForOffice);
    }

    [Fact]
    public void AdvancingMonth_RefreshesDesktopBriefing()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));
        var oldDate = session.View.Date;

        session.AdvanceMonth();

        Assert.NotEqual(oldDate, session.View.Date);
        Assert.NotEmpty(session.View.Briefings);
        Assert.NotEmpty(session.View.Advisors);
    }
}
