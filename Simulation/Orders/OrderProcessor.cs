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
            ImproveRelationsOrder diplomacyOrder => ProcessImproveRelationsOrder(state, diplomacyOrder),
            NegotiateTradeAgreementOrder tradeOrder => ProcessTradeAgreementOrder(state, tradeOrder),
            EndTradeAgreementOrder endTradeOrder => ProcessEndTradeAgreementOrder(state, endTradeOrder),
            NegotiateNonAggressionPactOrder pactOrder => ProcessNonAggressionPactOrder(state, pactOrder),
            EndNonAggressionPactOrder endPactOrder => ProcessEndNonAggressionPactOrder(state, endPactOrder),
            RespondToDiplomaticProposalOrder responseOrder => ProcessDiplomaticProposalResponse(state, responseOrder),
            DeclareWarOrder warOrder => ProcessDeclareWarOrder(state, warOrder),
            SetWarStanceOrder stanceOrder => ProcessSetWarStanceOrder(state, stanceOrder),
            OfferPeaceOrder peaceOrder => ProcessOfferPeaceOrder(state, peaceOrder),
            AppointAdvisorOrder appointmentOrder => ProcessAppointAdvisorOrder(state, appointmentOrder),
            DismissAdvisorOrder dismissalOrder => ProcessDismissAdvisorOrder(state, dismissalOrder),
            InvestigateCharacterOrder investigationOrder => ProcessInvestigationOrder(state, investigationOrder),
            ArrestCharacterOrder arrestOrder => ProcessArrestOrder(state, arrestOrder),
            ReleasePrisonerOrder releaseOrder => ProcessReleasePrisonerOrder(state, releaseOrder),
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
                "A living active Treasurer serving the target country must receive tax orders.");
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
                "A living active Treasurer serving the target country must receive budget orders.");
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

        if (!candidate.IsPoliticallyActive ||
            !order.Country.ContainsPoliticalFigure(candidate) ||
            ReferenceEquals(candidate, order.Country.Ruler))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Appointment rejected",
                "The proposed adviser must be an active political figure in the target country.");
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
            !target.IsPoliticallyActive ||
            !order.Country.ContainsPoliticalFigure(target) ||
            !target.Position.HasValue ||
            ReferenceEquals(target, order.Country.Ruler))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Dismissal rejected",
                "Only the ruler may dismiss an active office-holder serving this country.");
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
            !chancellor.IsPoliticallyActive ||
            !order.Country.ContainsPoliticalFigure(chancellor))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Investigation rejected",
                "An active Chancellor serving the target country must conduct the investigation.");
        }

        if (!subject.IsPoliticallyActive ||
            !order.Country.ContainsPoliticalFigure(subject) ||
            ReferenceEquals(subject, order.Country.Ruler))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Investigation rejected",
                "The subject must be an active political figure other than the ruler.");
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

    private static SimulationReport ProcessArrestOrder(
        GameState state,
        ArrestCharacterOrder order)
    {
        var marshal = order.Recipient;
        var subject = order.Subject;

        if (!IsServingMarshal(order.Country, marshal))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Arrest rejected",
                "An active Marshal serving the target country must carry out arrests.");
        }

        if (!subject.IsPoliticallyActive ||
            !order.Country.ContainsPoliticalFigure(subject) ||
            ReferenceEquals(subject, order.Country.Ruler) ||
            ReferenceEquals(subject, marshal))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Arrest rejected",
                "The subject must be another active political figure in this country.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.Country,
            marshal,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                $"{marshal.FullName} refuses the arrest",
                $"{marshal.FullName} refuses to arrest {subject.FullName}. " +
                $"Their willingness to obey is only {willingness:F0}/100.");
        }

        var plot = state.Plots.FirstOrDefault(candidate =>
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Country, order.Country) &&
            ReferenceEquals(candidate.Instigator, subject));

        var supporterInfluence = plot is null
            ? 0
            : order.Country.PoliticalFigures
                .Where(character =>
                    character.IsPoliticallyActive &&
                    plot.SupporterIds.Contains(character.Id))
                .Sum(character => character.Influence);

        var enforcement =
            marshal.Competence * 0.35 +
            willingness * 0.35 +
            order.Country.ArmyReadiness * 0.30;

        var resistance =
            subject.Influence * 0.45 +
            subject.Ambition * 0.20 +
            Math.Min(20, supporterInfluence * 0.15);

        if (enforcement < resistance)
        {
            order.Status = OrderStatus.Failed;
            order.Country.Government.Stability -= 3;
            order.Country.PublicUnrest += 2;
            subject.Influence += 5;

            var subjectToRuler = state.Relationships.GetOrCreate(subject, order.Issuer);
            subjectToRuler.ChangeOpinion(-20);
            subjectToRuler.ChangeFear(-10);

            if (plot is null)
            {
                plot = new PoliticalPlot
                {
                    Country = order.Country,
                    Instigator = subject,
                    Progress = 30
                };
                plot.DiscoveryStage = 1;
                state.Plots.Add(plot);
            }
            else
            {
                plot.Progress += 20;
                plot.DiscoveryStage = Math.Max(plot.DiscoveryStage, 1);
            }

            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"Attempt to arrest {subject.FullName} fails",
                $"{marshal.FullName} cannot secure {subject.FullName}. Enforcement strength " +
                $"{enforcement:F0} was insufficient against resistance {resistance:F0}. " +
                "The failed arrest damages government authority and hardens opposition.");
        }

        order.Status = OrderStatus.Completed;
        var oldPosition = subject.Position;
        subject.Position = null;
        subject.Status = PoliticalStatus.Imprisoned;
        subject.Influence = Math.Max(0, subject.Influence - 25);

        if (plot is not null)
        {
            plot.IsResolved = true;
            plot.Succeeded = false;
        }

        var toRuler = state.Relationships.GetOrCreate(subject, order.Issuer);
        toRuler.ChangeOpinion(-40);
        toRuler.Trust = 0;
        toRuler.ChangeFear(30);

        var credibleEvidence = plot?.DiscoveryStage >= 2;

        if (credibleEvidence)
        {
            order.Country.Government.Stability += 1;
        }
        else
        {
            order.Country.Ruler.Legitimacy -= 4;
            order.Country.Government.Stability -= 3;
            order.Country.PublicUnrest += 2;

            foreach (var observer in order.Country.PoliticalFigures.Where(character =>
                         character.IsPoliticallyActive &&
                         !ReferenceEquals(character, order.Country.Ruler) &&
                         !ReferenceEquals(character, subject)))
            {
                state.Relationships
                    .GetOrCreate(observer, order.Country.Ruler)
                    .ChangeOpinion(-5);
            }
        }

        var roleText = oldPosition.HasValue
            ? $" and is removed as {oldPosition.Value}"
            : string.Empty;

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{subject.FullName} imprisoned",
            credibleEvidence
                ? $"{marshal.FullName} arrests {subject.FullName}{roleText}. Credible evidence " +
                  "of conspiracy makes the action politically defensible."
                : $"{marshal.FullName} arrests {subject.FullName}{roleText}, but the government " +
                  "lacks credible evidence. The arbitrary-looking imprisonment damages " +
                  "legitimacy and stability.");
    }

    private static SimulationReport ProcessReleasePrisonerOrder(
        GameState state,
        ReleasePrisonerOrder order)
    {
        var prisoner = order.Recipient;

        if (!ReferenceEquals(order.Issuer, order.Country.Ruler) ||
            !prisoner.IsAlive ||
            prisoner.Status != PoliticalStatus.Imprisoned ||
            !order.Country.ContainsPoliticalFigure(prisoner))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Release rejected",
                "Only the ruler may release a living prisoner held in this country.");
        }

        prisoner.Status = PoliticalStatus.Active;

        var prisonerToRuler =
            state.Relationships.GetOrCreate(prisoner, order.Country.Ruler);
        prisonerToRuler.ChangeOpinion(10);
        prisonerToRuler.ChangeTrust(5);
        prisonerToRuler.ChangeFear(-15);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{prisoner.FullName} released",
            $"{prisoner.FullName} is released from imprisonment and may again participate " +
            "in political life. The gesture improves the relationship somewhat, but does " +
            "not erase the original grievance.");
    }

    private static SimulationReport ProcessImproveRelationsOrder(
        GameState state,
        ImproveRelationsOrder order)
    {
        var chancellor = order.Recipient;

        if (!IsServingChancellor(order.SourceCountry, chancellor) ||
            ReferenceEquals(order.SourceCountry, order.TargetCountry) ||
            !state.Countries.Contains(order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Diplomatic mission rejected",
                "An active Chancellor must conduct diplomacy with another simulated country.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.SourceCountry,
            chancellor,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{chancellor.FullName} refuses the diplomatic mission",
                $"{chancellor.FullName} refuses to lead outreach to {order.TargetCountry.Name}. " +
                $"Their willingness to obey is only {willingness:F0}/100.");
        }

        var relation = state.Diplomacy.GetOrCreate(
            order.SourceCountry,
            order.TargetCountry);

        var effectiveness = Diplomacy.DiplomaticCalculations.GetMissionEffectiveness(
            state,
            order.SourceCountry,
            chancellor);

        var improvement = Math.Clamp(
            (int)Math.Round(2 + effectiveness / 18.0 - relation.Tension / 45.0),
            1,
            8);

        relation.ChangeRelations(improvement);
        relation.ChangeTrust(Math.Max(1, improvement / 3));
        relation.ChangeTension(-Math.Max(1, improvement / 2));

        var foreignRulerToIssuer = state.Relationships.GetOrCreate(
            order.TargetCountry.Ruler,
            order.SourceCountry.Ruler);
        foreignRulerToIssuer.ChangeOpinion(Math.Max(1, improvement));
        foreignRulerToIssuer.ChangeTrust(Math.Max(1, improvement / 2));

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Diplomacy,
            $"Relations improve with {order.TargetCountry.Name}",
            $"{chancellor.FullName}'s mission improves relations by {improvement} points. " +
            $"Relations are now {relation.Relations}, trust {relation.Trust}, " +
            $"tension {relation.Tension}.");
    }

    private static SimulationReport ProcessTradeAgreementOrder(
        GameState state,
        NegotiateTradeAgreementOrder order)
    {
        var chancellor = order.Recipient;

        if (!IsServingChancellor(order.SourceCountry, chancellor) ||
            ReferenceEquals(order.SourceCountry, order.TargetCountry) ||
            !state.Countries.Contains(order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Trade negotiation rejected",
                "An active Chancellor must negotiate with another simulated country.");
        }

        if (!order.SourceCountry.IsNeighbor(order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                "Trade negotiation rejected",
                $"{order.TargetCountry.Name} is not an immediate neighbour. Long-distance " +
                "trade infrastructure is not modelled yet.");
        }

        var relation = state.Diplomacy.GetOrCreate(
            order.SourceCountry,
            order.TargetCountry);

        if (relation.HasTradeAgreement)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                "Trade negotiation rejected",
                $"{order.SourceCountry.Name} and {order.TargetCountry.Name} already have a trade agreement.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.SourceCountry,
            chancellor,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{chancellor.FullName} refuses trade negotiations",
                $"{chancellor.FullName} refuses to negotiate with {order.TargetCountry.Name}. " +
                $"Their willingness to obey is only {willingness:F0}/100.");
        }

        var acceptance = Diplomacy.DiplomaticCalculations.GetTradeAcceptanceScore(
            state,
            order.SourceCountry,
            order.TargetCountry,
            chancellor);

        if (acceptance < 55)
        {
            order.Status = OrderStatus.Failed;
            relation.ChangeRelations(-1);
            relation.ChangeTension(2);

            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{order.TargetCountry.Name} rejects the trade agreement",
                $"{chancellor.FullName} completes the negotiation, but the foreign government " +
                "declines the proposal. Relations cool slightly and tension increases.");
        }

        relation.HasTradeAgreement = true;
        relation.TradeAgreementStartedOn = state.Date;
        relation.ChangeRelations(3);
        relation.ChangeTrust(5);
        relation.ChangeTension(-3);

        var foreignRulerToIssuer = state.Relationships.GetOrCreate(
            order.TargetCountry.Ruler,
            order.SourceCountry.Ruler);
        foreignRulerToIssuer.ChangeOpinion(3);
        foreignRulerToIssuer.ChangeTrust(5);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Diplomacy,
            $"Trade agreement with {order.TargetCountry.Name}",
            $"{order.SourceCountry.Name} and {order.TargetCountry.Name} conclude a trade agreement. " +
            "Both economies will begin receiving trade income this month.");
    }

    private static SimulationReport ProcessEndTradeAgreementOrder(
        GameState state,
        EndTradeAgreementOrder order)
    {
        var chancellor = order.Recipient;

        if (!IsServingChancellor(order.SourceCountry, chancellor) ||
            ReferenceEquals(order.SourceCountry, order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Trade termination rejected",
                "An active Chancellor must formally end a trade agreement.");
        }

        var relation = state.Diplomacy.GetOrCreate(
            order.SourceCountry,
            order.TargetCountry);

        if (!relation.HasTradeAgreement)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                "No trade agreement to end",
                $"{order.SourceCountry.Name} has no trade agreement with {order.TargetCountry.Name}.");
        }

        relation.HasTradeAgreement = false;
        relation.TradeAgreementStartedOn = null;
        relation.ChangeRelations(-3);
        relation.ChangeTrust(-5);
        relation.ChangeTension(4);

        var foreignRulerToIssuer = state.Relationships.GetOrCreate(
            order.TargetCountry.Ruler,
            order.SourceCountry.Ruler);
        foreignRulerToIssuer.ChangeOpinion(-5);
        foreignRulerToIssuer.ChangeTrust(-8);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Diplomacy,
            $"Trade agreement with {order.TargetCountry.Name} ended",
            $"{order.SourceCountry.Name} terminates its trade agreement with " +
            $"{order.TargetCountry.Name}. The decision damages trust and raises tension.");
    }

    private static SimulationReport ProcessNonAggressionPactOrder(
        GameState state,
        NegotiateNonAggressionPactOrder order)
    {
        var chancellor = order.Recipient;

        if (!IsServingChancellor(order.SourceCountry, chancellor) ||
            ReferenceEquals(order.SourceCountry, order.TargetCountry) ||
            !state.Countries.Contains(order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Pact negotiation rejected",
                "An active Chancellor must negotiate with another simulated country.");
        }

        if (!order.SourceCountry.IsNeighbor(order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                "Pact negotiation rejected",
                $"{order.TargetCountry.Name} is not an immediate neighbour.");
        }

        var relation = state.Diplomacy.GetOrCreate(
            order.SourceCountry,
            order.TargetCountry);

        if (relation.HasNonAggressionPact)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                "Pact negotiation rejected",
                $"{order.SourceCountry.Name} and {order.TargetCountry.Name} already have a non-aggression pact.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.SourceCountry,
            chancellor,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{chancellor.FullName} refuses pact negotiations",
                $"{chancellor.FullName} refuses to negotiate with {order.TargetCountry.Name}. " +
                $"Their willingness to obey is only {willingness:F0}/100.");
        }

        var acceptance = Diplomacy.DiplomaticCalculations.GetNonAggressionAcceptanceScore(
            state,
            order.SourceCountry,
            order.TargetCountry,
            chancellor);

        if (acceptance < 55)
        {
            order.Status = OrderStatus.Failed;
            relation.ChangeRelations(-1);
            relation.ChangeTension(2);

            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{order.TargetCountry.Name} rejects the non-aggression pact",
                $"{chancellor.FullName} completes the negotiation, but the foreign government " +
                "declines the pact. Relations cool slightly and tension increases.");
        }

        relation.HasNonAggressionPact = true;
        relation.NonAggressionPactStartedOn = state.Date;
        relation.ChangeRelations(3);
        relation.ChangeTrust(6);
        relation.ChangeTension(-8);

        var foreignRulerToIssuer = state.Relationships.GetOrCreate(
            order.TargetCountry.Ruler,
            order.SourceCountry.Ruler);
        foreignRulerToIssuer.ChangeOpinion(3);
        foreignRulerToIssuer.ChangeTrust(5);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Diplomacy,
            $"Non-aggression pact with {order.TargetCountry.Name}",
            $"{order.SourceCountry.Name} and {order.TargetCountry.Name} conclude a " +
            "non-aggression pact. Strategic tension falls, but any underlying border " +
            "dispute remains unresolved.");
    }

    private static SimulationReport ProcessEndNonAggressionPactOrder(
        GameState state,
        EndNonAggressionPactOrder order)
    {
        var chancellor = order.Recipient;

        if (!IsServingChancellor(order.SourceCountry, chancellor) ||
            ReferenceEquals(order.SourceCountry, order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Pact termination rejected",
                "An active Chancellor must formally end a non-aggression pact.");
        }

        var relation = state.Diplomacy.GetOrCreate(
            order.SourceCountry,
            order.TargetCountry);

        if (!relation.HasNonAggressionPact)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                "No non-aggression pact to end",
                $"{order.SourceCountry.Name} has no non-aggression pact with {order.TargetCountry.Name}.");
        }

        relation.HasNonAggressionPact = false;
        relation.NonAggressionPactStartedOn = null;
        relation.ChangeRelations(-5);
        relation.ChangeTrust(-10);
        relation.ChangeTension(12);

        var foreignRulerToIssuer = state.Relationships.GetOrCreate(
            order.TargetCountry.Ruler,
            order.SourceCountry.Ruler);
        foreignRulerToIssuer.ChangeOpinion(-6);
        foreignRulerToIssuer.ChangeTrust(-10);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Diplomacy,
            $"Non-aggression pact with {order.TargetCountry.Name} ended",
            $"{order.SourceCountry.Name} renounces its non-aggression pact with " +
            $"{order.TargetCountry.Name}. Trust falls sharply and strategic tension rises.");
    }

    private static SimulationReport ProcessSetWarStanceOrder(
        GameState state,
        SetWarStanceOrder order)
    {
        var marshal = order.Recipient;

        if (order.War.Status != Military.WarStatus.Active ||
            !order.War.IsParticipant(order.Country) ||
            !IsServingMarshal(order.Country, marshal))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Military directive rejected",
                "An active Marshal can only receive directives for an active war their country is fighting.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.Country,
            marshal,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Military,
                $"{marshal.FullName} refuses the military directive",
                $"{marshal.FullName} refuses to adopt the requested " +
                $"{order.RequestedStance} stance. Their willingness to obey is " +
                $"{willingness:F0}/100.");
        }

        var enactedStance =
            willingness < 35 &&
            order.RequestedStance != Military.WarStance.Balanced
                ? Military.WarStance.Balanced
                : order.RequestedStance;

        order.War.SetStance(order.Country, enactedStance);
        order.Status = OrderStatus.Completed;

        var interpretation = enactedStance == order.RequestedStance
            ? "as ordered"
            : "but moderates the instruction into a Balanced stance";

        return new SimulationReport(
            state.Date,
            ReportCategory.Military,
            $"{marshal.FullName} sets the war stance",
            $"{marshal.FullName} receives the order for a {order.RequestedStance} " +
            $"campaign and implements it {interpretation}. Current stance: {enactedStance}.");
    }

    private static SimulationReport ProcessOfferPeaceOrder(
        GameState state,
        OfferPeaceOrder order)
    {
        var chancellor = order.Recipient;

        if (order.War.Status != Military.WarStatus.Active ||
            !order.War.IsParticipant(order.Country) ||
            !ReferenceEquals(order.Issuer, order.Country.Ruler) ||
            !IsServingChancellor(order.Country, chancellor))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Peace offer rejected",
                "An active Chancellor may negotiate peace only in a war their country is fighting.");
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
                ReportCategory.Diplomacy,
                $"{chancellor.FullName} refuses peace negotiations",
                $"{chancellor.FullName} refuses to carry the proposed settlement to the enemy. " +
                $"Their willingness to obey is only {willingness:F0}/100.");
        }

        var acceptance = Systems.WarSystem.GetPeaceAcceptanceScore(
            order.War,
            order.Country,
            order.RequestedSettlement);

        if (acceptance < 55)
        {
            order.Status = OrderStatus.Failed;

            return new SimulationReport(
                state.Date,
                ReportCategory.Military,
                $"{order.War.OpponentOf(order.Country).Name} rejects the peace offer",
                $"The proposed {order.RequestedSettlement} settlement is rejected. " +
                $"Estimated acceptance was {acceptance:F0}/100, so the war continues.");
        }

        order.Status = OrderStatus.Completed;

        return Systems.WarSystem.ResolveNegotiatedPeace(
            state,
            order.War,
            order.RequestedSettlement);
    }


    private static SimulationReport ProcessDeclareWarOrder(
        GameState state,
        DeclareWarOrder order)
    {
        var chancellor = order.Recipient;

        if (!IsServingChancellor(order.SourceCountry, chancellor) ||
            ReferenceEquals(order.SourceCountry, order.TargetCountry) ||
            !state.Countries.Contains(order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "War declaration rejected",
                "An active Chancellor must deliver a declaration to another simulated country.");
        }

        if (!order.SourceCountry.IsNeighbor(order.TargetCountry))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                "War declaration rejected",
                $"{order.TargetCountry.Name} is not an immediate neighbour in the current prototype.");
        }

        if (state.Wars.Any(war =>
                war.Status == Military.WarStatus.Active &&
                war.IsParticipant(order.SourceCountry) &&
                war.IsParticipant(order.TargetCountry)))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Military,
                "War declaration rejected",
                $"{order.SourceCountry.Name} and {order.TargetCountry.Name} are already at war.");
        }

        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            order.SourceCountry,
            chancellor,
            order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{chancellor.FullName} refuses to deliver the declaration",
                $"{chancellor.FullName} refuses to formally declare war on " +
                $"{order.TargetCountry.Name}. Their willingness to obey is only " +
                $"{willingness:F0}/100.");
        }

        var relation = state.Diplomacy.GetOrCreate(
            order.SourceCountry,
            order.TargetCountry);
        var preWarRelations = relation.Relations;
        var brokeNonAggressionPact = relation.HasNonAggressionPact;

        relation.HasTradeAgreement = false;
        relation.TradeAgreementStartedOn = null;
        relation.HasNonAggressionPact = false;
        relation.NonAggressionPactStartedOn = null;
        relation.Relations = Math.Min(-70, relation.Relations - 30);
        relation.Trust = Math.Max(0, relation.Trust - 40);
        relation.Tension = 100;

        if (brokeNonAggressionPact)
        {
            relation.ChangeRelations(-10);
            relation.ChangeTrust(-20);
            order.SourceCountry.Ruler.Legitimacy -= 8;
            order.SourceCountry.Government.Stability -= 5;
            order.SourceCountry.PublicUnrest += 5;
        }

        var pendingProposals = state.DiplomaticProposals.Where(proposal =>
            proposal.Status == Diplomacy.DiplomaticProposalStatus.Pending &&
            ((ReferenceEquals(proposal.SourceCountry, order.SourceCountry) &&
              ReferenceEquals(proposal.TargetCountry, order.TargetCountry)) ||
             (ReferenceEquals(proposal.SourceCountry, order.TargetCountry) &&
              ReferenceEquals(proposal.TargetCountry, order.SourceCountry))));

        foreach (var proposal in pendingProposals)
            proposal.Status = Diplomacy.DiplomaticProposalStatus.Withdrawn;

        ApplyWarDeclarationPoliticalCost(order.SourceCountry, preWarRelations);

        var targetToSource = state.Relationships.GetOrCreate(
            order.TargetCountry.Ruler,
            order.SourceCountry.Ruler);
        targetToSource.ChangeOpinion(-35);
        targetToSource.Trust = 0;
        targetToSource.ChangeFear(10);

        var sourceToTarget = state.Relationships.GetOrCreate(
            order.SourceCountry.Ruler,
            order.TargetCountry.Ruler);
        sourceToTarget.ChangeOpinion(-20);
        sourceToTarget.ChangeTrust(-20);

        var war = new Military.War
        {
            Attacker = order.SourceCountry,
            Defender = order.TargetCountry,
            StartedOn = state.Date,
            AttackerGoal = order.Goal
        };

        state.Wars.Add(war);
        order.Status = OrderStatus.Completed;

        var treatyBreachText = brokeNonAggressionPact
            ? " The declaration also breaks an active non-aggression pact, causing a " +
              "major additional loss of trust and domestic legitimacy."
            : string.Empty;

        return new SimulationReport(
            state.Date,
            ReportCategory.Military,
            $"{order.SourceCountry.Name} declares war on {order.TargetCountry.Name}",
            $"{chancellor.FullName} delivers the declaration. Trade and pending " +
            $"diplomatic offers between the two states end immediately.{treatyBreachText} " +
            $"Declared war goal: {order.Goal}. War score begins at 0.");
    }

    private static void ApplyWarDeclarationPoliticalCost(
        Countries.Country country,
        int previousRelations)
    {
        if (previousRelations >= 25)
        {
            country.Ruler.Legitimacy -= 5;
            country.Government.Stability -= 4;
            country.PublicUnrest += 4;
        }
        else if (previousRelations >= 0)
        {
            country.Ruler.Legitimacy -= 3;
            country.Government.Stability -= 2;
            country.PublicUnrest += 2;
        }
        else
        {
            country.Government.Stability -= 1;
            country.PublicUnrest += 1;
        }
    }

    private static SimulationReport ProcessDiplomaticProposalResponse(
        GameState state,
        RespondToDiplomaticProposalOrder order)
    {
        var proposal = order.Proposal;
        var chancellor = order.Recipient;

        if (proposal.Status != Diplomacy.DiplomaticProposalStatus.Pending ||
            !ReferenceEquals(proposal.TargetCountry, order.Country) ||
            !ReferenceEquals(order.Issuer, order.Country.Ruler) ||
            !IsServingChancellor(order.Country, chancellor))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Diplomatic response rejected",
                "The proposal is no longer pending or cannot be handled by the current Chancellor.");
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
                ReportCategory.Diplomacy,
                $"{chancellor.FullName} refuses to formalise the response",
                $"{chancellor.FullName} refuses to deliver the ruler's response to " +
                $"{proposal.SourceCountry.Name}. The proposal remains pending.");
        }

        var relation = state.Diplomacy.GetOrCreate(
            proposal.SourceCountry,
            proposal.TargetCountry);

        var proposalName = proposal.Type == Diplomacy.DiplomaticProposalType.TradeAgreement
            ? "trade agreement"
            : "non-aggression pact";

        if (!order.Accept)
        {
            proposal.Status = Diplomacy.DiplomaticProposalStatus.Rejected;
            relation.ChangeRelations(-2);
            relation.ChangeTrust(-3);
            relation.ChangeTension(
                proposal.Type == Diplomacy.DiplomaticProposalType.NonAggressionPact ? 3 : 2);

            var proposerToRuler = state.Relationships.GetOrCreate(
                proposal.SourceCountry.Ruler,
                proposal.TargetCountry.Ruler);
            proposerToRuler.ChangeOpinion(-4);
            proposerToRuler.ChangeTrust(-3);

            order.Status = OrderStatus.Completed;

            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{proposalName} proposal from {proposal.SourceCountry.Name} rejected",
                $"{order.Country.Name} rejects the proposed {proposalName}. " +
                "The refusal causes a small deterioration in relations.");
        }

        if (proposal.Type == Diplomacy.DiplomaticProposalType.TradeAgreement)
        {
            relation.HasTradeAgreement = true;
            relation.TradeAgreementStartedOn = state.Date;
            relation.ChangeRelations(3);
            relation.ChangeTrust(5);
            relation.ChangeTension(-3);
        }
        else
        {
            relation.HasNonAggressionPact = true;
            relation.NonAggressionPactStartedOn = state.Date;
            relation.ChangeRelations(3);
            relation.ChangeTrust(6);
            relation.ChangeTension(-8);
        }

        var sourceToTarget = state.Relationships.GetOrCreate(
            proposal.SourceCountry.Ruler,
            proposal.TargetCountry.Ruler);
        sourceToTarget.ChangeOpinion(3);
        sourceToTarget.ChangeTrust(5);

        var targetToSource = state.Relationships.GetOrCreate(
            proposal.TargetCountry.Ruler,
            proposal.SourceCountry.Ruler);
        targetToSource.ChangeOpinion(2);
        targetToSource.ChangeTrust(3);

        proposal.Status = Diplomacy.DiplomaticProposalStatus.Accepted;
        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Diplomacy,
            $"{proposalName} proposal from {proposal.SourceCountry.Name} accepted",
            $"{proposal.SourceCountry.Name} and {proposal.TargetCountry.Name} " +
            $"enter a {proposalName}.");
    }

    private static bool IsServingChancellor(
        Countries.Country country,
        Character character)
    {
        return character.Position == Position.Chancellor &&
               character.IsPoliticallyActive &&
               country.ContainsPoliticalFigure(character) &&
               !ReferenceEquals(character, country.Ruler);
    }

    private static bool IsServingTreasurer(
        Countries.Country country,
        Character character)
    {
        return character.Position == Position.Treasurer &&
               character.IsPoliticallyActive &&
               country.ContainsPoliticalFigure(character) &&
               !ReferenceEquals(character, country.Ruler);
    }

    private static bool IsServingMarshal(
        Countries.Country country,
        Character character)
    {
        return character.Position == Position.Marshal &&
               character.IsPoliticallyActive &&
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
