using LeaderGame.Simulation.Information;
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

        if (State.Player.HasWon)
            throw new InvalidOperationException("Orders cannot be issued after the campaign has been won.");

        if (!State.Player.IsInPower &&
            ReferenceEquals(order.Issuer, State.Player.Country.Ruler))
        {
            order.Status = OrderStatus.Rejected;
            State.Reports.Add(new Reports.SimulationReport(
                State.Date,
                Reports.ReportCategory.Order,
                "Government order unavailable",
                $"{State.Player.Lineage.Name} does not currently control the government of " +
                $"{State.Player.Country.Name}, so it cannot issue directives in the ruler's name."));
            return;
        }

        if (!order.Issuer.IsAlive)
            throw new InvalidOperationException("A dead character cannot issue an order.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be submitted.");

        State.PendingOrders.Add(order);
    }

    public void AdvanceMonth()
    {
        if (State.Player.HasLost || State.Player.HasWon)
            return;

        State.Reports.AddRange(LifeSystem.ProcessMonth(State));

        State.Reports.AddRange(SuccessionSystem.Process(State));
        State.Reports.AddRange(PlayerContinuitySystem.ResolveLeadership(State));

        if (State.Player.HasLost)
            return;

        ProcessOrders();

        State.Reports.AddRange(EconomySystem.ProcessMonth(State));

        WorldDynamicsSystem.ProcessMonth(State);

        ForeignGovernmentSystem.ProcessMonth(State);

        PoliticalSystem.ProcessMonth(State);

        State.Reports.AddRange(DomesticPoliticsSystem.ProcessMonth(State));

        State.Reports.AddRange(OppositionActionSystem.ProcessMonth(State));

        State.Reports.AddRange(ElectionSystem.ProcessMonth(State));

        DiplomacySystem.ProcessMonth(State);

        MilitaryAiSystem.ProcessMonth(State);

        State.Reports.AddRange(WarSystem.ProcessMonth(State));

        State.Reports.AddRange(ForeignPolicySystem.ProcessMonth(State));

        State.Reports.AddRange(CabinetProposalSystem.ProcessMonth(State));

        State.Reports.AddRange(PlotSystem.ProcessMonth(State));

        State.Reports.AddRange(PlayerContinuitySystem.ProcessMonth(State));

        State.Reports.AddRange(CampaignSystem.ProcessMonth(State));

        InformationSystem.CaptureTruthSnapshot(State);
        State.Reports.AddRange(InformationSystem.ProcessMonth(State));

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
