using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Orders;

internal static class OrderProcessor
{
    public static SimulationReport Process(GameState state, Order order)
    {
        if (order.Status != OrderStatus.Pending)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.System,
                "Order ignored",
                $"Order {order.Id} had already been processed.");
        }

        return order switch
        {
            ChangeTaxOrder taxOrder => ProcessChangeTaxOrder(state, taxOrder),
            AppointAdvisorOrder appointmentOrder => ProcessAppointAdvisorOrder(state, appointmentOrder),
            _ => RejectUnknownOrder(state, order)
        };
    }

    private static SimulationReport ProcessChangeTaxOrder(GameState state, ChangeTaxOrder order)
    {
        if (order.TargetTaxRate is < 0m or > 0.60m)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Tax order rejected",
                "The requested tax rate must be between 0% and 60%.");
        }

        if (order.Recipient is not Advisor treasurer ||
            treasurer.Position != Position.Treasurer ||
            !treasurer.IsAlive ||
            !order.Country.Advisors.Contains(treasurer))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Tax order rejected",
                "A living Treasurer serving the target country must receive tax orders.");
        }

        var oldRate = order.Country.TaxRate;
        var requestedChange = order.TargetTaxRate - oldRate;

        // Orders are not magical state changes. The recipient's ability and willingness
        // determine how closely implementation matches the ruler's instruction.
        var implementationFactor =
            0.65m +
            (Math.Clamp(treasurer.Competence, 0, 100) / 100m * 0.25m) +
            (Math.Clamp(treasurer.Loyalty, 0, 100) / 100m * 0.10m);

        var enactedChange = requestedChange * implementationFactor;
        order.Country.TaxRate = oldRate + enactedChange;

        var administrationChange =
            (Math.Clamp(treasurer.Competence, 0, 100) - 50) / 5000m;
        order.Country.AdministrativeEfficiency += administrationChange;

        var percentagePointChange = (double)(Math.Abs(enactedChange) * 100m);
        var administrativeMitigation =
            1.15 - (Math.Clamp(treasurer.Competence, 0, 100) / 200.0);
        var socialPressure = percentagePointChange * administrativeMitigation;

        if (enactedChange > 0)
        {
            order.Country.PublicUnrest += socialPressure;
            order.Country.Government.Stability -= socialPressure * 0.6;
        }
        else if (enactedChange < 0)
        {
            order.Country.PublicUnrest -= socialPressure * 0.6;
            order.Country.Government.Stability += socialPressure * 0.3;
        }

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{treasurer.FullName} implements the tax order",
            $"Requested {order.TargetTaxRate:P1}; enacted {order.Country.TaxRate:P1}. " +
            $"Competence {treasurer.Competence}, loyalty {treasurer.Loyalty}. " +
            $"Public unrest is now {order.Country.PublicUnrest:F1} and stability " +
            $"{order.Country.Government.Stability:F1}.");
    }

    private static SimulationReport ProcessAppointAdvisorOrder(
        GameState state,
        AppointAdvisorOrder order)
    {
        if (order.Recipient is not Advisor candidate ||
            !candidate.IsAlive ||
            !order.Country.Advisors.Contains(candidate))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Appointment rejected",
                "The proposed adviser must be a living political figure in the target country.");
        }

        if (candidate.Position.HasValue)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Appointment rejected",
                $"{candidate.FullName} already holds the office of {candidate.Position.Value}.");
        }

        var previousHolder = order.Country.GetAdvisor(order.Position);

        if (previousHolder is not null)
        {
            previousHolder.Position = null;
            previousHolder.Loyalty = Math.Clamp(previousHolder.Loyalty - 15, 0, 100);
        }

        candidate.Position = order.Position;
        candidate.Loyalty = Math.Clamp(candidate.Loyalty + 5, 0, 100);
        order.Status = OrderStatus.Completed;

        var replacementText = previousHolder is null
            ? $"The office was vacant."
            : $"{previousHolder.FullName} was dismissed and their loyalty fell to " +
              $"{previousHolder.Loyalty}.";

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{candidate.FullName} appointed {order.Position}",
            $"{candidate.FullName} now serves as {order.Position}. {replacementText} " +
            $"The new office-holder's loyalty is {candidate.Loyalty}.");
    }

    private static SimulationReport RejectUnknownOrder(GameState state, Order order)
    {
        order.Status = OrderStatus.Rejected;

        return new SimulationReport(
            state.Date,
            ReportCategory.System,
            "Unknown order rejected",
            $"No processor exists for {order.GetType().Name}.");
    }
}
