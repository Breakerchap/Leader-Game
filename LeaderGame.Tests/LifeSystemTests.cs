using LeaderGame.Simulation;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Reports;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class LifeSystemTests
{
    [Fact]
    public void NaturalDeath_TriggersSuccessionInSameMonth()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var oldRuler = country.Ruler;
        var heir = state.Player.Lineage.Members.Single(character =>
            !ReferenceEquals(character, oldRuler));

        oldRuler.Age = 90;
        oldRuler.Health = 1;
        state.Random = new SequenceRandom(0.0);

        simulation.AdvanceMonth();

        Assert.False(oldRuler.IsAlive);
        Assert.Same(heir, country.Ruler);
        Assert.Same(heir, state.Player.CurrentCharacter);
        Assert.Contains(
            state.Reports,
            report => report.Category == ReportCategory.Personal &&
                      report.Title.Contains("dies"));
    }

    [Fact]
    public void IllnessCanOccurFromDeterministicRandomSource()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var ruler = state.Player.Country.Ruler;

        ruler.Age = 70;
        ruler.Health = 100;

        // First draw avoids death, second triggers illness, third chooses severity.
        state.Random = new SequenceRandom(1.0, 0.0, 0.5);

        simulation.AdvanceMonth();

        Assert.True(ruler.IsAlive);
        Assert.InRange(ruler.Health, 70, 89);
        Assert.Contains(
            state.Reports,
            report => report.Category == ReportCategory.Personal &&
                      report.Title.Contains("falls ill"));
    }

    [Fact]
    public void CharactersAgeAtEndOfYear()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var ruler = state.Player.Country.Ruler;
        var oldAge = ruler.Age;

        state.Date = new GameDate(1450, 12);
        state.Random = new SequenceRandom(1.0);

        simulation.AdvanceMonth();

        Assert.Equal(oldAge + 1, ruler.Age);
        Assert.Equal(new GameDate(1451, 1), state.Date);
    }

    private sealed class SequenceRandom : IRandomSource
    {
        private readonly Queue<double> _values;

        public SequenceRandom(params double[] values)
        {
            _values = new Queue<double>(values);
        }

        public double NextDouble()
        {
            return _values.Count > 0
                ? _values.Dequeue()
                : 1.0;
        }
    }
}
