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

            var atWar = state.Wars.Any(war =>
                war.Status == Military.WarStatus.Active &&
                war.IsParticipant(foreignCountry) &&
                war.IsParticipant(playerCountry));

            if (atWar ||
                HasPendingProposal(state, foreignCountry, playerCountry))
            {
                continue;
            }

            var chancellor =
                foreignCountry.GetOfficeHolder(Position.Chancellor);

            if (chancellor is null)
                continue;

            DiplomaticProposalType? proposedType = null;

            if (!relation.HasTradeAgreement &&
                !HasRecentProposal(
                    state,
                    foreignCountry,
                    playerCountry,
                    DiplomaticProposalType.TradeAgreement))
            {
                var tradeDesirability =
                    DiplomaticCalculations.GetTradeDesirabilityScore(
                        state,
                        foreignCountry,
                        playerCountry,
                        chancellor);

                if (tradeDesirability >= 65)
                    proposedType = DiplomaticProposalType.TradeAgreement;
            }

            if (proposedType is null &&
                !relation.HasNonAggressionPact &&
                !HasRecentProposal(
                    state,
                    foreignCountry,
                    playerCountry,
                    DiplomaticProposalType.NonAggressionPact))
            {
                var pactDesirability =
                    DiplomaticCalculations.GetNonAggressionDesirabilityScore(
                        state,
                        foreignCountry,
                        playerCountry,
                        chancellor);

                if (pactDesirability >= 65 &&
                    (relation.Tension >= 20 ||
                     relation.BorderDisputeSeverity >= 20))
                {
                    proposedType = DiplomaticProposalType.NonAggressionPact;
                }
            }

            if (proposedType is null)
                continue;

            var proposal = new DiplomaticProposal
            {
                Type = proposedType.Value,
                SourceCountry = foreignCountry,
                TargetCountry = playerCountry,
                CreatedOn = state.Date
            };

            state.DiplomaticProposals.Add(proposal);

            var proposalName = proposedType == DiplomaticProposalType.TradeAgreement
                ? "trade agreement"
                : "non-aggression pact";

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Diplomacy,
                $"{foreignCountry.Name} proposes a {proposalName}",
                $"{foreignCountry.Ruler.FullName}'s government offers a {proposalName} " +
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

            if (proposal.Type == DiplomaticProposalType.NonAggressionPact)
                relation.ChangeTension(1);

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
