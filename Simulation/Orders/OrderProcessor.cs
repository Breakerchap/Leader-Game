using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Politics;
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

        var treasurer = order.Recipient;

        if (treasurer.Position != Position.Treasurer ||
            !treasurer.IsAlive ||
            !order.Country.ContainsPoliticalFigure(treasurer) ||
            ReferenceEquals(treasurer, order.Country.Ruler))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Tax order rejected",
                "A living Treasurer serving the target country must receive tax orders.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.Country,
            treasurer,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;

            var relationship = state.Relationships.GetOrCreate(treasurer, order.Issuer);
            relationship.ChangeOpinion(-5);
            relationship.ChangeTrust(-4);

            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                $"{treasurer.FullName} refuses the tax order",
                $"{treasurer.FullName} refuses to implement the requested " +
                $"{order.TargetTaxRate:P1} tax rate. Their willingness to obey was " +
                $"{willingness:F0}/100.");
        }

        var oldRate = order.Country.TaxRate;
        var requestedChange = order.TargetTaxRate - oldRate;
        var implementationFactor =
            PoliticalCalculations.GetImplementationFactor(treasurer, willingness);

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

        var implementationDescription = willingness < 45
            ? "obstructed"
            : willingness < 65
                ? "reluctant"
                : "co-operative";

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{treasurer.FullName} implements the tax order",
            $"Requested {order.TargetTaxRate:P1}; enacted {order.Country.TaxRate:P1}. " +
            $"Implementation was {implementationDescription}: competence " +
            $"{treasurer.Competence}/100, willingness {willingness:F0}/100. " +
            $"Public unrest is now {order.Country.PublicUnrest:F1} and stability " +
            $"{order.Country.Government.Stability:F1}.");
    }

    private static SimulationReport ProcessAppointAdvisorOrder(
        GameState state,
        AppointAdvisorOrder order)
    {
        var candidate = order.Recipient;

        if (!candidate.IsAlive ||
            !order.Country.ContainsPoliticalFigure(candidate) ||
            ReferenceEquals(candidate, order.Country.Ruler))
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

        var previousHolder = order.Country.GetOfficeHolder(order.Position);

        if (previousHolder is not null)
        {
            previousHolder.Position = null;

            var dismissedToRuler =
                state.Relationships.GetOrCreate(previousHolder, order.Issuer);
            dismissedToRuler.ChangeOpinion(-25);
            dismissedToRuler.ChangeTrust(-15);
            dismissedToRuler.ChangeFear(5);

            previousHolder.ChangeAllegiance(
                PoliticalKeys.Lineage(state.Player.Lineage.Id),
                -5);
        }

        candidate.Position = order.Position;

        var candidateToRuler =
            state.Relationships.GetOrCreate(candidate, order.Issuer);
        candidateToRuler.ChangeOpinion(10);
        candidateToRuler.ChangeTrust(5);

        order.Status = OrderStatus.Completed;

        var replacementText = previousHolder is null
            ? "The office was vacant."
            : $"{previousHolder.FullName} was dismissed; their opinion and trust of " +
              $"{order.Issuer.FullName} have fallen.";

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{candidate.FullName} appointed {order.Position}",
            $"{candidate.FullName} now serves as {order.Position}. {replacementText}");
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
