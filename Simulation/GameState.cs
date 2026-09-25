using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation;

public class GameState
{
    public GameDate Date { get; set; }

    public List<Country> Countries { get; } = [];

    public List<Order> PendingOrders { get; } = [];

    public List<PoliticalPlot> Plots { get; } = [];

    public List<SimulationReport> Reports { get; } = [];

    public RelationshipGraph Relationships { get; } = new();

    public DiplomaticGraph Diplomacy { get; } = new();

    /// <summary>
    /// Seeded simulation randomness. Tests and future save loading can replace
    /// or restore this source without changing simulation systems.
    /// </summary>
    public IRandomSource Random { get; set; } = new SimulationRandom(42);

    public required PlayerState Player { get; set; }

    public Country? FindCountry(string id) =>
        Countries.FirstOrDefault(country => country.Id == id);
}
