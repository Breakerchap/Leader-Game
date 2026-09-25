using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PeaceNegotiationTests
{
    [Fact]
    public void ExhaustedLosingEnemyAcceptsWhitePeace()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var war = DeclareWar(state, source, target);
        war.WarScore = 55;
        target.WarExhaustion = 80;
        target.ArmyReadiness = 30;

        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var order = new OfferPeaceOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = source,
            War = war,
            Terms = PeaceOfferTerms.WhitePeace
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(WarStatus.WhitePeace, war.Status);
    }

    [Fact]
    public void DominantSideCanDemandReparations()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var war = DeclareWar(state, source, target);
        war.WarScore = 90;
        target.WarExhaustion = 85;

        var targetDebt = target.Debt;

        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        simulation.SubmitOrder(new OfferPeaceOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = source,
            War = war,
            Terms = PeaceOfferTerms.DemandReparations
        });

        simulation.AdvanceMonth();

        Assert.Equal(WarStatus.NegotiatedPeace, war.Status);
        Assert.True(target.Treasury >= 0);
        Assert.True(target.Debt >= targetDebt);
    }

    [Fact]
    public void WinningEnemyRejectsCheekyReparationsDemand()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var war = DeclareWar(state, source, target);
        war.WarScore = -60;
        target.WarExhaustion = 10;
        target.ArmyReadiness = 80;

        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var order = new OfferPeaceOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = source,
            War = war,
            Terms = PeaceOfferTerms.DemandReparations
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal(WarStatus.Active, war.Status);
    }

    private static War DeclareWar(
        GameState state,
        Country source,
        Country target)
    {
        var simulation = new GameSimulation(state);
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;

        simulation.SubmitOrder(new DeclareWarOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = source,
            TargetCountry = target
        });

        simulation.AdvanceMonth();

        return state.Wars.Single();
    }

    private sealed class ConstantRandom : IRandomSource
    {
        private readonly double _value;

        public ConstantRandom(double value) => _value = value;

        public double NextDouble() => _value;
    }
}
