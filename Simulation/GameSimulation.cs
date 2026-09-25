using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Systems;

namespace LeaderGame.Simulation;

public class GameSimulation
{
    public GameState State { get; }

    public GameSimulation(GameState state)
    {
        State = state;
    }

    public void SubmitOrder(Order order)
    {
        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be submitted.");

        State.PendingOrders.Add(order);
    }

    public void AdvanceMonth()
    {
        ProcessOrders();

        State.Reports.AddRange(EconomySystem.ProcessMonth(State));

        State.Date = State.Date.NextMonth();
    }

    private void ProcessOrders()
    {
        foreach (var order in State.PendingOrders.ToArray())
        {
            State.Reports.Add(OrderProcessor.Process(State, order));
        }

        State.PendingOrders.RemoveAll(order => order.Status != OrderStatus.Pending);
    }
}
