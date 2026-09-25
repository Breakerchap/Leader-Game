using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public sealed class ReleasePrisonerOrder : Order
{
    public required Country Country { get; init; }
}
