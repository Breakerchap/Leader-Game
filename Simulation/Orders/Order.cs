using LeaderGame.Simulation.Characters;

namespace LeaderGame.Simulation.Orders;

public enum OrderStatus
{
    Pending,
    Completed,
    Failed,
    Refused,
    Rejected
}

public abstract class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Character Issuer { get; init; }

    public required Character Recipient { get; init; }

    public required GameDate IssuedOn { get; init; }

    public OrderStatus Status { get; internal set; } = OrderStatus.Pending;
}
