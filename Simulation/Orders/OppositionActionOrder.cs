using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Politics;

namespace LeaderGame.Simulation.Orders;

public enum OppositionActionType
{
    OrganiseSupport,
    BuildCoalition,
    PublicPressure
}

/// <summary>
/// A deliberate action taken by the player's political lineage while it is
/// outside government. The controlled opposition leader is both issuer and
/// recipient because this represents the leader directing their own political
/// organisation rather than commanding the state.
/// </summary>
public sealed class OppositionActionOrder : Order
{
    public required Country Country { get; init; }

    public OppositionActionType ActionType { get; init; }

    public PowerBaseType TargetPowerBase { get; init; }
}
