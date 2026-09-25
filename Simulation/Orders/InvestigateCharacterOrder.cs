using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public sealed class InvestigateCharacterOrder : Order
{
    public required Country Country { get; init; }

    public required Character Subject { get; init; }
}
