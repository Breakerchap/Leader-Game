using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;
using LeaderGame.Simulation.Systems;

namespace LeaderGame.Tests;

public class CourtActionTests
{
    [Fact]
    public void PrivateAudience_BuildsTrustButAlsoInfluence()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var subject = country.PoliticalFigures.First(character =>
            character.Id == 6);

        var relationship =
            state.Relationships.GetOrCreate(
                subject,
                country.Ruler);

        var trust = relationship.Trust;
        var opinion = relationship.Opinion;
        var influence = subject.Influence;

        var order = new CourtActionOrder
        {
            Issuer = country.Ruler,
            Recipient = subject,
            IssuedOn = state.Date,
            Country = country,
            Subject = subject,
            ActionType = CourtActionType.PrivateAudience
        };

        CourtActionSystem.Process(state, order);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(relationship.Trust > trust);
        Assert.True(relationship.Opinion > opinion);
        Assert.True(subject.Influence > influence);
    }

    [Fact]
    public void Patronage_BuysLoyaltyWhileEmpoweringRecipient()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var subject = country.PoliticalFigures.First(character =>
            character.Id == 6);

        var relationship =
            state.Relationships.GetOrCreate(
                subject,
                country.Ruler);

        var treasury = country.Treasury;
        var debt = country.Debt;
        var trust = relationship.Trust;
        var influence = subject.Influence;

        var strongestBase =
            Enum.GetValues<PowerBaseType>()
                .OrderByDescending(type =>
                    subject.GetPowerBaseStanding(type))
                .First();

        var standing =
            subject.GetPowerBaseStanding(
                strongestBase);

        var order = new CourtActionOrder
        {
            Issuer = country.Ruler,
            Recipient = subject,
            IssuedOn = state.Date,
            Country = country,
            Subject = subject,
            ActionType = CourtActionType.GrantPatronage
        };

        CourtActionSystem.Process(state, order);

        Assert.True(
            country.Treasury < treasury ||
            country.Debt > debt);
        Assert.True(relationship.Trust > trust);
        Assert.True(subject.Influence > influence);
        Assert.True(
            subject.GetPowerBaseStanding(
                strongestBase) >
            standing);
    }

    [Fact]
    public void PublicRebuke_TradesTrustForFearAndLowerInfluence()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var subject = country.PoliticalFigures.First(character =>
            character.Id == 6);

        var relationship =
            state.Relationships.GetOrCreate(
                subject,
                country.Ruler);

        relationship.Trust = 55;
        relationship.Opinion = 20;
        relationship.Fear = 10;

        var trust = relationship.Trust;
        var opinion = relationship.Opinion;
        var fear = relationship.Fear;
        var influence = subject.Influence;

        var order = new CourtActionOrder
        {
            Issuer = country.Ruler,
            Recipient = subject,
            IssuedOn = state.Date,
            Country = country,
            Subject = subject,
            ActionType = CourtActionType.PublicRebuke
        };

        CourtActionSystem.Process(state, order);

        Assert.True(relationship.Trust < trust);
        Assert.True(relationship.Opinion < opinion);
        Assert.True(relationship.Fear > fear);
        Assert.True(subject.Influence < influence);
    }

    [Fact]
    public void KnownPlot_CanBeDisruptedByPublicRebuke()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var subject = country.PoliticalFigures.First(character =>
            character.Id == 6);

        var plot = new PoliticalPlot
        {
            Country = country,
            Instigator = subject,
            Progress = 52
        };
        plot.DiscoveryStage = 1;
        state.Plots.Add(plot);

        var order = new CourtActionOrder
        {
            Issuer = country.Ruler,
            Recipient = subject,
            IssuedOn = state.Date,
            Country = country,
            Subject = subject,
            ActionType = CourtActionType.PublicRebuke
        };

        CourtActionSystem.Process(state, order);

        Assert.True(plot.Progress < 52);
    }

    [Fact]
    public void GameSession_AllowsOnlyOnePersonalCourtActionPerMonth()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(
            new GameSimulation(state));

        var figures =
            session.View.Court.Figures
                .Where(figure =>
                    figure.CanReceiveCourtAction)
                .Take(2)
                .ToList();

        Assert.Equal(2, figures.Count);

        session.TakeCourtAction(
            "PrivateAudience",
            figures[0].Id);

        session.TakeCourtAction(
            "GrantPatronage",
            figures[1].Id);

        var order = Assert.IsType<CourtActionOrder>(
            Assert.Single(
                state.PendingOrders));

        Assert.Equal(
            figures[0].Id,
            order.Subject.Id);
        Assert.False(
            session.View.Court.CanTakeCourtAction);
        Assert.Contains(
            "already",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PendingCourtAction_RoundTripsThroughSave()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var subject = country.PoliticalFigures.First(character =>
            character.Id == 6);

        state.PendingOrders.Add(
            new CourtActionOrder
            {
                Issuer = country.Ruler,
                Recipient = subject,
                IssuedOn = state.Date,
                Country = country,
                Subject = subject,
                ActionType =
                    CourtActionType.GrantPatronage
            });

        var loaded =
            GameSaveService.Deserialize(
                GameSaveService.Serialize(state));

        var restored =
            Assert.IsType<CourtActionOrder>(
                Assert.Single(
                    loaded.PendingOrders));

        Assert.Equal(
            subject.Id,
            restored.Subject.Id);
        Assert.Equal(
            CourtActionType.GrantPatronage,
            restored.ActionType);
        Assert.Same(
            loaded.Player.Country,
            restored.Country);
    }
}
