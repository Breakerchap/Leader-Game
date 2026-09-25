using LeaderGame.Simulation;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class MilitaryAiTests
{
    [Fact]
    public void ExhaustedForeignArmyChoosesDefensiveStance()
    {
        var state = DemoScenario.Create();
        state.Random = new ConstantRandom(0.5);

        var player = state.Player.Country;
        var valeria = state.Countries.Single(country => country.Id == "valeria");
        valeria.WarExhaustion = 80;

        var war = new War
        {
            Attacker = player,
            Defender = valeria,
            StartedOn = state.Date
        };
        state.Wars.Add(war);

        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(WarStance.Defensive, war.DefenderStance);
    }

    [Fact]
    public void StrongForeignArmyCanPressAdvantageAggressively()
    {
        var state = DemoScenario.Create();
        state.Random = new ConstantRandom(0.5);

        var player = state.Player.Country;
        var valeria = state.Countries.Single(country => country.Id == "valeria");
        valeria.ArmySize = player.ArmySize * 2;
        valeria.ArmyReadiness = 90;
        valeria.WarExhaustion = 0;

        var war = new War
        {
            Attacker = valeria,
            Defender = player,
            StartedOn = state.Date,
            WarScore = 45
        };
        state.Wars.Add(war);

        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(WarStance.Aggressive, war.AttackerStance);
    }

    private sealed class ConstantRandom : IRandomSource
    {
        private readonly double _value;

        public ConstantRandom(double value) => _value = value;

        public double NextDouble() => _value;
    }
}
