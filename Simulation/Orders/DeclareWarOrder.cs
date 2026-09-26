using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;

namespace LeaderGame.Simulation.Orders;

public sealed class DeclareWarOrder : Order
{
    public required Country SourceCountry { get; init; }

    public required Country TargetCountry { get; init; }

    public WarAim Aim { get; init; } =
        WarAim.Reparations;
}
