using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public sealed class AppointAdvisorOrder : Order
{
    public required Country Country { get; init; }

    public required Position Position { get; init; }
}
