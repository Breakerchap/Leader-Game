using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public sealed class EndNonAggressionPactOrder : Order
{
    public required Country SourceCountry { get; init; }

    public required Country TargetCountry { get; init; }
}
