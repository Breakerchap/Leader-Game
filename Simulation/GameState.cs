using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Player;

namespace LeaderGame.Simulation;

public class GameState
{
    public GameDate Date { get; set; }

    public List<Country> Countries { get; } = [];

    public List<Order> PendingOrders { get; } = [];

    public required PlayerState Player { get; set; }
}