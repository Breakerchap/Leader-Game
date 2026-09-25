using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;

namespace LeaderGame.Simulation.Orders;

public sealed class RespondToDiplomaticProposalOrder : Order
{
    public required Country Country { get; init; }

    public required DiplomaticProposal Proposal { get; init; }

    public bool Accept { get; init; }
}
