using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public enum CourtActionType
{
    PrivateAudience,
    GrantPatronage,
    PublicRebuke
}

public sealed class CourtActionOrder : Order
{
    public required Country Country { get; init; }

    public required Characters.Character Subject { get; init; }

    public CourtActionType ActionType { get; init; }
}
