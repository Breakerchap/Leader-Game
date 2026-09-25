using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Reports;
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
    public void ForeignAffairsView_UsesReportedRelationsInsteadOfHiddenTruth()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var foreign = state.Countries.First(other =>
            !ReferenceEquals(other, country));

        state.Diplomacy.GetOrCreate(country, foreign).Relations = 99;

        state.Knowledge.Update(new KnownInformation
        {
            Key = new InformationKey(
                InformationMetric.DiplomaticRelations,
                foreign.Id,
                country.Id),
            Estimate = 12,
            Margin = 5,
            ReportedConfidence = 70,
            AsOf = state.Date,
            ReceivedOn = state.Date,
            SourceAdvisorId = country.Ruler.Id,
            SourceAdvisorName = "Test Chancellor",
            WasRequested = false
        });

        var session = new GameSession(new GameSimulation(state));
        var view = session.View.ForeignAffairs.Countries.Single(item =>
            item.Id == foreign.Id);

        Assert.Contains("+12", view.Relations);
        Assert.DoesNotContain("+99", view.Relations);
    }

    [Fact]
    public void ForeignAffairsActions_QueueDiplomaticOrders()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));
        var target = session.View.ForeignAffairs.Countries.First();

        session.ImproveRelations(target.Id);
        session.ToggleTradeAgreement(target.Id);

        Assert.Contains(
            state.PendingOrders,
            order => order is ImproveRelationsOrder);

        Assert.Contains(
            state.PendingOrders,
            order => order is NegotiateTradeAgreementOrder or EndTradeAgreementOrder);
    }

    [Fact]
    public void WarButton_QueuesDeclarationForKnownValidNeighbour()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));
        var target = session.View.ForeignAffairs.Countries.First(country =>
            country.CanDeclareWar);

        session.DeclareWar(target.Id);

        var order = Assert.IsType<DeclareWarOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Equal(target.Id, order.TargetCountry.Id);
    }

    [Fact]
    public void MilitaryView_UsesReportedEnemyStrengthInsteadOfHiddenArmy()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var enemy = state.Countries.First(other =>
            !ReferenceEquals(other, country));

        enemy.ArmySize = 25_000;

        state.Knowledge.Update(new KnownInformation
        {
            Key = new InformationKey(
                InformationMetric.ArmySize,
                enemy.Id),
            Estimate = 7_250,
            Margin = 1_400,
            ReportedConfidence = 60,
            AsOf = state.Date,
            ReceivedOn = state.Date,
            SourceAdvisorId = country.Ruler.Id,
            SourceAdvisorName = "Test Marshal",
            WasRequested = false
        });

        state.Wars.Add(new War
        {
            Attacker = country,
            Defender = enemy,
            StartedOn = state.Date
        });

        var session = new GameSession(new GameSimulation(state));
        var campaign = Assert.Single(session.View.Military.Campaigns);

        Assert.Contains("7,250", campaign.EnemyArmy);
        Assert.DoesNotContain("25,000", campaign.EnemyArmy);
    }

    [Fact]
    public void MilitaryActions_QueueCampaignAndPeaceOrders()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var enemy = state.Countries.First(other =>
            !ReferenceEquals(other, country));

        var war = new War
        {
            Attacker = country,
            Defender = enemy,
            StartedOn = state.Date
        };
        state.Wars.Add(war);

        var session = new GameSession(new GameSimulation(state));

        session.SetWarStance(war.Id, "Aggressive");
        session.OfferPeace(war.Id, "WhitePeace");

        Assert.Contains(
            state.PendingOrders,
            order =>
                order is SetWarStanceOrder stance &&
                stance.War.Id == war.Id &&
                stance.RequestedStance == WarStance.Aggressive);

        Assert.Contains(
            state.PendingOrders,
            order =>
                order is OfferPeaceOrder peace &&
                peace.War.Id == war.Id &&
                peace.Terms == PeaceOfferTerms.WhitePeace);
    }

    [Fact]
    public void EconomyHistory_PreservesReportedClaimInsteadOfHiddenCurrentValue()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        state.AdvisorReports.Add(new AdvisorIntelligenceReport
        {
            Topic = InformationTopic.Economy,
            Advisor = treasurer,
            SubjectCountry = country,
            ProducedOn = state.Date,
            DataAsOf = state.Date,
            WasRequested = true,
            Title = "Test Treasury return",
            Summary = "A deliberately fixed historical claim."
        });

        state.AdvisorReports[^1].Facts.Add(new KnownInformation
        {
            Key = new InformationKey(
                InformationMetric.Treasury,
                country.Id),
            Estimate = 111_000,
            Margin = 10_000,
            ReportedConfidence = 70,
            AsOf = state.Date,
            ReceivedOn = state.Date,
            SourceAdvisorId = treasurer.Id,
            SourceAdvisorName = treasurer.FullName,
            WasRequested = true
        });

        country.Treasury = 9_999_999m;

        var session = new GameSession(new GameSimulation(state));
        var snapshot = session.View.Economy.History.First();

        Assert.Contains("111,000", snapshot.Treasury);
        Assert.DoesNotContain("9,999,999", snapshot.Treasury);
    }

    [Fact]
    public void PendingOrderDesk_SurvivesSaveLoadWithReadableDescription()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        state.PendingOrders.Add(new ChangeTaxOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.16m
        });

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var session = new GameSession(new GameSimulation(loaded));
        var pending = Assert.Single(session.View.PendingOrderDetails);

        Assert.Equal("Tax", pending.Type);
        Assert.Contains("16", pending.Description);
        Assert.Equal(
            loaded.Player.Country.GetOfficeHolder(Position.Treasurer)!.FullName,
            pending.Recipient);
    }

    [Fact]
    public void OrderOutcomes_AppearInDedicatedBriefingFeed()
    {
        var state = DemoScenario.Create();

        state.Reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Order,
            "Treasurer refuses the directive",
            "The Treasurer refuses to carry out the order."));

        var session = new GameSession(new GameSimulation(state));
        var outcome = Assert.Single(session.View.RecentOrderOutcomes);

        Assert.True(outcome.NeedsAttention);
        Assert.Contains("refuses", outcome.Title);
    }

    [Fact]
    public void StartingScenario_RebuildsDesktopAroundChosenCountry()
    {
        var session = new GameSession();

        Assert.True(GameSession.AvailableScenarios.Count >= 2);

        session.StartNewCampaign(ScenarioCatalog.ValeriaId);

        Assert.Equal("Valeria", session.View.CountryName);
        Assert.Equal("Vieri Coalition", session.View.LineageName);
        Assert.Equal(
            "The Valerian Republic",
            session.View.Campaign.ScenarioName);
        Assert.Equal(3, session.View.Campaign.TotalObjectives);
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

    [Fact]
    public void CourtInvestigation_QueuesRealChancellorOrder()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));
        var target = session.View.Court.Figures.First(figure =>
            figure.CanInvestigate);

        session.InvestigateCharacter(target.Id);

        var order = Assert.IsType<InvestigateCharacterOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Equal(target.Id, order.Subject.Id);
        Assert.Equal(
            Position.Chancellor,
            order.Recipient.Position);
    }

    [Fact]
    public void CourtArrest_QueuesRealMarshalOrder()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(new GameSimulation(state));
        var target = session.View.Court.Figures.First(figure =>
            figure.CanArrest);

        session.ArrestCharacter(target.Id);

        var order = Assert.IsType<ArrestCharacterOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Equal(target.Id, order.Subject.Id);
        Assert.Equal(
            Position.Marshal,
            order.Recipient.Position);
    }

    [Fact]
    public void CourtRelease_QueuesDirectRulerOrderForPrisoner()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var prisoner = country.PoliticalFigures.First(character =>
            character.IsPoliticallyActive &&
            !ReferenceEquals(character, country.Ruler) &&
            character.Position is null);

        prisoner.Status = PoliticalStatus.Imprisoned;

        var session = new GameSession(new GameSimulation(state));
        var view = session.View.Court.Figures.Single(figure =>
            figure.Id == prisoner.Id);

        Assert.True(view.CanRelease);

        session.ReleasePrisoner(prisoner.Id);

        var order = Assert.IsType<ReleasePrisonerOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Same(prisoner, order.Recipient);
        Assert.Same(country.Ruler, order.Issuer);
    }

}
