using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class WarGoalsAndPeaceTests
{
    [Fact]
    public void DeclarationStoresChosenWarGoal()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var simulation = new GameSimulation(state);

        simulation.SubmitOrder(new DeclareWarOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = source,
            TargetCountry = target,
            Goal = WarGoalType.HumiliateRival
        });

        simulation.AdvanceMonth();

        Assert.Equal(
            WarGoalType.HumiliateRival,
            Assert.Single(state.Wars).AttackerGoal);
    }

    [Fact]
    public void DestroyedDefenderArmyProducesAttackerVictory()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var war = DeclareWar(state, source, target);
        target.ArmySize = 0;

        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(WarStatus.AttackerVictory, war.Status);
    }

    [Fact]
    public void DestroyedAttackerArmyProducesDefenderVictory()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var war = DeclareWar(state, source, target);
        source.ArmySize = 0;

        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(WarStatus.DefenderVictory, war.Status);
    }

    [Fact]
    public void BorderWarGoalVictoryReducesUnderlyingDispute()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);
        var relation = state.Diplomacy.GetOrCreate(source, target);
        relation.BorderDisputeSeverity = 80;

        var war = DeclareWar(
            state,
            source,
            target,
            WarGoalType.SettleBorderDispute);

        target.ArmySize = 0;
        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(WarStatus.AttackerVictory, war.Status);
        Assert.Equal(10, relation.BorderDisputeSeverity);
    }

    [Fact]
    public void ExhaustedOpponentCanAcceptNegotiatedWhitePeace()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var war = DeclareWar(state, source, target);
        target.WarExhaustion = 100;
        war.WarScore = 0;

        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var order = new OfferPeaceOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = source,
            War = war,
            RequestedSettlement = PeaceSettlementType.WhitePeace
        };

        var simulation = new GameSimulation(state);
        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(WarStatus.WhitePeace, war.Status);
        Assert.Equal(PeaceSettlementType.WhitePeace, war.Settlement);
    }

    [Fact]
    public void WinningOpponentCanRejectUnfavourableWhitePeace()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var war = DeclareWar(state, source, target);
        war.WarScore = -70;
        target.WarExhaustion = 0;

        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;
        var order = new OfferPeaceOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = source,
            War = war,
            RequestedSettlement = PeaceSettlementType.WhitePeace
        };

        var simulation = new GameSimulation(state);
        simulation.SubmitOrder(order);
        simulation.AdvanceMonth();

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal(WarStatus.Active, war.Status);
    }

    [Fact]
    public void HumiliationGoalDamagesDefenderRulerRatherThanPayingStandardReparations()
    {
        var state = DemoScenario.Create();
        var source = state.Player.Country;
        var target = state.Countries.Single(country => country.Id == "valeria");
        state.Random = new ConstantRandom(0.5);

        var targetLegitimacy = target.Ruler.Legitimacy;
        var targetInfluence = target.Ruler.Influence;
        var targetTreasury = target.Treasury;

        var war = DeclareWar(
            state,
            source,
            target,
            WarGoalType.HumiliateRival);

        target.ArmySize = 0;
        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(WarStatus.AttackerVictory, war.Status);
        Assert.True(target.Ruler.Legitimacy < targetLegitimacy);
        Assert.True(target.Ruler.Influence < targetInfluence);
        Assert.Equal(targetTreasury, target.Treasury);
    }

    private static War DeclareWar(
        GameState state,
        Country source,
        Country target,
        WarGoalType goal = WarGoalType.Reparations)
    {
        var simulation = new GameSimulation(state);
        var chancellor = source.GetOfficeHolder(Position.Chancellor)!;

        simulation.SubmitOrder(new DeclareWarOrder
        {
            Issuer = source.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = source,
            TargetCountry = target,
            Goal = goal
        });

        simulation.AdvanceMonth();

        return Assert.Single(state.Wars);
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
