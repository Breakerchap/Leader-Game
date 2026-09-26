using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

/// <summary>
/// Maintains the player's political continuity separately from possession of
/// government office. Losing an election, succession or coup can push the
/// lineage into opposition without immediately ending the campaign.
/// </summary>
internal static class PlayerContinuitySystem
{
    private const double LowViabilityThreshold = 28;
    private const int LowViabilityGraceMonths = 18;

    public static IEnumerable<SimulationReport> ResolveLeadership(GameState state)
    {
        if (state.Player.HasLost)
            return [];

        var player = state.Player;
        var current = player.CurrentCharacter;

        if (current.IsAlive &&
            player.Lineage.Contains(current))
        {
            if (player.IsInPower &&
                !ReferenceEquals(current, player.Country.Ruler))
            {
                var previous = current;
                player.CurrentCharacter = player.Country.Ruler;

                return
                [
                    new SimulationReport(
                        state.Date,
                        ReportCategory.Personal,
                        $"{player.Country.Ruler.FullName} assumes lineage leadership",
                        $"{previous.FullName} no longer heads {player.Lineage.Name}. " +
                        $"Because {player.Country.Ruler.FullName} now rules {player.Country.Name} " +
                        "for the lineage, political control passes to the new ruler.")
                ];
            }

            return [];
        }

        var successor = FindLineageLeader(state);

        if (successor is null)
        {
            player.HasLost = true;
            player.LossReason =
                $"{player.Lineage.Name} has no living member able to continue its political leadership.";

            return
            [
                new SimulationReport(
                    state.Date,
                    ReportCategory.System,
                    "Political lineage ends",
                    player.LossReason)
            ];
        }

        var predecessorName = current.FullName;
        player.CurrentCharacter = successor;

        return
        [
            new SimulationReport(
                state.Date,
                ReportCategory.Personal,
                $"{successor.FullName} takes leadership of {player.Lineage.Name}",
                $"{predecessorName} can no longer lead the lineage. " +
                $"{successor.FullName} becomes its political standard-bearer" +
                (player.IsInPower
                    ? $" and continues control of {player.Country.Name}."
                    : " while the struggle to regain national power continues."))
        ];
    }

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        if (state.Player.HasLost || state.Player.HasWon)
            return [];

        var reports = new List<SimulationReport>();
        reports.AddRange(ResolveLeadership(state));

        if (state.Player.HasLost)
            return reports;

        var player = state.Player;

