using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class WarSystemTests
{
    [Fact]
    public void ActiveWarProducesCasualtiesExhaustionAndWarScoreMovement()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var sourceArmy = source.ArmySize;
        var targetArmy = target.ArmySize;

        DeclareWar(state, source, target);

        var war = Assert.Single(state.Wars);

        Assert.Equal(1, war.MonthsActive);
        Assert.True(source.ArmySize < sourceArmy);
        Assert.True(target.ArmySize < targetArmy);
        Assert.True(source.WarExhaustion > 0);
        Assert.NotEqual(0, war.WarScore);
    }

    [Fact]
    public void OverwhelmingAdvantageCanEndWarDecisively()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        target.ArmySize = 500;
        target.ArmyReadiness = 10;

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

        var war = Assert.Single(state.Wars);
        war.WarScore = 99;

        simulation.AdvanceMonth();

        Assert.Equal(WarStatus.AttackerVictory, war.Status);
    }

    [Fact]
    public void WarOperationsCanCreateDebt()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);
        source.Treasury = 0;

        DeclareWar(state, source, target);

        Assert.True(source.Debt > 0);
    }

    [Fact]
    public void MarshalCanReceiveStanceDirective()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        DeclareWar(state, source, target);

        var war = Assert.Single(state.Wars);
        var marshal = source.GetOfficeHolder(Position.Marshal)!;
        var simulation = new GameSimulation(state);

        var order = new SetWarStanceOrder
        {
            Issuer = source.Ruler,
            Recipient = marshal,
            IssuedOn = state.Date,
            Country = source,
            War = war,
            RequestedStance = WarStance.Aggressive
        };

        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(WarStance.Aggressive, war.AttackerStance);
    }

    private static void DeclareWar(
        GameState state,
        Countries.Country source,
        Countries.Country target)
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
    }

    private sealed class ConstantRandom : IRandomSource
    {
        private readonly double _value;

        public ConstantRandom(double value)
        {
            _value = value;
        }

        public double NextDouble() => _value;
    }
}
