using LeaderGame.Simulation.Characters;

namespace LeaderGame.Tests;

public class RelationshipTests
{
    [Fact]
    public void Relationships_AreDirectional()
    {
        var a = new Character
        {
            Id = 1,
            FirstName = "A",
            LastName = "One"
        };

        var b = new Character
        {
            Id = 2,
            FirstName = "B",
            LastName = "Two"
        };

        var graph = new RelationshipGraph();

        graph.Set(a, b, opinion: 80, trust: 90, fear: 0);
        graph.Set(b, a, opinion: -40, trust: 20, fear: 30);

        Assert.Equal(80, graph.GetOrCreate(a, b).Opinion);
        Assert.Equal(-40, graph.GetOrCreate(b, a).Opinion);
        Assert.Equal(90, graph.GetOrCreate(a, b).Trust);
        Assert.Equal(20, graph.GetOrCreate(b, a).Trust);
    }
}
