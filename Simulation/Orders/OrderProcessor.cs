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
            SetBudgetOrder budgetOrder => ProcessSetBudgetOrder(state, budgetOrder),
            AppointAdvisorOrder appointmentOrder => ProcessAppointAdvisorOrder(state, appointmentOrder),
            DismissAdvisorOrder dismissalOrder => ProcessDismissAdvisorOrder(state, dismissalOrder),
            InvestigateCharacterOrder investigationOrder => ProcessInvestigationOrder(state, investigationOrder),
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

        if (!IsServingTreasurer(order.Country, treasurer))
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

    private static SimulationReport ProcessSetBudgetOrder(
        GameState state,
        SetBudgetOrder order)
    {
        if (!IsFundingTargetValid(order.TargetArmyFunding) ||
            !IsFundingTargetValid(order.TargetAdministrationFunding) ||
            !IsFundingTargetValid(order.TargetCourtFunding))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Budget order rejected",
                "Each funding target must be between 50% and 150% of normal funding.");
        }

        var treasurer = order.Recipient;

        if (!IsServingTreasurer(order.Country, treasurer))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Budget order rejected",
                "A living Treasurer serving the target country must receive budget orders.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.Country,
            treasurer,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                $"{treasurer.FullName} refuses the budget order",
                $"{treasurer.FullName} refuses to reorganise government spending. " +
                $"Their willingness to obey was {willingness:F0}/100.");
        }

        var implementationFactor =
            PoliticalCalculations.GetImplementationFactor(treasurer, willingness);

        order.Country.ArmyFunding = MoveTowardsTarget(
            order.Country.ArmyFunding,
            order.TargetArmyFunding,
            implementationFactor);
        order.Country.AdministrationFunding = MoveTowardsTarget(
            order.Country.AdministrationFunding,
            order.TargetAdministrationFunding,
            implementationFactor);
        order.Country.CourtFunding = MoveTowardsTarget(
            order.Country.CourtFunding,
            order.TargetCourtFunding,
            implementationFactor);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{treasurer.FullName} implements the budget",
            $"Requested funding — army {order.TargetArmyFunding:P0}, administration " +
            $"{order.TargetAdministrationFunding:P0}, court {order.TargetCourtFunding:P0}. " +
            $"Enacted — army {order.Country.ArmyFunding:P0}, administration " +
            $"{order.Country.AdministrationFunding:P0}, court {order.Country.CourtFunding:P0}. " +
            $"Willingness {willingness:F0}/100.");
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
            ApplyDismissalConsequences(state, order.Country, order.Issuer, previousHolder);

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

    private static SimulationReport ProcessDismissAdvisorOrder(
        GameState state,
        DismissAdvisorOrder order)
    {
        var target = order.Recipient;

        if (!ReferenceEquals(order.Issuer, order.Country.Ruler) ||
            !target.IsAlive ||
            !order.Country.ContainsPoliticalFigure(target) ||
            !target.Position.HasValue ||
            ReferenceEquals(target, order.Country.Ruler))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Dismissal rejected",
                "Only the ruler may dismiss a living office-holder serving this country.");
        }

        var oldPosition = target.Position.Value;
        var oldInfluence = target.Influence;

        ApplyDismissalConsequences(state, order.Country, order.Issuer, target);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{target.FullName} dismissed as {oldPosition}",
            $"{target.FullName} loses the office of {oldPosition}. Their immediate " +
            $"influence falls from {oldInfluence} to {target.Influence}, but the " +
            "dismissal creates a personal grievance against the ruler.");
    }

    private static SimulationReport ProcessInvestigationOrder(
        GameState state,
        InvestigateCharacterOrder order)
    {
        var chancellor = order.Recipient;
        var subject = order.Subject;

        if (chancellor.Position != Position.Chancellor ||
            !chancellor.IsAlive ||
            !order.Country.ContainsPoliticalFigure(chancellor))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Investigation rejected",
                "A living Chancellor serving the target country must conduct the investigation.");
        }

        if (!subject.IsAlive ||
            !order.Country.ContainsPoliticalFigure(subject) ||
            ReferenceEquals(subject, order.Country.Ruler))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Investigation rejected",
                "The subject must be a living political figure other than the ruler.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.Country,
            chancellor,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;

            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                $"{chancellor.FullName} refuses the investigation",
                $"{chancellor.FullName} will not investigate {subject.FullName}. " +
                $"Their willingness to obey is only {willingness:F0}/100.");
        }

        var effectiveness =
            chancellor.Competence * 0.60 +
            willingness * 0.40;

        var plot = state.Plots.FirstOrDefault(candidate =>
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Country, order.Country) &&
            ReferenceEquals(candidate.Instigator, subject));

        var subjectToRuler = state.Relationships.GetOrCreate(subject, order.Issuer);
        subjectToRuler.ChangeOpinion(-10);
        subjectToRuler.ChangeTrust(-5);
        subjectToRuler.ChangeFear(8);

        order.Status = OrderStatus.Completed;

        if (plot is null)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"Investigation of {subject.FullName}",
                effectiveness >= 70
                    ? $"{chancellor.FullName} conducts a thorough investigation and finds " +
                      $"no evidence of an organised plot by {subject.FullName}. The scrutiny " +
                      "has nevertheless damaged their relationship with the ruler."
                    : $"{chancellor.FullName}'s investigation of {subject.FullName} is " +
                      "inconclusive. The scrutiny has damaged their relationship with the ruler.");
        }

        var disruption = 5 + effectiveness * 0.15;
        plot.Progress -= disruption;

        if (effectiveness >= 75)
        {
            plot.DiscoveryStage = Math.Max(plot.DiscoveryStage, 2);

            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"Plot by {subject.FullName} exposed",
                $"{chancellor.FullName} uncovers credible evidence of a coup plot. " +
                "The investigation disrupts its organisation substantially.");
        }

        if (effectiveness >= 50)
        {
            plot.DiscoveryStage = Math.Max(plot.DiscoveryStage, 1);

            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"Suspicious activity around {subject.FullName}",
                $"{chancellor.FullName} uncovers suspicious contacts and meetings, " +
                "but not enough evidence to prove a coup plot. The investigation still " +
                "disrupts some of the subject's political activity.");
        }

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"Investigation of {subject.FullName} inconclusive",
            $"{chancellor.FullName} fails to establish anything useful. Unknown to the " +
            "government, the investigation still disrupts some political activity.");
    }

    private static bool IsServingTreasurer(
        Countries.Country country,
        Character character)
    {
        return character.Position == Position.Treasurer &&
               character.IsAlive &&
               country.ContainsPoliticalFigure(character) &&
               !ReferenceEquals(character, country.Ruler);
    }

    private static bool IsFundingTargetValid(decimal target) =>
        target is >= 0.5m and <= 1.5m;

    private static decimal MoveTowardsTarget(
        decimal current,
        decimal target,
        decimal implementationFactor)
    {
        return current + (target - current) * implementationFactor;
    }

    private static void ApplyDismissalConsequences(
        GameState state,
        Countries.Country country,
        Character ruler,
        Character dismissed)
    {
        dismissed.Position = null;
        dismissed.Influence = Math.Max(0, dismissed.Influence - 8);

        var dismissedToRuler =
            state.Relationships.GetOrCreate(dismissed, ruler);
        dismissedToRuler.ChangeOpinion(-25);
        dismissedToRuler.ChangeTrust(-15);
        dismissedToRuler.ChangeFear(5);

        dismissed.ChangeAllegiance(
            PoliticalKeys.Lineage(state.Player.Lineage.Id),
            -5);
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
