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

            if (relation.HasTradeAgreement ||
                HasPendingProposal(state, foreignCountry, playerCountry) ||
                HasRecentProposal(state, foreignCountry, playerCountry))
            {
                continue;
            }

            var chancellor =
                foreignCountry.GetOfficeHolder(Position.Chancellor);

            if (chancellor is null)
                continue;

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
        Countries.Country target)
    {
        return state.DiplomaticProposals.Any(proposal =>
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