        if (player.IsInPower)
        {
            var returningToPower = player.MonthsOutOfPower > 0;

            player.MonthsOutOfPower = 0;
            player.ConsecutiveLowViabilityMonths = 0;
            player.PoliticalViability = 100;

            if (!ReferenceEquals(
                    player.CurrentCharacter,
                    player.Country.Ruler) &&
                player.Lineage.Contains(player.Country.Ruler))
            {
                player.CurrentCharacter = player.Country.Ruler;
            }

            if (returningToPower)
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{player.Lineage.Name} returns to power",
                    $"{player.CurrentCharacter.FullName} now controls the government of " +
                    $"{player.Country.Name}. The campaign continues, but the restored " +
                    "government still has to prove that the return is durable."));
            }

            return reports;
        }

        player.MonthsOutOfPower++;

        var assessment = AssessViability(state);
        player.PoliticalViability = assessment.Score;

        if (assessment.Score < LowViabilityThreshold &&
            !assessment.HasNearTermPathToPower)
        {
            player.ConsecutiveLowViabilityMonths++;
        }
        else
        {
            player.ConsecutiveLowViabilityMonths = 0;
        }

        if (player.ConsecutiveLowViabilityMonths >= LowViabilityGraceMonths)
        {
            player.HasLost = true;
            player.LossReason =
                $"{player.Lineage.Name} has spent {player.MonthsOutOfPower} months outside power " +
                "without meaningful political support or a credible route back to government.";

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.System,
                "Political relevance exhausted",
                player.LossReason));

            return reports;
        }

        if (player.MonthsOutOfPower is 1 or 6 or 12 ||
            player.ConsecutiveLowViabilityMonths == 12)
        {
            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                player.MonthsOutOfPower == 1
                    ? $"{player.Lineage.Name} enters opposition"
                    : $"{player.Lineage.Name} remains outside government",
                DescribeAssessment(player, assessment)));
        }

        return reports;
    }

    private static PoliticalViabilityAssessment AssessViability(
        GameState state)
    {
        var player = state.Player;
        var country = player.Country;
        var living = player.Lineage.Members
            .Where(member => member.IsAlive)
            .ToList();
        var active = living
            .Where(member => member.IsPoliticallyActive)
            .ToList();

        if (living.Count == 0)
            return new PoliticalViabilityAssessment(0, false, 0);

        if (active.Count == 0)
        {
            var residualInfluence = living.Max(member => member.Influence) * 0.10;

            return new PoliticalViabilityAssessment(
                residualInfluence,
                false,
                0);
        }

        var strongestMember = active.Max(member =>
        {
            var backing =
                PoliticalCalculations.GetPowerBaseInfluence(country, member);

            return
                member.Influence * 0.35 +
                backing * 0.40 +
                member.Legitimacy * 0.15 +
                member.Ambition * 0.10;
        });

        var score = strongestMember * 0.65;
        score += Math.Min(10, Math.Max(0, active.Count - 1) * 3);

        var hasNearTermPath = false;

        var bloc = state.PoliticalBlocs
            .Where(candidate =>
                candidate.IsActive &&
                ReferenceEquals(candidate.Country, country) &&
                player.Lineage.Contains(candidate.Leader))
            .OrderByDescending(candidate => candidate.Cohesion)
            .FirstOrDefault();

        if (bloc is not null)
        {
            score += Math.Min(13, 5 + bloc.Cohesion * 0.08);

            if (bloc.Cohesion >= 65)
                hasNearTermPath = true;
        }

        var plot = state.Plots
            .Where(candidate =>
                !candidate.IsResolved &&
                ReferenceEquals(candidate.Country, country) &&
                player.Lineage.Contains(candidate.Instigator))
            .OrderByDescending(candidate => candidate.Progress)
            .FirstOrDefault();

        if (plot is not null)
        {
            score += Math.Min(12, plot.Progress * 0.12);

            if (plot.Progress >= 60)
                hasNearTermPath = true;
        }

        if (player.Lineage.Type == PoliticalLineageType.Party &&
            country.Government.HoldsScheduledElections)
        {
            var nominee = ElectionSystem.GetPlayerLineageNominee(
                state,
                country);

            if (nominee is not null)
            {
                var monthsUntilElection =
                    country.Government.MonthsUntilElection <= 0
                        ? country.Government.ElectionIntervalMonths
                        : country.Government.MonthsUntilElection;
                var backing =
                    PoliticalCalculations.GetPowerBaseInfluence(
                        country,
                        nominee);
                var candidateStrength =
                    nominee.Influence * 0.45 +
                    backing * 0.55;

                score += Math.Clamp(
                    (24 - monthsUntilElection) * 0.5,
                    0,
                    12);

                if (monthsUntilElection <= 18 &&
                    candidateStrength >= 35)
                {
                    hasNearTermPath = true;
                }
            }
        }

        if (player.Lineage.Type == PoliticalLineageType.Dynasty)
        {
            var successionIndex = country.SuccessionOrder.FindIndex(
                candidate =>
                    candidate.IsPoliticallyActive &&
                    player.Lineage.Contains(candidate));

            if (successionIndex >= 0)
            {
                score += Math.Max(3, 16 - successionIndex * 5);

                if (successionIndex <= 1)
                    hasNearTermPath = true;
            }
        }

        if (active.Any(member =>
                PoliticalCalculations.GetThreatScore(
                    state,
                    country,
                    member) >= 65))
        {
            hasNearTermPath = true;
        }

        var regimeOpportunity =
            (100 - country.Government.Stability) * 0.05 +
            country.PublicUnrest * 0.03;
        score += Math.Min(8, regimeOpportunity);

        return new PoliticalViabilityAssessment(
            Math.Clamp(score, 0, 100),
            hasNearTermPath,
            active.Count);
    }

    private static Character? FindLineageLeader(GameState state)
    {
        var player = state.Player;

        if (player.Country.Ruler.IsAlive &&
            player.Lineage.Contains(player.Country.Ruler))
        {
            return player.Country.Ruler;
        }

        var active = player.Lineage.Members
            .Where(member => member.IsPoliticallyActive)
            .OrderByDescending(member => LeadershipScore(
                player.Country,
                member))
            .FirstOrDefault();

        if (active is not null)
            return active;

        return player.Lineage.Members
            .Where(member => member.IsAlive)
            .OrderByDescending(member => member.Influence)
            .ThenByDescending(member => member.Legitimacy)
            .FirstOrDefault();
    }

    private static double LeadershipScore(
        Countries.Country country,
        Character member)
    {
        return
            member.Influence * 0.40 +
            member.Legitimacy * 0.30 +
            PoliticalCalculations.GetPowerBaseInfluence(
                country,
                member) * 0.30;
    }

    private static string DescribeAssessment(
        PlayerState player,
        PoliticalViabilityAssessment assessment)
    {
        if (assessment.HasNearTermPathToPower)
        {
            return $"{player.Lineage.Name} is out of office, but it still has a credible " +
                   "near-term route back to power. Losing government has not ended the campaign.";
        }

        if (assessment.Score >= 55)
        {
            return $"{player.Lineage.Name} remains a major political force despite losing office. " +
                   "Its supporters and leading figures are strong enough that a return to power remains plausible.";
        }

        if (assessment.Score >= LowViabilityThreshold)
        {
            return $"{player.Lineage.Name} is weakened but still politically relevant. " +
                   "It needs to rebuild support or create a clearer path back to government.";
        }

        return $"{player.Lineage.Name} is becoming politically marginal. " +
               $"With only {assessment.ActiveMembers} active lineage member(s) and no clear route " +
               "back to power, prolonged weakness could eventually end the campaign.";
    }

    private sealed record PoliticalViabilityAssessment(
        double Score,
        bool HasNearTermPathToPower,
        int ActiveMembers);
}
