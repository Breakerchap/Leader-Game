using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

/// <summary>
/// Scheduled republican elections. Elections use the same political actors and
/// constituencies as the rest of the simulation rather than a separate vote stat.
/// </summary>
internal static class ElectionSystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var country in state.Countries.Where(country =>
                     country.Government.HoldsScheduledElections &&
                     country.Ruler.IsAlive))
        {
            var government = country.Government;

            if (government.MonthsUntilElection <= 0)
            {
                government.MonthsUntilElection =
                    government.ElectionIntervalMonths;
            }

            government.MonthsUntilElection--;

            if (government.MonthsUntilElection <=
                government.ElectionCampaignMonths)
            {
                ApplyCampaignMomentum(state, country);
            }

            if (ReferenceEquals(country, state.Player.Country) &&
                government.MonthsUntilElection is 6 or 3 or 1)
            {
                reports.Add(CampaignReport(
                    state,
                    country,
                    government.MonthsUntilElection));
            }

            if (government.MonthsUntilElection > 0)
                continue;

            reports.Add(ResolveElection(state, country));

            government.MonthsUntilElection =
                government.ElectionIntervalMonths;
        }

        return reports;
    }

    public static IReadOnlyList<Character> GetCandidateField(
        GameState state,
        Country country)
    {
        var active = country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                character.Status == PoliticalStatus.Active)
            .ToList();

        if (active.Count == 0)
            return [];

        if (ReferenceEquals(country, state.Player.Country) &&
            state.Player.Lineage.Type == PoliticalLineageType.Party)
        {
            var nominee = active
                .Where(state.Player.Lineage.Contains)
                .OrderByDescending(candidate =>
                    GetBaseElectionScore(state, country, candidate))
                .FirstOrDefault();

            var challengers = active
                .Where(candidate =>
                    nominee is null ||
                    !state.Player.Lineage.Contains(candidate))
                .OrderByDescending(candidate =>
                    GetBaseElectionScore(state, country, candidate))
                .Take(nominee is null ? 3 : 2)
                .ToList();

            if (nominee is not null)
                challengers.Insert(0, nominee);

            return challengers;
        }

        return active
            .OrderByDescending(candidate =>
                GetBaseElectionScore(state, country, candidate))
            .Take(3)
            .ToList();
    }

    private static void ApplyCampaignMomentum(
        GameState state,
        Country country)
    {
        foreach (var candidate in GetCandidateField(state, country))
        {
            var campaignSkill =
                candidate.Competence * 0.55 +
                candidate.Ambition * 0.45;

            var noise =
                (state.Random.NextDouble() - 0.5) * 20.0;

            var momentum = campaignSkill + noise;

            if (momentum >= 78)
            {
                candidate.ChangePowerBaseStanding(
                    PowerBaseType.Party,
                    1);
            }
            else if (momentum <= 42)
            {
                candidate.ChangePowerBaseStanding(
                    PowerBaseType.Party,
                    -1);
            }
        }
    }

    private static SimulationReport CampaignReport(
        GameState state,
        Country country,
        int monthsRemaining)
    {
        var candidates = GetCandidateField(state, country);
        var names = FormatNames(candidates);

        var title = monthsRemaining switch
        {
            6 => $"{country.Name}'s election campaign begins",
            3 => $"{country.Name}'s election enters its final quarter",
            1 => $"{country.Name} votes next month",
            _ => $"{country.Name}'s election approaches"
        };

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            title,
            $"The constitutional election is {monthsRemaining} " +
            $"{(monthsRemaining == 1 ? "month" : "months")} away. " +
            $"The visible candidate field is {names}. " +
            "Political backing can still shift before the vote.");
    }

    private static SimulationReport ResolveElection(
        GameState state,
        Country country)
    {
        var candidates = GetCandidateField(state, country);

        if (candidates.Count == 0)
        {
            country.Government.Stability -= 5;

            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{country.Name}'s election cannot be completed",
                "No eligible political figure can take office. The failed constitutional process damages stability.");
        }

        var ranked = candidates
            .Select(candidate => new ElectionResult(
                candidate,
                GetBaseElectionScore(state, country, candidate) +
                (state.Random.NextDouble() - 0.5) * 16.0))
            .OrderByDescending(result => result.Score)
            .ToList();

        var winner = ranked[0].Candidate;
        var runnerUp = ranked.Count > 1
            ? ranked[1]
            : null;

        var oldRuler = country.Ruler;
        var margin = runnerUp is null
            ? 20.0
            : ranked[0].Score - runnerUp.Score;

        ApplyElectionConsequences(
            state,
            country,
            winner,
            oldRuler,
            ranked.Select(result => result.Candidate).ToList(),
            margin);

        var continuityText = BuildPlayerContinuityText(
            state,
            country,
            winner);

        var resultDescription = margin switch
        {
            < 3 => "an extremely narrow result",
            < 7 => "a narrow but clear result",
            < 13 => "a clear result",
            _ => "a decisive result"
        };

        var transferText = ReferenceEquals(winner, oldRuler)
            ? $"{winner.FullName} retains office"
            : $"{winner.FullName} replaces {oldRuler.FullName}";

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{winner.FullName} wins the {country.Name} election",
            $"{transferText} after {resultDescription}. " +
            $"{continuityText}");
    }

    private static void ApplyElectionConsequences(
        GameState state,
        Country country,
        Character winner,
        Character oldRuler,
        IReadOnlyList<Character> candidates,
        double margin)
    {
        foreach (var candidate in candidates)
        {
            candidate.ChangePowerBaseStanding(
                PowerBaseType.Party,
                ReferenceEquals(candidate, winner) ? 4 : -2);
        }

        if (!ReferenceEquals(winner, oldRuler))
        {
            winner.Position = null;
            oldRuler.Influence = Math.Max(
                25,
                oldRuler.Influence - 15);
            oldRuler.Legitimacy = Math.Max(
                0,
                oldRuler.Legitimacy - 6);
            country.Ruler = winner;
        }

        winner.Influence = 100;
        winner.Legitimacy = Math.Min(
            100,
            winner.Legitimacy + 6);

        if (margin < 3)
        {
            country.Government.Stability -= 2;
            country.PublicUnrest += 1.5;
        }
        else if (margin >= 13)
        {
            country.Government.Stability += 1;
        }

        if (!ReferenceEquals(country, state.Player.Country))
            return;

        if (state.Player.Lineage.Type != PoliticalLineageType.Party)
            return;

        if (state.Player.Lineage.Contains(winner))
        {
            state.Player.CurrentCharacter = winner;
            state.Player.ElectionsWon++;
            return;
        }

        state.Player.HasLost = true;
        state.Player.LossReason =
            $"{winner.FullName} won the election in {country.Name}, ending " +
            $"{state.Player.Lineage.Name}'s control of the government.";
    }

    private static string BuildPlayerContinuityText(
        GameState state,
        Country country,
        Character winner)
    {
        if (!ReferenceEquals(country, state.Player.Country) ||
            state.Player.Lineage.Type != PoliticalLineageType.Party)
        {
            return "The result changes the political balance of the republic.";
        }

        return state.Player.Lineage.Contains(winner)
            ? $"{state.Player.Lineage.Name} retains control of the government."
            : $"{state.Player.Lineage.Name} loses control of the government.";
    }

    private static double GetBaseElectionScore(
        GameState state,
        Country country,
        Character candidate)
    {
        var constituency =
            PoliticalCalculations.GetPowerBaseInfluence(
                country,
                candidate);

        var score =
            constituency * 0.55 +
            candidate.Influence * 0.20 +
            candidate.Competence * 0.10 +
            candidate.Ambition * 0.10 +
            candidate.Legitimacy * 0.05;

        if (!ReferenceEquals(candidate, country.Ruler))
            return score;

        score += 2.0;
        score +=
            (country.Government.Stability - 50) * 0.08;
        score +=
            (50 - country.PublicUnrest) * 0.05;

        if (country.LastMonthlyBalance > 0)
            score += 1.5;
        else if (country.LastMonthlyBalance < 0)
            score -= 1.5;

        return score;
    }

    private static string FormatNames(
        IReadOnlyList<Character> candidates)
    {
        if (candidates.Count == 0)
            return "unclear";

        if (candidates.Count == 1)
            return candidates[0].FullName;

        if (candidates.Count == 2)
        {
            return $"{candidates[0].FullName} and " +
                   $"{candidates[1].FullName}";
        }

        return string.Join(
                   ", ",
                   candidates
                       .Take(candidates.Count - 1)
                       .Select(candidate => candidate.FullName)) +
               $" and {candidates[^1].FullName}";
    }

    private sealed record ElectionResult(
        Character Candidate,
        double Score);
}
