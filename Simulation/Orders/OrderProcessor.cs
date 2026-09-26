using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;
using LeaderGame.Simulation.Systems;

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
            RespondToDiplomaticProposalOrder responseOrder => ProcessDiplomaticProposalResponse(state, responseOrder),
            DeclareWarOrder warOrder => ProcessDeclareWarOrder(state, warOrder),
            SetWarStanceOrder stanceOrder => ProcessSetWarStanceOrder(state, stanceOrder),
            OfferPeaceOrder peaceOrder => ProcessOfferPeaceOrder(state, peaceOrder),
            AppointAdvisorOrder appointmentOrder => ProcessAppointAdvisorOrder(state, appointmentOrder),
            DismissAdvisorOrder dismissalOrder => ProcessDismissAdvisorOrder(state, dismissalOrder),
            InvestigateCharacterOrder investigationOrder => ProcessInvestigationOrder(state, investigationOrder),
            ArrestCharacterOrder arrestOrder => ProcessArrestOrder(state, arrestOrder),
            ReleasePrisonerOrder releaseOrder => ProcessReleasePrisonerOrder(state, releaseOrder),
            RequestReportOrder reportOrder => ProcessRequestReportOrder(state, reportOrder),
            OppositionActionOrder oppositionOrder => OppositionActionSystem.ProcessPlayerAction(state, oppositionOrder),
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
                $"{order.TargetTaxRate:P1} tax rate. The refusal makes clear that " +
                "their willingness to carry out the ruler's wishes is very low.");
        }

        if (InstitutionSystem.TrySubmitProposal(
                state,
                order,
                out var institutionalTaxReport))
        {
            return institutionalTaxReport!;
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

        ApplyTaxPowerBaseReaction(order.Country, enactedChange);

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
            $"Implementation appeared {implementationDescription}. Any wider effect on " +
            "unrest or regime stability will have to be assessed through subsequent reports.");
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
                "The refusal indicates serious resistance inside the treasury.");
        }

        if (InstitutionSystem.TrySubmitProposal(
                state,
                order,
                out var institutionalBudgetReport))
        {
            return institutionalBudgetReport!;
        }

        var implementationFactor =
            PoliticalCalculations.GetImplementationFactor(treasurer, willingness);

        var oldArmyFunding = order.Country.ArmyFunding;
        var oldAdministrationFunding = order.Country.AdministrationFunding;
        var oldCourtFunding = order.Country.CourtFunding;

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

        ApplyBudgetPowerBaseReaction(
            order.Country,
            oldArmyFunding,
            oldAdministrationFunding,
            oldCourtFunding);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{treasurer.FullName} implements the budget",
            $"Requested funding — army {order.TargetArmyFunding:P0}, administration " +
            $"{order.TargetAdministrationFunding:P0}, court {order.TargetCourtFunding:P0}. " +
            $"Enacted — army {order.Country.ArmyFunding:P0}, administration " +
            $"{order.Country.AdministrationFunding:P0}, court {order.Country.CourtFunding:P0}. " +
            "The difference between request and enactment reflects how the order was implemented.");
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
        ApplyAppointmentPowerBaseConsequences(
            order.Country,
            order.Issuer,
            candidate,
            order.Position);

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
            $"{target.FullName} loses the office of {oldPosition}. The dismissal costs " +
            "them some access to government but creates a serious personal and political grievance.");
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
            ReferenceEquals(subject, order.Country.Ruler) ||
            ReferenceEquals(subject, chancellor))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Investigation rejected",
                "The subject must be another active political figure in the country.");
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
                "The refusal suggests serious reluctance or a conflicting political interest.");
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
                "The refusal reveals serious resistance inside the command structure.");
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
                $"{marshal.FullName} cannot secure {subject.FullName}. The target's political " +
                "support and capacity to resist prove stronger than the government expected. " +
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
                "They are plainly unwilling to carry out the mission.");
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
            $"{chancellor.FullName}'s mission appears to have improved the diplomatic climate. " +
            "A later foreign-affairs assessment will indicate how much the relationship actually changed.");
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
                "They are plainly unwilling to conduct the talks.");
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

    private static SimulationReport ProcessOfferPeaceOrder(
        GameState state,
        OfferPeaceOrder order)
    {
        var chancellor = order.Recipient;

        if (order.War.Status != Military.WarStatus.Active ||
            !order.War.IsParticipant(order.Country) ||
            !IsServingChancellor(order.Country, chancellor))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Peace offer rejected",
                "An active Chancellor can only negotiate peace in an active war their country is fighting.");
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
                $"{chancellor.FullName} refuses to negotiate peace",
                $"{chancellor.FullName} refuses to deliver the proposed peace terms. " +
                "The refusal reveals a serious political break inside the government.");
        }

        var opponent = order.War.OpponentOf(order.Country);
        var acceptance = Military.PeaceCalculations.GetAcceptanceScore(
            state,
            order.War,
            order.Country,
            order.Terms);

        if (acceptance < 55)
        {
            order.Status = OrderStatus.Failed;

            var relation = state.Diplomacy.GetOrCreate(order.Country, opponent);
            relation.ChangeTension(1);

            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{opponent.Name} rejects the peace offer",
                $"{chancellor.FullName} presents the proposed {order.Terms} settlement, " +
                $"but {opponent.Name} believes continuing the war is preferable.");
        }

        var transfer = 0m;

        switch (order.Terms)
        {
            case Military.PeaceOfferTerms.WhitePeace:
                order.War.Status = Military.WarStatus.WhitePeace;
                break;

            case Military.PeaceOfferTerms.DemandReparations:
                order.War.Status = Military.WarStatus.NegotiatedPeace;
                transfer = TransferPayment(
                    opponent,
                    order.Country,
                    opponent.Gdp * 0.005m);
                order.Country.Government.Stability += 1;
                opponent.Government.Stability -= 1;
                break;

            case Military.PeaceOfferTerms.OfferReparations:
                order.War.Status = Military.WarStatus.NegotiatedPeace;
                transfer = TransferPayment(
                    order.Country,
                    opponent,
                    order.Country.Gdp * 0.005m);
                opponent.Government.Stability += 1;
                order.Country.Government.Stability -= 1;
                break;
        }

        order.Country.WarExhaustion -= 5;
        opponent.WarExhaustion -= 5;

        var postWarRelation = state.Diplomacy.GetOrCreate(order.Country, opponent);
        postWarRelation.Relations = Math.Min(postWarRelation.Relations, -55);
        postWarRelation.Trust = Math.Min(postWarRelation.Trust, 20);
        postWarRelation.Tension = 65;

        order.Status = OrderStatus.Completed;

        var reparationsText = transfer > 0
            ? order.Terms == Military.PeaceOfferTerms.DemandReparations
                ? $" {opponent.Name} pays {transfer:N0} in reparations."
                : $" {order.Country.Name} pays {transfer:N0} in reparations."
            : string.Empty;

        return new SimulationReport(
            state.Date,
            ReportCategory.Diplomacy,
            $"{order.Country.Name} and {opponent.Name} make peace",
            $"{opponent.Name} accepts the {order.Terms} settlement after " +
            $"{order.War.MonthsActive} months of war.{reparationsText}");
    }

    private static decimal TransferPayment(
        Countries.Country payer,
        Countries.Country receiver,
        decimal amount)
    {
        var fromTreasury = Math.Min(payer.Treasury, amount);
        payer.Treasury -= fromTreasury;

        var borrowed = amount - fromTreasury;

        if (borrowed > 0)
            payer.Debt += borrowed;

        receiver.Treasury += amount;

        return amount;
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
                $"{order.RequestedStance} stance. The refusal indicates a serious command dispute.");
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
                $"{order.TargetCountry.Name}. They are unwilling to deliver the declaration.");
        }

        var war = Military.WarDeclarationService.Declare(
            state,
            order.SourceCountry,
            order.TargetCountry);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Military,
            $"{order.SourceCountry.Name} declares war on {order.TargetCountry.Name}",
            $"{chancellor.FullName} delivers the declaration. Trade and pending " +
            "diplomatic offers between the two states end immediately. Reliable information " +
            "about the opening military position will depend on subsequent field reports.");
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

        if (proposal.Type == Diplomacy.DiplomaticProposalType.TributeUltimatum)
        {
            return ProcessTributeUltimatumResponse(
                state,
                order,
                proposal,
                relation);
        }

        if (!order.Accept)
        {
            proposal.Status = Diplomacy.DiplomaticProposalStatus.Rejected;
            relation.ChangeRelations(-2);
            relation.ChangeTrust(-3);
            relation.ChangeTension(2);

            var proposerToRuler = state.Relationships.GetOrCreate(
                proposal.SourceCountry.Ruler,
                proposal.TargetCountry.Ruler);
            proposerToRuler.ChangeOpinion(-4);
            proposerToRuler.ChangeTrust(-3);

            order.Status = OrderStatus.Completed;

            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"Trade proposal from {proposal.SourceCountry.Name} rejected",
                $"{order.Country.Name} rejects the proposed trade agreement. " +
                "The refusal causes a small deterioration in relations.");
        }

        relation.HasTradeAgreement = true;
        relation.TradeAgreementStartedOn = state.Date;
        relation.ChangeRelations(3);
        relation.ChangeTrust(5);
        relation.ChangeTension(-3);

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
            $"Trade proposal from {proposal.SourceCountry.Name} accepted",
            $"{proposal.SourceCountry.Name} and {proposal.TargetCountry.Name} " +
            "enter a trade agreement. Both countries begin receiving trade income this month.");
    }

    private static SimulationReport ProcessTributeUltimatumResponse(
        GameState state,
        RespondToDiplomaticProposalOrder order,
        Diplomacy.DiplomaticProposal proposal,
        Diplomacy.DiplomaticRelation relation)
    {
        if (!order.Accept)
        {
            proposal.Status = Diplomacy.DiplomaticProposalStatus.Rejected;
            relation.ChangeRelations(-6);
            relation.ChangeTrust(-8);
            relation.Tension = 100;

            var sourceToTarget = state.Relationships.GetOrCreate(
                proposal.SourceCountry.Ruler,
                proposal.TargetCountry.Ruler);
            sourceToTarget.ChangeOpinion(-15);
            sourceToTarget.ChangeTrust(-10);

            order.Status = OrderStatus.Completed;

            return new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{order.Country.Name} rejects {proposal.SourceCountry.Name}'s ultimatum",
                $"{order.Country.Ruler.FullName} rejects a demand for " +
                $"{proposal.DemandedPayment:N0}. Tension reaches a breaking point; " +
                "the foreign government may now choose war.");
        }

        var payment = TransferPayment(
            order.Country,
            proposal.SourceCountry,
            proposal.DemandedPayment);

        proposal.Status = Diplomacy.DiplomaticProposalStatus.Accepted;
        relation.ChangeRelations(-2);
        relation.ChangeTrust(-2);
        relation.ChangeTension(-30);

        order.Country.Ruler.Legitimacy -= 3;
        order.Country.Government.Stability -= 2;
        order.Country.PublicUnrest += 1;

        var targetToSource = state.Relationships.GetOrCreate(
            proposal.TargetCountry.Ruler,
            proposal.SourceCountry.Ruler);
        targetToSource.ChangeOpinion(-12);
        targetToSource.ChangeTrust(-8);
        targetToSource.ChangeFear(8);

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Diplomacy,
            $"{order.Country.Name} yields to {proposal.SourceCountry.Name}",
            $"{order.Country.Name} pays {payment:N0} under the ultimatum. " +
            "Immediate tension falls, but the concession damages the ruler's " +
            "domestic legitimacy and may require new debt.");
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

    private static void ApplyTaxPowerBaseReaction(
        Countries.Country country,
        decimal enactedChange)
    {
        if (enactedChange == 0)
            return;

        var percentagePoints = (double)(enactedChange * 100m);
        var ruler = country.Ruler;

        ruler.ChangePowerBaseStanding(
            PowerBaseType.Merchants,
            (int)Math.Round(-percentagePoints * 1.2, MidpointRounding.AwayFromZero));
        ruler.ChangePowerBaseStanding(
            PowerBaseType.Workers,
            (int)Math.Round(-percentagePoints * 0.9, MidpointRounding.AwayFromZero));
        ruler.ChangePowerBaseStanding(
            PowerBaseType.Peasantry,
            (int)Math.Round(-percentagePoints, MidpointRounding.AwayFromZero));
    }

    private static void ApplyBudgetPowerBaseReaction(
        Countries.Country country,
        decimal oldArmyFunding,
        decimal oldAdministrationFunding,
        decimal oldCourtFunding)
    {
        var ruler = country.Ruler;

        var militaryDelta = (int)Math.Round(
            (double)(country.ArmyFunding - oldArmyFunding) * 20,
            MidpointRounding.AwayFromZero);
        var bureaucracyDelta = (int)Math.Round(
            (double)(country.AdministrationFunding - oldAdministrationFunding) * 20,
            MidpointRounding.AwayFromZero);
        var courtDelta = (int)Math.Round(
            (double)(country.CourtFunding - oldCourtFunding) * 20,
            MidpointRounding.AwayFromZero);

        ruler.ChangePowerBaseStanding(PowerBaseType.Military, militaryDelta);
        ruler.ChangePowerBaseStanding(PowerBaseType.Bureaucracy, bureaucracyDelta);
        ruler.ChangePowerBaseStanding(PowerBaseType.Aristocracy, courtDelta);
        ruler.ChangePowerBaseStanding(
            PowerBaseType.RoyalFamily,
            (int)Math.Round(courtDelta * 0.5, MidpointRounding.AwayFromZero));
    }

    private static decimal MoveTowardsTarget(
        decimal current,
        decimal target,
        decimal implementationFactor)
    {
        return current + (target - current) * implementationFactor;
    }

    private static void ApplyAppointmentPowerBaseConsequences(
        Countries.Country country,
        Character ruler,
        Character candidate,
        Position position)
    {
        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
        {
            if (country.GetPowerBaseStrength(powerBase) < 40)
                continue;

            var candidateBacking = candidate.GetPowerBaseStanding(powerBase);

            if (candidateBacking < 65)
                continue;

            var representationGain = Math.Clamp(
                1 + (candidateBacking - 65) / 15,
                1,
                3);

            ruler.ChangePowerBaseStanding(powerBase, representationGain);
        }

        // Office gives a politician access, visibility and patronage of its own.
        // Placating a group by appointing its favourite can therefore create a
        // stronger future rival rather than being a free political concession.
        switch (position)
        {
            case Position.Marshal:
                candidate.ChangePowerBaseStanding(PowerBaseType.Military, 6);
                break;

            case Position.Treasurer:
                candidate.ChangePowerBaseStanding(PowerBaseType.Merchants, 4);
                candidate.ChangePowerBaseStanding(PowerBaseType.Bureaucracy, 4);
                break;

            case Position.Chancellor:
                candidate.ChangePowerBaseStanding(PowerBaseType.Bureaucracy, 4);
                candidate.ChangePowerBaseStanding(PowerBaseType.Aristocracy, 2);
                candidate.ChangePowerBaseStanding(PowerBaseType.RegionalElites, 2);
                candidate.ChangePowerBaseStanding(PowerBaseType.Party, 3);
                break;
        }
    }

    private static void ApplyDismissalConsequences(
        GameState state,
        Countries.Country country,
        Character ruler,
        Character dismissed)
    {
        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
        {
            if (country.GetPowerBaseStrength(powerBase) < 40)
                continue;

            var dismissedBacking = dismissed.GetPowerBaseStanding(powerBase);

            if (dismissedBacking < 65)
                continue;

            var politicalCost = Math.Clamp(
                1 + (dismissedBacking - 65) / 15,
                1,
                3);

            ruler.ChangePowerBaseStanding(powerBase, -politicalCost);
            dismissed.ChangePowerBaseStanding(powerBase, 1);
        }

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

    private static SimulationReport ProcessRequestReportOrder(
        GameState state,
        RequestReportOrder order)
    {
        if (!ReferenceEquals(order.Issuer, order.Country.Ruler))
        {
            order.Status = OrderStatus.Rejected;

            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Report request rejected",
                "Only the ruler may formally task an adviser with a report.");
        }

        var result = InformationSystem.QueueRequestedReport(
            state,
            order.Country,
            order.Recipient,
            order.Topic,
            order.SubjectCountry,
            order.RelatedCountry);

        order.Status = result.Accepted
            ? OrderStatus.Completed
            : result.Refused
                ? OrderStatus.Refused
                : OrderStatus.Rejected;

        return new SimulationReport(
            state.Date,
            ReportCategory.Personal,
            result.Title,
            result.Details);
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
