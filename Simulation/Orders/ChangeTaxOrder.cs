using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public sealed class ChangeTaxOrder : Order
{
    public required Country Country { get; init; }

    /// <summary>
    /// Target tax rate as a fraction. For example, 0.12 means 12%.
    /// </summary>
    public required decimal TargetTaxRate { get; init; }
}
