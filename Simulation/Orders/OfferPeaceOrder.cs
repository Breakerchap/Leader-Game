using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;

namespace LeaderGame.Simulation.Orders;

public sealed class OfferPeaceOrder : Order
{
    public required Country Country { get; init; }

    public required War War { get; init; }

    public PeaceSettlementType RequestedSettlement { get; init; } =
        PeaceSettlementType.WhitePeace;
}
