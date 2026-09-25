using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public sealed class DismissAdvisorOrder : Order
{
    public required Country Country { get; init; }
}
