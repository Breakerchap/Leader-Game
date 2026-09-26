using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public enum RegionalActionType
{
    AssertCentralAuthority,
    BargainWithLocalElites,
    InvestInRegion
}

public sealed class RegionalActionOrder : Order
{
    public required Country Country { get; init; }

    public required Region Region { get; init; }

    public RegionalActionType ActionType { get; init; }
}
