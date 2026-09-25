using LeaderGame.Simulation.Characters;

namespace LeaderGame.Simulation.Orders;

public abstract class Order
{
    public required Character Issuer { get; init; }

    public required Character Recipient { get; init; }

    public bool HasBeenProcessed { get; set; }
}