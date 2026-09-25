using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;

namespace LeaderGame.Simulation.Orders;

public sealed class SetWarStanceOrder : Order
{
    public required Country Country { get; init; }

    public required War War { get; init; }

    public WarStance RequestedStance { get; init; } = WarStance.Balanced;
}
