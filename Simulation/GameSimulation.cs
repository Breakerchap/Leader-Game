namespace LeaderGame.Simulation;

public class GameSimulation
{
    public GameState State { get; }

    public GameSimulation(GameState state)
    {
        State = state;
    }

    public void AdvanceMonth()
    {
        ProcessOrders();

        State.Date.AdvanceMonth();
    }

    private void ProcessOrders()
    {
        foreach (var order in State.PendingOrders)
        {
            if (order.HasBeenProcessed)
                continue;

            // Eventually:
            // interpret order
            // decide whether recipient obeys
            // execute consequences
            // record outcome

            order.HasBeenProcessed = true;
        }

        State.PendingOrders.RemoveAll(order => order.HasBeenProcessed);
    }
}