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
        if (State.Player.HasLost)
            throw new InvalidOperationException("Orders cannot be issued after the player has lost.");

        if (!order.Issuer.IsAlive)
            throw new InvalidOperationException("A dead character cannot issue an order.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be submitted.");

        State.PendingOrders.Add(order);
    }

    public void AdvanceMonth()
    {
        State.Reports.AddRange(LifeSystem.ProcessMonth(State));

        State.Reports.AddRange(SuccessionSystem.Process(State));

        ProcessOrders();

        State.Reports.AddRange(EconomySystem.ProcessMonth(State));

        PoliticalSystem.ProcessMonth(State);

        State.Reports.AddRange(DomesticPoliticsSystem.ProcessMonth(State));

        DiplomacySystem.ProcessMonth(State);

        MilitaryAiSystem.ProcessMonth(State);

        State.Reports.AddRange(WarSystem.ProcessMonth(State));

        State.Reports.AddRange(ForeignPolicySystem.ProcessMonth(State));

        State.Reports.AddRange(PlotSystem.ProcessMonth(State));

        State.Date = State.Date.NextMonth();
    }

    private void ProcessOrders()
    {
        foreach (var order in State.PendingOrders.ToArray())
        {
            if (!order.Issuer.IsAlive)
            {
                order.Status = OrderStatus.Rejected;
                State.Reports.Add(new Reports.SimulationReport(
                    State.Date,
                    Reports.ReportCategory.Order,
                    "Order cancelled",
                    $"An order from {order.Issuer.FullName} was cancelled because the issuer is dead."));
                continue;
            }

            State.Reports.Add(OrderProcessor.Process(State, order));
        }

        State.PendingOrders.RemoveAll(order => order.Status != OrderStatus.Pending);
    }
}
