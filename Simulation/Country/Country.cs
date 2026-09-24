using LeaderGame.Simulation.Characters;

namespace LeaderGame.Simulation.Countries;

public class Country
{
    public required string Id { get; init; }
    public required string Name { get; set; }

    public long Population { get; set; }

    public decimal Gdp { get; set; }
    public decimal Treasury { get; set; }

    public int ArmySize { get; set; }

    public required Government Government { get; set; }

    public required Ruler Ruler { get; set; }

    public List<Advisor> Advisors { get; } = [];
}