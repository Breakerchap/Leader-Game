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

        reports.AddRange(ProcessGoverningPromises(state));

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
                EnsureAiCampaignPromise(state, country);
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

    public static ElectionPromise? GetActivePromise(
        GameState state,
        Country country)
    {
        return state.ElectionPromises
            .Where(promise =>
                ReferenceEquals(promise.Country, country) &&
                promise.Status is
                    ElectionPromiseStatus.Campaigning or
                    ElectionPromiseStatus.AwaitingFulfilment)
            .OrderByDescending(promise => promise.MadeOn.Year)
            .ThenByDescending(promise => promise.MadeOn.Month)
            .FirstOrDefault();
    }

    public static SimulationReport MakePlayerCampaignPromise(
        GameState state,
        ElectionPromiseType type)
    {
        var country = state.Player.Country;

        if (!country.Government.HoldsScheduledElections ||
            country.Government.MonthsUntilElection >
                country.Government.ElectionCampaignMonths ||
            country.Government.MonthsUntilElection <= 0)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                "No active election campaign",
                "A public campaign promise can only be made during the official campaign period.");
        }

        if (GetActivePromise(state, country) is not null)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                "Campaign promise already made",
                "The party has already made its principal public commitment for this election.");
        }

        var nominee = GetPlayerLineageNominee(state, country);

        if (nominee is null)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                "No party nominee available",
                "The political lineage has no active candidate able to make an electoral commitment.");
        }

        var promise = CreatePromise(
            state,
            country,
            nominee,
            type);

        state.ElectionPromises.Add(promise);
        ApplyCampaignPromiseSupport(promise);

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            IsCouncilElection(country)
                ? $"{nominee.FullName} makes an electoral pledge"
                : $"{nominee.FullName} makes an election promise",
            DescribePromise(promise) +
            (IsCouncilElection(country)
                ? " The pledge reshapes support among the factions and interests represented inside the council."
                : " The commitment immediately reshapes which constituencies see the candidate as representing their interests."));
    }

    public static string DescribePromise(ElectionPromise promise)
    {
        return promise.Type switch
        {
            ElectionPromiseType.TaxRelief =>
                $"Promise to reduce the tax rate to {promise.TargetValue:P0} or lower.",
            ElectionPromiseType.AdministrativeInvestment =>
                $"Promise to raise administration funding to at least {promise.TargetValue:P0}.",
            ElectionPromiseType.MilitaryInvestment =>
                $"Promise to raise army funding to at least {promise.TargetValue:P0}.",
            ElectionPromiseType.CoalitionPatronage =>
                $"Promise to raise court and patronage funding to at least {promise.TargetValue:P0}.",
            _ => "Public campaign commitment."
        };
    }

    public static Character? GetPlayerLineageNominee(
        GameState state,
        Country country)
    {
        if (!ReferenceEquals(country, state.Player.Country) ||
            state.Player.Lineage.Type != PoliticalLineageType.Party)
        {
            return null;
        }

        return country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                state.Player.Lineage.Contains(character))
            .OrderByDescending(candidate =>
                GetBaseElectionScore(state, country, candidate))
            .FirstOrDefault();
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
            var nominee = GetPlayerLineageNominee(
                state,
                country);

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

        var title = IsCouncilElection(country)
            ? monthsRemaining switch
            {
                6 => $"{country.Name}'s council election manoeuvring begins",
                3 => $"{country.Name}'s council election enters its final quarter",
                1 => $"{InstitutionSystem.BodyName(country.Government.LegislativeBody)} elects next month",
                _ => $"{country.Name}'s council election approaches"
            }
            : monthsRemaining switch
            {
                6 => $"{country.Name}'s election campaign begins",
                3 => $"{country.Name}'s election enters its final quarter",
                1 => $"{country.Name} votes next month",
                _ => $"{country.Name}'s election approaches"
            };

        var nominee = GetPlayerLineageNominee(
            state,
            country);

        var lineageText = nominee is null
            ? string.Empty
            : $" {nominee.FullName} is the {state.Player.Lineage.Name} nominee.";

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            title,
            $"{(IsCouncilElection(country) ? "The council election" : "The constitutional election")} is {monthsRemaining} " +
            $"{(monthsRemaining == 1 ? "month" : "months")} away. " +
            $"The visible candidate field is {names}." +
            lineageText +
            " Political backing can still shift before the vote.");
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
            runnerUp?.Candidate,
            margin);

        ResolveCampaignPromiseAfterElection(
            state,
            country,
            winner);

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
            IsCouncilElection(country)
                ? $"{winner.FullName} is elected by the {InstitutionSystem.BodyName(country.Government.LegislativeBody)}"
                : $"{winner.FullName} wins the {country.Name} election",
            $"{transferText} after {resultDescription}. " +
            $"{continuityText}");
    }

    private static void ApplyElectionConsequences(
        GameState state,
        Country country,
        Character winner,
        Character oldRuler,
        IReadOnlyList<Character> candidates,
        Character? runnerUp,
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

        if (runnerUp is not null)
        {
            var loserToWinner = state.Relationships.GetOrCreate(
                runnerUp,
                winner);

            loserToWinner.ChangeOpinion(
                margin < 3
                    ? -12
                    : margin < 7
                        ? -8
                        : -4);
            loserToWinner.ChangeTrust(
                margin < 7
                    ? -5
                    : -2);

            if (margin < 7)
            {
                runnerUp.Influence = Math.Min(
                    100,
                    runnerUp.Influence + 4);
                runnerUp.ChangePowerBaseStanding(
                    PowerBaseType.Party,
                    3);
            }
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

        // Losing an election moves the lineage into opposition. It only becomes
        // a campaign defeat later if the lineage remains politically irrelevant
        // with no credible route back to power.
        var oppositionLeader = GetPlayerLineageNominee(state, country);

        if (oppositionLeader is not null)
            state.Player.CurrentCharacter = oppositionLeader;
    }

    private static ElectionPromise CreatePromise(
        GameState state,
        Country country,
        Character candidate,
        ElectionPromiseType type)
    {
        var target = type switch
        {
            ElectionPromiseType.TaxRelief =>
                Math.Max(0.02m, country.TaxRate - 0.02m),
            ElectionPromiseType.AdministrativeInvestment =>
                Math.Max(1.15m, country.AdministrationFunding + 0.10m),
            ElectionPromiseType.MilitaryInvestment =>
                Math.Max(1.20m, country.ArmyFunding + 0.10m),
            ElectionPromiseType.CoalitionPatronage =>
                Math.Max(1.15m, country.CourtFunding + 0.10m),
            _ => 1m
        };

        return new ElectionPromise
        {
            Country = country,
            Candidate = candidate,
            Type = type,
            TargetValue = Math.Min(1.5m, target),
            MadeOn = state.Date
        };
    }

    private static void ApplyCampaignPromiseSupport(
        ElectionPromise promise)
    {
        var candidate = promise.Candidate;

        if (IsCouncilElection(promise.Country) &&
            promise.Type == ElectionPromiseType.TaxRelief)
        {
            candidate.ChangePowerBaseStanding(PowerBaseType.Merchants, 5);
            candidate.ChangePowerBaseStanding(PowerBaseType.Party, 3);
            candidate.ChangePowerBaseStanding(PowerBaseType.Bureaucracy, 1);
            return;
        }

        switch (promise.Type)
        {
            case ElectionPromiseType.TaxRelief:
                candidate.ChangePowerBaseStanding(PowerBaseType.Merchants, 4);
                candidate.ChangePowerBaseStanding(PowerBaseType.Workers, 3);
                candidate.ChangePowerBaseStanding(PowerBaseType.Peasantry, 3);
                break;

            case ElectionPromiseType.AdministrativeInvestment:
                candidate.ChangePowerBaseStanding(PowerBaseType.Bureaucracy, 5);
                candidate.ChangePowerBaseStanding(PowerBaseType.Party, 2);
                candidate.ChangePowerBaseStanding(PowerBaseType.Merchants, 1);
                break;

            case ElectionPromiseType.MilitaryInvestment:
                candidate.ChangePowerBaseStanding(PowerBaseType.Military, 6);
                candidate.ChangePowerBaseStanding(PowerBaseType.RegionalElites, 2);
                break;

            case ElectionPromiseType.CoalitionPatronage:
                candidate.ChangePowerBaseStanding(PowerBaseType.Party, 4);
                candidate.ChangePowerBaseStanding(PowerBaseType.Aristocracy, 3);
                candidate.ChangePowerBaseStanding(PowerBaseType.RegionalElites, 3);
                break;
        }
    }

    private static void EnsureAiCampaignPromise(
        GameState state,
        Country country)
    {
        if (ReferenceEquals(country, state.Player.Country) ||
            GetActivePromise(state, country) is not null)
        {
            return;
        }

        var candidate = GetCandidateField(state, country)
            .FirstOrDefault();

        if (candidate is null)
            return;

        var type =
            country.PublicUnrest >= 45 && country.TaxRate > 0.07m
                ? ElectionPromiseType.TaxRelief
                : country.AdministrativeEfficiency < 0.70m
                    ? ElectionPromiseType.AdministrativeInvestment
                    : country.ArmyReadiness < 55
                        ? ElectionPromiseType.MilitaryInvestment
                        : ElectionPromiseType.CoalitionPatronage;

        var promise = CreatePromise(
            state,
            country,
            candidate,
            type);

        state.ElectionPromises.Add(promise);
        ApplyCampaignPromiseSupport(promise);
    }

    private static void ResolveCampaignPromiseAfterElection(
        GameState state,
        Country country,
        Character winner)
    {
        var promise = state.ElectionPromises
            .Where(candidate =>
                ReferenceEquals(candidate.Country, country) &&
                candidate.Status == ElectionPromiseStatus.Campaigning)
            .OrderByDescending(candidate => candidate.MadeOn.Year)
            .ThenByDescending(candidate => candidate.MadeOn.Month)
            .FirstOrDefault();

        if (promise is null)
            return;

        if (ReferenceEquals(promise.Candidate, winner))
        {
            promise.Status = ElectionPromiseStatus.AwaitingFulfilment;
            promise.MonthsSinceElection = 0;
        }
        else
        {
            promise.Status = ElectionPromiseStatus.Lapsed;
        }
    }

    private static IEnumerable<SimulationReport> ProcessGoverningPromises(
        GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var promise in state.ElectionPromises.Where(promise =>
                     promise.Status == ElectionPromiseStatus.AwaitingFulfilment))
        {
            if (!promise.Candidate.IsPoliticallyActive ||
                !ReferenceEquals(promise.Country.Ruler, promise.Candidate))
            {
                promise.Status = ElectionPromiseStatus.Lapsed;
                continue;
            }

            if (IsPromiseFulfilled(promise))
            {
                promise.Status = ElectionPromiseStatus.Fulfilled;
                ApplyFulfilledPromiseEffects(promise);

                if (ReferenceEquals(promise.Country, state.Player.Country))
                {
                    reports.Add(new SimulationReport(
                        state.Date,
                        ReportCategory.Politics,
                        $"{promise.Candidate.FullName} fulfils an election promise",
                        DescribePromise(promise) +
                        " Delivering the commitment strengthens the government's credibility with the constituencies that backed it."));
                }

                continue;
            }

            promise.MonthsSinceElection++;

            if (promise.MonthsSinceElection < 6)
                continue;

            promise.Status = ElectionPromiseStatus.Broken;
            ApplyBrokenPromiseEffects(promise);

            if (ReferenceEquals(promise.Country, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{promise.Candidate.FullName} breaks an election promise",
                    DescribePromise(promise) +
                    " Six months have passed without delivery. The broken commitment damages political credibility and creates fresh unrest."));
            }
        }

        return reports;
    }

    private static bool IsPromiseFulfilled(
        ElectionPromise promise)
    {
        return promise.Type switch
        {
            ElectionPromiseType.TaxRelief =>
                promise.Country.TaxRate <= promise.TargetValue,
            ElectionPromiseType.AdministrativeInvestment =>
                promise.Country.AdministrationFunding >= promise.TargetValue,
            ElectionPromiseType.MilitaryInvestment =>
                promise.Country.ArmyFunding >= promise.TargetValue,
            ElectionPromiseType.CoalitionPatronage =>
                promise.Country.CourtFunding >= promise.TargetValue,
            _ => false
        };
    }

    private static void ApplyFulfilledPromiseEffects(
        ElectionPromise promise)
    {
        var candidate = promise.Candidate;

        foreach (var powerBase in PromiseConstituencies(promise))
            candidate.ChangePowerBaseStanding(powerBase, 4);

        candidate.Legitimacy = Math.Min(
            100,
            candidate.Legitimacy + 3);
        promise.Country.Government.Stability += 1.5;
    }

    private static void ApplyBrokenPromiseEffects(
        ElectionPromise promise)
    {
        var candidate = promise.Candidate;

        foreach (var powerBase in PromiseConstituencies(promise))
            candidate.ChangePowerBaseStanding(powerBase, -8);

        candidate.Legitimacy = Math.Max(
            0,
            candidate.Legitimacy - 6);
        promise.Country.Government.Stability -= 3;
        promise.Country.PublicUnrest += 3;
    }

    private static IEnumerable<PowerBaseType> PromiseConstituencies(
        ElectionPromise promise)
    {
        if (IsCouncilElection(promise.Country) &&
            promise.Type == ElectionPromiseType.TaxRelief)
        {
            return
            [
                PowerBaseType.Merchants,
                PowerBaseType.Party,
                PowerBaseType.Bureaucracy
            ];
        }

        return promise.Type switch
        {
            ElectionPromiseType.TaxRelief =>
                [PowerBaseType.Merchants, PowerBaseType.Workers, PowerBaseType.Peasantry],
            ElectionPromiseType.AdministrativeInvestment =>
                [PowerBaseType.Bureaucracy, PowerBaseType.Party],
            ElectionPromiseType.MilitaryInvestment =>
                [PowerBaseType.Military, PowerBaseType.RegionalElites],
            ElectionPromiseType.CoalitionPatronage =>
                [PowerBaseType.Party, PowerBaseType.Aristocracy, PowerBaseType.RegionalElites],
            _ => []
        };
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
            : $"{state.Player.Lineage.Name} moves into opposition; the campaign continues while it remains politically viable.";
    }

    private static double GetBaseElectionScore(
        GameState state,
        Country country,
        Character candidate)
    {
        var constituency = IsCouncilElection(country)
            ? GetCouncilElectionBacking(country, candidate)
            : PoliticalCalculations.GetPowerBaseInfluence(
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

    private static bool IsCouncilElection(Country country) =>
        country.Government.ElectionMethod ==
        ElectionMethod.CouncilElection;

    private static double GetCouncilElectionBacking(
        Country country,
        Character candidate)
    {
        var bases = new (PowerBaseType Type, double Weight)[]
        {
            (PowerBaseType.Merchants, 2.0),
            (PowerBaseType.Party, 1.7),
            (PowerBaseType.Bureaucracy, 1.3),
            (PowerBaseType.Aristocracy, 0.9),
            (PowerBaseType.RegionalElites, 0.8)
        };

        var weighted = 0.0;
        var totalWeight = 0.0;

        foreach (var (powerBase, weight) in bases)
        {
            var structural =
                Math.Max(
                    0.05,
                    country.GetPowerBaseStrength(powerBase) / 100.0);
            var effectiveWeight = weight * structural;

            weighted +=
                candidate.GetPowerBaseStanding(powerBase) *
                effectiveWeight;
            totalWeight += effectiveWeight;
        }

        return totalWeight > 0
            ? weighted / totalWeight
            : 50;
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
