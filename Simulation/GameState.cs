using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation;

public class GameState
{
    public GameDate Date { get; set; }

    public List<Country> Countries { get; } = [];

    public List<Order> PendingOrders { get; } = [];

    public List<SimulationReport> Reports { get; } = [];

    public RelationshipGraph Relationships { get; } = new();

    public required PlayerState Player { get; set; }
}
