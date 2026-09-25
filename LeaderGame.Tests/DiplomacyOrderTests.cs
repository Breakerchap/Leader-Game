using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class DiplomacyOrderTests
{
    [Fact]
    public void Chancellor_CanImproveRelations()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var target = state.Countries.Single(candidate => candidate.Id == "valeria");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(country, target);
        var before = relation.Relations;

        var order = new ImproveRelationsOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(relation.Relations > before);
        Assert.True(relation.Tension < 47);
    }

    [Fact]
    public void HostileChancellor_CanRefuseDiplomaticMission()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var target = state.Countries.Single(candidate => candidate.Id == "nordmark");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(country, target);
        var before = relation.Relations;

        chancellor.Ambition = 100;
        chancellor.SetAllegiance(PoliticalKeys.Country(country.Id), 0);

        var relationship = state.Relationships.GetOrCreate(chancellor, country.Ruler);
        relationship.Opinion = -100;
        relationship.Trust = 0;
        relationship.Fear = 0;

        var order = new ImproveRelationsOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Refused, order.Status);
        Assert.Equal(before, relation.Relations);
    }

    [Fact]
    public void FriendlyNeighbour_AcceptsTradeAndBothCountriesEarnIncome()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var target = state.Countries.Single(candidate => candidate.Id == "nordmark");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(country, target);

        var order = new NegotiateTradeAgreementOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(relation.HasTradeAgreement);
        Assert.True(country.LastMonthlyTradeIncome > 0);
        Assert.True(target.LastMonthlyTradeIncome > 0);
    }

    [Fact]
    public void HostileNeighbour_CanRejectTradeProposal()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var target = state.Countries.Single(candidate => candidate.Id == "valeria");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(country, target);

        var order = new NegotiateTradeAgreementOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.False(relation.HasTradeAgreement);
        Assert.True(relation.Tension > 47);
    }

    [Fact]
    public void EndingTrade_RemovesFutureTradeIncome()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var target = state.Countries.Single(candidate => candidate.Id == "nordmark");
        var chancellor = country.GetOfficeHolder(Position.Chancellor)!;
        var relation = state.Diplomacy.GetOrCreate(country, target);

        relation.HasTradeAgreement = true;
        relation.TradeAgreementStartedOn = state.Date;

        simulation.AdvanceMonth();
        Assert.True(country.LastMonthlyTradeIncome > 0);

        var order = new EndTradeAgreementOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.False(relation.HasTradeAgreement);
        Assert.Equal(0m, country.LastMonthlyTradeIncome);
    }
}
