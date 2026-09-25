using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public sealed class SetBudgetOrder : Order
{
    public required Country Country { get; init; }

    public required decimal TargetArmyFunding { get; init; }

    public required decimal TargetAdministrationFunding { get; init; }

    public required decimal TargetCourtFunding { get; init; }
}
