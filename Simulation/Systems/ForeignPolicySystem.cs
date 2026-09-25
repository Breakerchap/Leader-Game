using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class ForeignPolicySystem
{
    private const int ProposalLifetimeMonths = 3;
    private const int ProposalCooldownMonths = 12;

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        AdvancePendingProposals(state, reports);
        EscalateRejectedUltimata(state, reports);

        if (state.Date.Month is not (1 or 4 or 7 or 10))
            return reports;

        var playerCountry = state.Player.Country;

        foreach (var foreignCountry in state.Countries.Where(country =>
                     !ReferenceEquals(country, playerCountry) &&
                     country.IsNeighbor(playerCountry)))
        {
            var relation = state.Diplomacy.GetOrCreate(
                foreignCountry,
                playerCountry);

            if (AreAtWar(state, foreignCountry, playerCountry) ||
                HasPendingProposal(state, foreignCountry, playerCountry))
            {
                continue;
            }

            var chancellor =
                foreignCountry.GetOfficeHolder(Position.Chancellor);

            if (chancellor is null)
                continue;

            if (!relation.HasTradeAgreement &&
                ShouldIssueUltimatum(foreignCountry, playerCountry, relation) &&
                !HasRecentProposal(
                    state,
                    foreignCountry,
                    playerCountry,
                    DiplomaticProposalType.TributeUltimatum))
            {
                var demand = Math.Clamp(
                    playerCountry.Gdp * 0.003m,
                    50_000m,
                    300_000m);

                var ultimatum = new DiplomaticProposal
                {
                    Type = DiplomaticProposalType.TributeUltimatum,
                    SourceCountry = foreignCountry,
                    TargetCountry = playerCountry,
                    CreatedOn = state.Date,
                    DemandedPayment = demand
                };

                state.DiplomaticProposals.Add(ultimatum);

                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Diplomacy,
                    $"{foreignCountry.Name} issues an ultimatum",
                    $"{foreignCountry.Ruler.FullName}'s government demands " +
                    $"{demand:N0} from {playerCountry.Name}. Rejecting or ignoring " +
                    "the demand may lead to war."));

                continue;
            }

            if (relation.HasTradeAgreement ||
                HasRecentProposal(
                    state,
                    foreignCountry,
                    playerCountry,
                    DiplomaticProposalType.TradeAgreement))
            {
                continue;
            }

            var desirability =
                DiplomaticCalculations.GetTradeDesirabilityScore(
                    state,
                    foreignCountry,
                    playerCountry,
                    chancellor);

            if (desirability < 65)
                continue;

            var proposal = new DiplomaticProposal
            {
                Type = DiplomaticProposalType.TradeAgreement,
                SourceCountry = foreignCountry,
                TargetCountry = playerCountry,
                CreatedOn = state.Date
            };

            state.DiplomaticProposals.Add(proposal);

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{foreignCountry.Name} proposes a trade agreement",
                $"{foreignCountry.Ruler.FullName}'s government offers a trade agreement " +
                $"to {playerCountry.Name}. The proposal will not remain open indefinitely."));
        }

        return reports;
    }

    private static void AdvancePendingProposals(
        GameState state,
        List<SimulationReport> reports)
    {
        foreach (var proposal in state.DiplomaticProposals.Where(proposal =>
                     proposal.Status == DiplomaticProposalStatus.Pending))
        {
            proposal.MonthsOpen++;

            if (proposal.MonthsOpen < ProposalLifetimeMonths)
                continue;

            proposal.Status = DiplomaticProposalStatus.Expired;

            var relation = state.Diplomacy.GetOrCreate(
                proposal.SourceCountry,
                proposal.TargetCountry);

            if (proposal.Type == DiplomaticProposalType.TributeUltimatum)
            {
                relation.ChangeRelations(-5);
                relation.ChangeTrust(-5);
                relation.Tension = 100;

                if (ReferenceEquals(proposal.TargetCountry, state.Player.Country))
                {
                    reports.Add(new SimulationReport(
                        state.Date,
                        ReportCategory.Diplomacy,
                        $"Ultimatum from {proposal.SourceCountry.Name} expires",
                        $"{proposal.SourceCountry.Name} treats the unanswered demand " +
                        "as a refusal. Tension reaches a breaking point."));
                }

                continue;
            }

            relation.ChangeRelations(-1);
            relation.ChangeTrust(-2);

            if (ReferenceEquals(proposal.TargetCountry, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Diplomacy,
                    $"Proposal from {proposal.SourceCountry.Name} expires",
                    $"{proposal.SourceCountry.Name}'s diplomatic proposal expired without " +
                    "an answer. The silence slightly damages trust."));
            }
        }
    }

    private static void EscalateRejectedUltimata(
        GameState state,
        List<SimulationReport> reports)
    {
        var playerCountry = state.Player.Country;

        foreach (var proposal in state.DiplomaticProposals.Where(proposal =>
                     proposal.Type == DiplomaticProposalType.TributeUltimatum &&
                     proposal.Status is DiplomaticProposalStatus.Rejected
                         or DiplomaticProposalStatus.Expired &&
                     ReferenceEquals(proposal.TargetCountry, playerCountry) &&
                     MonthsBetween(proposal.CreatedOn, state.Date) <= 4))
        {
            var source = proposal.SourceCountry;
            var relation = state.Diplomacy.GetOrCreate(source, playerCountry);

            if (AreAtWar(state, source, playerCountry) ||
                relation.Tension < 95)
            {
                continue;
            }

            var leverage = CalculateMilitaryLeverage(source, playerCountry);

            if (leverage < 1.05 && source.Ruler.Ambition < 80)
                continue;

            Military.WarDeclarationService.Declare(
                state,
                source,
                playerCountry);

            proposal.Status = DiplomaticProposalStatus.EscalatedToWar;

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Military,
                $"{source.Name} declares war after its ultimatum fails",
                $"{source.Ruler.FullName}'s government responds to the failed " +
                $"ultimatum by declaring war on {playerCountry.Name}. The first " +
                "campaign month will begin next month."));
        }
    }

    private static bool ShouldIssueUltimatum(
        Countries.Country source,
        Countries.Country target,
        DiplomaticRelation relation)
    {
        if (relation.Relations > -55 ||
            relation.Tension < 75)
        {
            return false;
        }

        var leverage = CalculateMilitaryLeverage(source, target);

        return leverage >= 1.10 ||
               (leverage >= 1.0 && source.Ruler.Ambition >= 85);
    }

    private static double CalculateMilitaryLeverage(
        Countries.Country source,
        Countries.Country target)
    {
        var sourcePower =
            source.ArmySize *
            (0.50 + source.ArmyReadiness / 200.0) *
            (0.80 + (double)source.ArmyFunding * 0.20);

        var targetPower =
            target.ArmySize *
            (0.50 + target.ArmyReadiness / 200.0) *
            (0.80 + (double)target.ArmyFunding * 0.20);

        return sourcePower / Math.Max(1, targetPower);
    }

    private static bool AreAtWar(
        GameState state,
        Countries.Country first,
        Countries.Country second)
    {
        return state.Wars.Any(war =>
            war.Status == Military.WarStatus.Active &&
            war.IsParticipant(first) &&
            war.IsParticipant(second));
    }

    private static bool HasPendingProposal(
        GameState state,
        Countries.Country source,
        Countries.Country target)
    {
        return state.DiplomaticProposals.Any(proposal =>
            proposal.Status == DiplomaticProposalStatus.Pending &&
            ReferenceEquals(proposal.SourceCountry, source) &&
            ReferenceEquals(proposal.TargetCountry, target));
    }

    private static bool HasRecentProposal(
        GameState state,
        Countries.Country source,
        Countries.Country target,
        DiplomaticProposalType type)
    {
        return state.DiplomaticProposals.Any(proposal =>
            proposal.Type == type &&
            ReferenceEquals(proposal.SourceCountry, source) &&
            ReferenceEquals(proposal.TargetCountry, target) &&
            MonthsBetween(proposal.CreatedOn, state.Date) < ProposalCooldownMonths);
    }

    private static int MonthsBetween(GameDate start, GameDate end)
    {
        return (end.Year - start.Year) * 12 +
               (end.Month - start.Month);
    }
}
