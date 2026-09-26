using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class InstitutionSystem
{
    public static bool TrySubmitProposal(
        GameState state,
        ChangeTaxOrder order,
        out SimulationReport? report)
    {
        report = null;

        if (!RequiresApproval(order.Country, order))
            return false;

        if (HasPendingProposal(state, order.Country, LegislativeProposalType.TaxRate))
        {
            order.Status = OrderStatus.Rejected;
            report = new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Tax proposal already before the legislature",
                $"{BodyName(order.Country.Government.LegislativeBody)} is already considering a tax proposal.");
            return true;
        }

        state.LegislativeProposals.Add(new LegislativeProposal
        {
            Country = order.Country,
            Sponsor = order.Issuer,
            Drafter = order.Recipient,
            Type = LegislativeProposalType.TaxRate,
            CreatedOn = state.Date,
            TargetTaxRate = order.TargetTaxRate
        });

        order.Status = OrderStatus.Completed;
        report = new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"Tax proposal submitted to {BodyName(order.Country.Government.LegislativeBody)}",
            $"{order.Recipient.FullName} has drafted the requested tax change to {order.TargetTaxRate:P1}. " +
            "It is now awaiting formal institutional approval rather than taking effect immediately.");
        return true;
    }

    public static bool TrySubmitProposal(
        GameState state,
        SetBudgetOrder order,
        out SimulationReport? report)
    {
        report = null;

        if (!RequiresApproval(order.Country, order))
            return false;

        if (HasPendingProposal(state, order.Country, LegislativeProposalType.Budget))
        {
            order.Status = OrderStatus.Rejected;
            report = new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Budget proposal already before the legislature",
                $"{BodyName(order.Country.Government.LegislativeBody)} is already considering a budget proposal.");
            return true;
        }

        state.LegislativeProposals.Add(new LegislativeProposal
        {
            Country = order.Country,
            Sponsor = order.Issuer,
            Drafter = order.Recipient,
            Type = LegislativeProposalType.Budget,
            CreatedOn = state.Date,
            TargetArmyFunding = order.TargetArmyFunding,
            TargetAdministrationFunding = order.TargetAdministrationFunding,
            TargetCourtFunding = order.TargetCourtFunding
        });

        order.Status = OrderStatus.Completed;
        report = new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"Budget submitted to {BodyName(order.Country.Government.LegislativeBody)}",
            $"{order.Recipient.FullName} has drafted the requested spending settlement. " +
            "It now requires formal institutional approval before the funding changes take effect.");
        return true;
    }

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var proposal in state.LegislativeProposals.Where(p =>
                     p.Status == LegislativeProposalStatus.Pending).ToList())
        {
            // A proposal submitted during this same tick cannot also be voted
            // through instantly. Institutions create real time between intent
            // and enactment.
            if (proposal.CreatedOn == state.Date)
                continue;

            proposal.MonthsOpen++;
            ResolveVote(state, proposal, reports);
        }

        return reports;
    }

    public static string BodyName(LegislativeBodyType body) => body switch
    {
        LegislativeBodyType.RoyalCouncil => "Royal Council",
        LegislativeBodyType.Assembly => "Assembly",
        _ => "Executive"
    };

    public static string AuthorityDescription(Government government) =>
        government.LegislativeBody switch
        {
            LegislativeBodyType.Assembly =>
                "Taxation and budgets require Assembly approval. Political backing inside the institutions matters as much as issuing the directive.",
            LegislativeBodyType.RoyalCouncil =>
                "The Royal Council can block extraordinary taxation or extreme funding changes, while ordinary fiscal administration remains a crown prerogative.",
            _ =>
                "Fiscal policy can be enacted by executive decree, though advisers and political constituencies can still resist its implementation."
        };

    private static bool RequiresApproval(
        Countries.Country country,
        ChangeTaxOrder order)
    {
        return country.Government.LegislativeBody switch
        {
            LegislativeBodyType.Assembly => true,
            LegislativeBodyType.RoyalCouncil =>
                order.TargetTaxRate - country.TaxRate >= 0.10m,
            _ => false
        };
    }

    private static bool RequiresApproval(
        Countries.Country country,
        SetBudgetOrder order)
    {
        if (country.Government.LegislativeBody == LegislativeBodyType.Assembly)
            return true;

        if (country.Government.LegislativeBody != LegislativeBodyType.RoyalCouncil)
            return false;

        return IsExtraordinaryFunding(order.TargetArmyFunding) ||
               IsExtraordinaryFunding(order.TargetAdministrationFunding) ||
               IsExtraordinaryFunding(order.TargetCourtFunding);
    }

    private static bool IsExtraordinaryFunding(decimal target) =>
        target < 0.70m || target > 1.30m;

    private static bool HasPendingProposal(
        GameState state,
        Countries.Country country,
        LegislativeProposalType type) =>
        state.LegislativeProposals.Any(proposal =>
            proposal.Status == LegislativeProposalStatus.Pending &&
            proposal.Type == type &&
            ReferenceEquals(proposal.Country, country));

    private static void ResolveVote(
        GameState state,
        LegislativeProposal proposal,
        List<SimulationReport> reports)
    {
        var support = CalculateSupport(proposal);
        var passed = support >= 50;

        if (passed)
        {
            proposal.Status = LegislativeProposalStatus.Passed;
            Enact(proposal);

            if (ReferenceEquals(proposal.Country, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{BodyName(proposal.Country.Government.LegislativeBody)} approves {ProposalLabel(proposal)}",
                    $"{proposal.Sponsor.FullName}'s {ProposalLabel(proposal)} has cleared the formal lawmaking process and now takes effect."));
            }

            return;
        }

        proposal.Status = LegislativeProposalStatus.Rejected;
        proposal.Sponsor.Influence = Math.Max(0, proposal.Sponsor.Influence - 2);
        proposal.Country.Government.Stability -= 0.4;

        if (ReferenceEquals(proposal.Country, state.Player.Country))
        {
            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{BodyName(proposal.Country.Government.LegislativeBody)} rejects {ProposalLabel(proposal)}",
                $"{proposal.Sponsor.FullName} failed to assemble enough institutional support. " +
                "The existing policy remains in force, and the defeat slightly weakens the government's authority."));
        }
    }

    private static double CalculateSupport(LegislativeProposal proposal)
    {
        var country = proposal.Country;
        var ruler = proposal.Sponsor;
        var government = country.Government;

        var bases = government.LegislativeBody switch
        {
            LegislativeBodyType.Assembly => new (PowerBaseType Type, double Weight)[]
            {
                (PowerBaseType.Party, 2.0),
                (PowerBaseType.Merchants, 1.3),
                (PowerBaseType.Bureaucracy, 1.0),
                (PowerBaseType.RegionalElites, 0.9),
                (PowerBaseType.Workers, 0.8),
                (PowerBaseType.Peasantry, 0.5)
            },
            LegislativeBodyType.RoyalCouncil => new (PowerBaseType, double)[]
            {
                (PowerBaseType.Aristocracy, 2.0),
                (PowerBaseType.Clergy, 1.2),
                (PowerBaseType.RegionalElites, 1.2),
                (PowerBaseType.Military, 0.8),
                (PowerBaseType.RoyalFamily, 0.8)
            },
            _ => Array.Empty<(PowerBaseType, double)>()
        };

        var totalWeight = 0.0;
        var weightedSupport = 0.0;

        foreach (var (powerBase, baseWeight) in bases)
        {
            var structuralWeight =
                Math.Max(0.05, country.GetPowerBaseStrength(powerBase) / 100.0);
            var weight = baseWeight * structuralWeight;

            weightedSupport += ruler.GetPowerBaseStanding(powerBase) * weight;
            totalWeight += weight;
        }

        var constituencySupport =
            totalWeight > 0 ? weightedSupport / totalWeight : 50;

        var executiveLeverage =
            (100 - government.LegislativeIndependence) * 0.16;
        var stabilityEffect =
            (government.Stability - 50) * 0.14;
        var proposalEffect = ProposalSupportModifier(proposal);

        return constituencySupport +
               executiveLeverage +
               stabilityEffect +
               proposalEffect;
    }

    private static double ProposalSupportModifier(
        LegislativeProposal proposal)
    {
        var country = proposal.Country;

        if (proposal.Type == LegislativeProposalType.TaxRate &&
            proposal.TargetTaxRate is { } targetTax)
        {
            var percentagePointChange =
                (double)((targetTax - country.TaxRate) * 100m);

            // Tax rises are intrinsically harder to pass. Cuts are easier, but
            // a serious deficit makes generosity less institutionally credible.
            var modifier = percentagePointChange > 0
                ? -percentagePointChange * 1.25
                : -percentagePointChange * 0.45;

            if (country.LastMonthlyBalance < 0)
                modifier += Math.Min(7, (double)(-country.LastMonthlyBalance /
                    Math.Max(1m, country.Gdp / 1500m)));

            return modifier;
        }

        if (proposal.Type == LegislativeProposalType.Budget)
        {
            var armyDelta =
                (double)((proposal.TargetArmyFunding ?? country.ArmyFunding) -
                         country.ArmyFunding);
            var administrationDelta =
                (double)((proposal.TargetAdministrationFunding ??
                          country.AdministrationFunding) -
                         country.AdministrationFunding);
            var courtDelta =
                (double)((proposal.TargetCourtFunding ?? country.CourtFunding) -
                         country.CourtFunding);

            var constituencyFit =
                armyDelta * (country.Ruler.GetPowerBaseStanding(PowerBaseType.Military) - 50) * 0.22 +
                administrationDelta * (country.Ruler.GetPowerBaseStanding(PowerBaseType.Bureaucracy) - 50) * 0.22 +
                courtDelta * (country.Ruler.GetPowerBaseStanding(PowerBaseType.Aristocracy) - 50) * 0.18;

            var spendingExpansion =
                Math.Max(0, armyDelta) +
                Math.Max(0, administrationDelta) +
                Math.Max(0, courtDelta);

            var fiscalPenalty =
                country.LastMonthlyBalance < 0
                    ? spendingExpansion * 7
                    : 0;

            return constituencyFit - fiscalPenalty;
        }

        return 0;
    }

    private static void Enact(LegislativeProposal proposal)
    {
        var country = proposal.Country;

        if (proposal.Type == LegislativeProposalType.TaxRate &&
            proposal.TargetTaxRate is { } targetTax)
        {
            var oldTax = country.TaxRate;
            country.TaxRate = targetTax;
            var change = country.TaxRate - oldTax;
            var points = Math.Abs((double)(change * 100m));

            if (change > 0)
            {
                country.PublicUnrest += points;
                country.Government.Stability -= points * 0.6;
            }
            else if (change < 0)
            {
                country.PublicUnrest -= points * 0.6;
                country.Government.Stability += points * 0.3;
            }

            ApplyTaxReaction(country, change);
            return;
        }

        if (proposal.Type == LegislativeProposalType.Budget)
        {
            var oldArmy = country.ArmyFunding;
            var oldAdministration = country.AdministrationFunding;
            var oldCourt = country.CourtFunding;

            if (proposal.TargetArmyFunding is { } army)
                country.ArmyFunding = army;
            if (proposal.TargetAdministrationFunding is { } administration)
                country.AdministrationFunding = administration;
            if (proposal.TargetCourtFunding is { } court)
                country.CourtFunding = court;

            ApplyBudgetReaction(
                country,
                oldArmy,
                oldAdministration,
                oldCourt);
        }
    }

    private static void ApplyTaxReaction(
        Countries.Country country,
        decimal change)
    {
        if (change == 0)
            return;

        var points = (double)(change * 100m);
        var ruler = country.Ruler;

        ruler.ChangePowerBaseStanding(
            PowerBaseType.Merchants,
            (int)Math.Round(-points * 1.2, MidpointRounding.AwayFromZero));
        ruler.ChangePowerBaseStanding(
            PowerBaseType.Workers,
            (int)Math.Round(-points * 0.9, MidpointRounding.AwayFromZero));
        ruler.ChangePowerBaseStanding(
            PowerBaseType.Peasantry,
            (int)Math.Round(-points, MidpointRounding.AwayFromZero));
    }

    private static void ApplyBudgetReaction(
        Countries.Country country,
        decimal oldArmy,
        decimal oldAdministration,
        decimal oldCourt)
    {
        var ruler = country.Ruler;

        var militaryDelta = (int)Math.Round(
            (double)(country.ArmyFunding - oldArmy) * 20,
            MidpointRounding.AwayFromZero);
        var bureaucracyDelta = (int)Math.Round(
            (double)(country.AdministrationFunding - oldAdministration) * 20,
            MidpointRounding.AwayFromZero);
        var courtDelta = (int)Math.Round(
            (double)(country.CourtFunding - oldCourt) * 20,
            MidpointRounding.AwayFromZero);

        ruler.ChangePowerBaseStanding(PowerBaseType.Military, militaryDelta);
        ruler.ChangePowerBaseStanding(PowerBaseType.Bureaucracy, bureaucracyDelta);
        ruler.ChangePowerBaseStanding(PowerBaseType.Aristocracy, courtDelta);
        ruler.ChangePowerBaseStanding(
            PowerBaseType.RoyalFamily,
            (int)Math.Round(courtDelta * 0.5, MidpointRounding.AwayFromZero));
    }

    private static string ProposalLabel(LegislativeProposal proposal) =>
        proposal.Type switch
        {
            LegislativeProposalType.TaxRate => "tax proposal",
            LegislativeProposalType.Budget => "budget",
            _ => "proposal"
        };
}
