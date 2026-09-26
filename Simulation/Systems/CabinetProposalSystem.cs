using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal sealed record CabinetProposalResponse(
    Order? Order,
    SimulationReport Report);

internal static class CabinetProposalSystem
{
    private const int ProposalLifetimeMonths = 4;
    private const int ProposalCooldownMonths = 6;
    private const int MaximumConcurrentProposals = 2;

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();
        var country = state.Player.Country;

        AdvancePendingProposals(state, country, reports);

        var urgentCrisisDecision =
            state.PoliticalCrises.Any(crisis =>
                crisis.Status ==
                    PoliticalCrisisStatus.Active &&
                crisis.AwaitingDecision &&
                ReferenceEquals(
                    crisis.Country,
                    country));

        if (state.Player.HasLost ||
            urgentCrisisDecision ||
            state.Date.Month is not (1 or 4 or 7 or 10))
        {
            return reports;
        }

        var pendingCount = state.CabinetProposals.Count(proposal =>
            proposal.Status == CabinetProposalStatus.Pending &&
            ReferenceEquals(proposal.Country, country));

        if (pendingCount >= MaximumConcurrentProposals)
            return reports;

        var candidate = BuildCandidates(state, country)
            .Where(candidate => !HasRecentEquivalent(state, candidate))
            .OrderByDescending(candidate => candidate.Priority)
            .ThenByDescending(candidate => candidate.Advisor.Competence)
            .FirstOrDefault();

        if (candidate is null)
            return reports;

        var proposal = new CabinetProposal
        {
            Country = country,
            Advisor = candidate.Advisor,
            Type = candidate.Type,
            TargetCountry = candidate.TargetCountry,
            War = candidate.War,
            TargetValue = candidate.TargetValue,
            Rationale = candidate.Rationale,
            CreatedOn = state.Date
        };

        state.CabinetProposals.Add(proposal);

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Personal,
            ProposalTitle(proposal),
            ProposalDescription(proposal) +
            " The recommendation is awaiting the ruler's decision."));

        return reports;
    }

    public static CabinetProposalResponse Respond(
        GameState state,
        CabinetProposal proposal,
        bool accept)
    {
        if (proposal.Status != CabinetProposalStatus.Pending ||
            !ReferenceEquals(proposal.Country, state.Player.Country))
        {
            return new CabinetProposalResponse(
                null,
                new SimulationReport(
                    state.Date,
                    ReportCategory.Personal,
                    "Cabinet recommendation unavailable",
                    "That recommendation is no longer awaiting a decision."));
        }

        if (!proposal.Advisor.IsPoliticallyActive)
        {
            proposal.Status = CabinetProposalStatus.Withdrawn;

            return new CabinetProposalResponse(
                null,
                new SimulationReport(
                    state.Date,
                    ReportCategory.Personal,
                    "Cabinet recommendation withdrawn",
                    $"{proposal.Advisor.FullName} can no longer press the recommendation."));
        }

        var towardRuler = state.Relationships.GetOrCreate(
            proposal.Advisor,
            proposal.Country.Ruler);

        if (!accept)
        {
            proposal.Status = CabinetProposalStatus.Rejected;
            towardRuler.ChangeOpinion(proposal.Advisor.Ambition >= 70 ? -3 : -2);
            towardRuler.ChangeTrust(-1);

            return new CabinetProposalResponse(
                null,
                new SimulationReport(
                    state.Date,
                    ReportCategory.Personal,
                    $"{proposal.Advisor.FullName}'s recommendation rejected",
                    $"The ruler declines the recommendation: {ProposalDescription(proposal)}"));
        }

        var order = CreateOrder(state, proposal);

        if (order is null)
        {
            proposal.Status = CabinetProposalStatus.Withdrawn;

            return new CabinetProposalResponse(
                null,
                new SimulationReport(
                    state.Date,
                    ReportCategory.Personal,
                    "Cabinet recommendation cannot be enacted",
                    $"{proposal.Advisor.FullName}'s recommendation is accepted in principle, " +
                    "but the responsible office or situation has changed and no valid order can be issued."));
        }

        proposal.Status = CabinetProposalStatus.Accepted;
        towardRuler.ChangeOpinion(3);
        towardRuler.ChangeTrust(2);
        proposal.Advisor.Influence = Math.Min(100, proposal.Advisor.Influence + 1);

        return new CabinetProposalResponse(
            order,
            new SimulationReport(
                state.Date,
                ReportCategory.Personal,
                $"{proposal.Advisor.FullName}'s recommendation accepted",
                $"The ruler accepts the recommendation. A formal order has been queued: " +
                ProposalDescription(proposal)));
    }

    public static string ProposalTitle(CabinetProposal proposal)
    {
        return proposal.Type switch
        {
            CabinetProposalType.LowerTaxes =>
                $"{proposal.Advisor.FullName} urges tax relief",
            CabinetProposalType.RaiseTaxes =>
                $"{proposal.Advisor.FullName} urges higher taxation",
            CabinetProposalType.RaiseArmyFunding =>
                $"{proposal.Advisor.FullName} requests more military funding",
            CabinetProposalType.RaiseAdministrationFunding =>
                $"{proposal.Advisor.FullName} requests administrative funding",
            CabinetProposalType.RaiseCourtFunding =>
                $"{proposal.Advisor.FullName} recommends more patronage funding",
            CabinetProposalType.ImproveRelations =>
                $"{proposal.Advisor.FullName} urges diplomatic outreach",
            CabinetProposalType.NegotiateTradeAgreement =>
                $"{proposal.Advisor.FullName} proposes trade talks",
            CabinetProposalType.DefensiveWarStance =>
                $"{proposal.Advisor.FullName} urges a defensive campaign",
            CabinetProposalType.AggressiveWarStance =>
                $"{proposal.Advisor.FullName} urges an aggressive campaign",
            CabinetProposalType.OfferWhitePeace =>
                $"{proposal.Advisor.FullName} recommends peace talks",
            _ => $"{proposal.Advisor.FullName} submits a recommendation"
        };
    }

    public static string ProposalDescription(CabinetProposal proposal)
    {
        var action = proposal.Type switch
        {
            CabinetProposalType.LowerTaxes =>
                $"Reduce the official tax rate to {proposal.TargetValue:P0}.",

            CabinetProposalType.RaiseTaxes =>
                $"Raise the official tax rate to {proposal.TargetValue:P0}.",

            CabinetProposalType.RaiseArmyFunding =>
                $"Raise army funding to {proposal.TargetValue:P0}.",

            CabinetProposalType.RaiseAdministrationFunding =>
                $"Raise administration funding to {proposal.TargetValue:P0}.",

            CabinetProposalType.RaiseCourtFunding =>
                $"Raise court and patronage funding to {proposal.TargetValue:P0}.",

            CabinetProposalType.ImproveRelations when proposal.TargetCountry is not null =>
                $"Send a diplomatic mission to improve relations with {proposal.TargetCountry.Name}.",

            CabinetProposalType.NegotiateTradeAgreement when proposal.TargetCountry is not null =>
                $"Open trade negotiations with {proposal.TargetCountry.Name}.",

            CabinetProposalType.DefensiveWarStance when proposal.War is not null =>
                $"Order a Defensive stance against {proposal.War.OpponentOf(proposal.Country).Name}.",

            CabinetProposalType.AggressiveWarStance when proposal.War is not null =>
                $"Order an Aggressive stance against {proposal.War.OpponentOf(proposal.Country).Name}.",

            CabinetProposalType.OfferWhitePeace when proposal.War is not null =>
                $"Offer white peace to {proposal.War.OpponentOf(proposal.Country).Name}.",

            _ => "Act on the adviser's recommendation."
        };

        return string.IsNullOrWhiteSpace(proposal.Rationale)
            ? action
            : $"{action} {proposal.Rationale}";
    }

    private static void AdvancePendingProposals(
        GameState state,
        Countries.Country country,
        List<SimulationReport> reports)
    {
        foreach (var proposal in state.CabinetProposals.Where(proposal =>
                     proposal.Status == CabinetProposalStatus.Pending &&
                     ReferenceEquals(proposal.Country, country)))
        {
            proposal.MonthsOpen++;

            if (!proposal.Advisor.IsPoliticallyActive)
            {
                proposal.Status = CabinetProposalStatus.Withdrawn;
                continue;
            }

            if (proposal.MonthsOpen < ProposalLifetimeMonths)
                continue;

            proposal.Status = CabinetProposalStatus.Expired;

            var towardRuler = state.Relationships.GetOrCreate(
                proposal.Advisor,
                country.Ruler);
            towardRuler.ChangeOpinion(-3);
            towardRuler.ChangeTrust(-2);

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Personal,
                $"{proposal.Advisor.FullName}'s recommendation goes unanswered",
                $"{ProposalDescription(proposal)} After months without a decision, " +
                $"{proposal.Advisor.FullName} withdraws the recommendation and is frustrated by the silence."));
        }
    }

    private static IEnumerable<ProposalCandidate> BuildCandidates(
        GameState state,
        Countries.Country country)
    {
        var treasurer = country.GetOfficeHolder(Position.Treasurer);
        var marshal = country.GetOfficeHolder(Position.Marshal);
        var chancellor = country.GetOfficeHolder(Position.Chancellor);

        if (treasurer is not null &&
            IsWillingToAdvise(state, country, treasurer))
        {
            foreach (var candidate in TreasurerCandidates(
                         state,
                         country,
                         treasurer))
            {
                yield return candidate;
            }
        }

        if (marshal is not null &&
            IsWillingToAdvise(state, country, marshal))
        {
            foreach (var candidate in MarshalCandidates(
                         state,
                         country,
                         marshal))
            {
                yield return candidate;
            }
        }

        if (chancellor is not null &&
            IsWillingToAdvise(state, country, chancellor))
        {
            foreach (var candidate in ChancellorCandidates(
                         state,
                         country,
                         chancellor))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<ProposalCandidate> TreasurerCandidates(
        GameState state,
        Countries.Country country,
        Character treasurer)
    {
        foreach (var demand in state.PowerBaseDemands
                     .Where(demand =>
                         !demand.IsResolved &&
                         ReferenceEquals(demand.Country, country))
                     .OrderByDescending(demand => demand.EscalationLevel)
                     .ThenByDescending(demand => demand.MonthsOpen))
        {
            var priority = 90 + demand.EscalationLevel * 10;

            switch (demand.Type)
            {
                case PowerBaseDemandType.LowerTaxes:
                    yield return new ProposalCandidate(
                        treasurer,
                        CabinetProposalType.LowerTaxes,
                        priority,
                        TargetValue: demand.TargetValue);
                    break;

                case PowerBaseDemandType.RaiseArmyFunding:
                    yield return new ProposalCandidate(
                        treasurer,
                        CabinetProposalType.RaiseArmyFunding,
                        priority,
                        TargetValue: demand.TargetValue);
                    break;

                case PowerBaseDemandType.RaiseAdministrationFunding:
                    yield return new ProposalCandidate(
                        treasurer,
                        CabinetProposalType.RaiseAdministrationFunding,
                        priority,
                        TargetValue: demand.TargetValue);
                    break;

                case PowerBaseDemandType.RaiseCourtFunding:
                    yield return new ProposalCandidate(
                        treasurer,
                        CabinetProposalType.RaiseCourtFunding,
                        priority,
                        TargetValue: demand.TargetValue);
                    break;
            }
        }

        if (country.Government.HoldsScheduledElections &&
            country.Government.MonthsUntilElection <=
                country.Government.ElectionCampaignMonths)
        {
            var months = Math.Max(
                1,
                country.Government.MonthsUntilElection);
            var urgency =
                country.Government.ElectionCampaignMonths - months;

            if (country.TaxRate > 0.07m)
            {
                yield return new ProposalCandidate(
                    treasurer,
                    CabinetProposalType.LowerTaxes,
                    76 +
                    urgency * 2 +
                    Math.Max(0, country.PublicUnrest - 30) * 0.20,
                    TargetValue: Math.Max(
                        0.02m,
                        country.TaxRate - 0.01m),
                    Rationale:
                        $"With the election {months} " +
                        $"{(months == 1 ? "month" : "months")} away, " +
                        "the Treasury expects even modest tax relief to improve the government's political position.");
            }

            if (country.CourtFunding < 1.10m &&
                country.GetPowerBaseStrength(PowerBaseType.Party) >= 50)
            {
                yield return new ProposalCandidate(
                    treasurer,
                    CabinetProposalType.RaiseCourtFunding,
                    68 + urgency * 1.5,
                    TargetValue: Math.Min(
                        1.5m,
                        country.CourtFunding + 0.10m),
                    Rationale:
                        "The approaching election makes patronage, coalition organisation and elite access more politically valuable than usual.");
            }
        }

        if (country.LastMonthlyBalance < 0 &&
            country.TaxRate < 0.18m)
        {
            var deficitScale =
                (double)(-country.LastMonthlyBalance /
                Math.Max(1m, country.Gdp / 1000m));

            yield return new ProposalCandidate(
                treasurer,
                CabinetProposalType.RaiseTaxes,
                72 + Math.Min(18, deficitScale * 8),
                TargetValue: Math.Min(0.60m, country.TaxRate + 0.02m));
        }

        if ((country.PublicUnrest >= 55 ||
             country.Ruler.GetPowerBaseStanding(PowerBaseType.Merchants) < 35) &&
            country.TaxRate > 0.06m)
        {
            yield return new ProposalCandidate(
                treasurer,
                CabinetProposalType.LowerTaxes,
                76 + Math.Max(0, country.PublicUnrest - 55) * 0.35,
                TargetValue: Math.Max(0.02m, country.TaxRate - 0.02m));
        }

        if (country.AdministrativeEfficiency < 0.62m &&
            country.AdministrationFunding < 1.20m)
        {
            yield return new ProposalCandidate(
                treasurer,
                CabinetProposalType.RaiseAdministrationFunding,
                68,
                TargetValue: Math.Min(
                    1.5m,
                    country.AdministrationFunding + 0.15m));
        }
    }

    private static IEnumerable<ProposalCandidate> MarshalCandidates(
        GameState state,
        Countries.Country country,
        Character marshal)
    {
        var wars = state.Wars
            .Where(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(country))
            .ToList();

        foreach (var war in wars)
        {
            var ownScore = ReferenceEquals(country, war.Attacker)
                ? war.WarScore
                : -war.WarScore;

            if (country.WarExhaustion >= 50 ||
                country.ArmyReadiness <= 40 ||
                ownScore <= -30)
            {
                yield return new ProposalCandidate(
                    marshal,
                    CabinetProposalType.DefensiveWarStance,
                    82 +
                    Math.Max(0, country.WarExhaustion - 50) * 0.4 +
                    Math.Max(0, -ownScore - 30) * 0.35,
                    War: war);
            }
            else if (ownScore >= 25 &&
                     country.ArmyReadiness >= 65 &&
                     country.WarExhaustion < 40)
            {
                yield return new ProposalCandidate(
                    marshal,
                    CabinetProposalType.AggressiveWarStance,
                    70 + Math.Min(15, (ownScore - 25) * 0.35),
                    War: war);
            }
        }

        if (country.ArmyReadiness < 55 &&
            country.ArmyFunding < 1.20m)
        {
            yield return new ProposalCandidate(
                marshal,
                CabinetProposalType.RaiseArmyFunding,
                74 + Math.Max(0, 55 - country.ArmyReadiness) * 0.5,
                TargetValue: Math.Min(
                    1.5m,
                    country.ArmyFunding + 0.20m));
        }
    }

    private static IEnumerable<ProposalCandidate> ChancellorCandidates(
        GameState state,
        Countries.Country country,
        Character chancellor)
    {
        foreach (var war in state.Wars.Where(war =>
                     war.Status == WarStatus.Active &&
                     war.IsParticipant(country)))
        {
            var ownScore = ReferenceEquals(country, war.Attacker)
                ? war.WarScore
                : -war.WarScore;

            var peaceDemand = state.PowerBaseDemands.FirstOrDefault(demand =>
                !demand.IsResolved &&
                demand.Type == PowerBaseDemandType.EndWar &&
                ReferenceEquals(demand.Country, country));

            if (peaceDemand is not null ||
                country.WarExhaustion >= 60 ||
                ownScore <= -40)
            {
                var priority =
                    88 +
                    (peaceDemand?.EscalationLevel ?? 0) * 10 +
                    Math.Max(0, country.WarExhaustion - 60) * 0.35 +
                    Math.Max(0, -ownScore - 40) * 0.25;

                yield return new ProposalCandidate(
                    chancellor,
                    CabinetProposalType.OfferWhitePeace,
                    priority,
                    War: war);
            }
        }

        foreach (var foreign in state.Countries.Where(foreign =>
                     !ReferenceEquals(foreign, country)))
        {
            if (AreAtWar(state, country, foreign))
                continue;

            var relation = state.Diplomacy.GetOrCreate(
                country,
                foreign);

            if (relation.Relations <= -20 &&
                relation.Tension < 85)
            {
                yield return new ProposalCandidate(
                    chancellor,
                    CabinetProposalType.ImproveRelations,
                    70 + Math.Min(20, (-relation.Relations - 20) * 0.4),
                    TargetCountry: foreign);
            }

            if (!relation.HasTradeAgreement &&
                relation.Relations >= 20 &&
                relation.Trust >= 45 &&
                relation.Tension <= 40)
            {
                var desirability =
                    DiplomaticCalculations.GetTradeDesirabilityScore(
                        state,
                        country,
                        foreign,
                        chancellor);

                if (desirability >= 55)
                {
                    yield return new ProposalCandidate(
                        chancellor,
                        CabinetProposalType.NegotiateTradeAgreement,
                        60 + (desirability - 55) * 0.35,
                        TargetCountry: foreign);
                }
            }
        }
    }

    private static bool IsWillingToAdvise(
        GameState state,
        Countries.Country country,
        Character advisor)
    {
        return PoliticalCalculations.GetOrderWillingness(
            state,
            country,
            advisor,
            country.Ruler) >= 30;
    }

    private static bool HasRecentEquivalent(
        GameState state,
        ProposalCandidate candidate)
    {
        return state.CabinetProposals.Any(proposal =>
            proposal.Type == candidate.Type &&
            ReferenceEquals(proposal.Advisor, candidate.Advisor) &&
            ReferenceEquals(proposal.TargetCountry, candidate.TargetCountry) &&
            ReferenceEquals(proposal.War, candidate.War) &&
            MonthsBetween(proposal.CreatedOn, state.Date) <
            ProposalCooldownMonths);
    }

    private static Order? CreateOrder(
        GameState state,
        CabinetProposal proposal)
    {
        var country = proposal.Country;
        var ruler = country.Ruler;
        var treasurer = country.GetOfficeHolder(Position.Treasurer);
        var marshal = country.GetOfficeHolder(Position.Marshal);
        var chancellor = country.GetOfficeHolder(Position.Chancellor);

        return proposal.Type switch
        {
            CabinetProposalType.LowerTaxes or
            CabinetProposalType.RaiseTaxes
                when treasurer is not null =>
                new ChangeTaxOrder
                {
                    Issuer = ruler,
                    Recipient = treasurer,
                    IssuedOn = state.Date,
                    Country = country,
                    TargetTaxRate = proposal.TargetValue
                },

            CabinetProposalType.RaiseArmyFunding
                when treasurer is not null =>
                new SetBudgetOrder
                {
                    Issuer = ruler,
                    Recipient = treasurer,
                    IssuedOn = state.Date,
                    Country = country,
                    TargetArmyFunding = proposal.TargetValue,
                    TargetAdministrationFunding =
                        country.AdministrationFunding,
                    TargetCourtFunding = country.CourtFunding
                },

            CabinetProposalType.RaiseAdministrationFunding
                when treasurer is not null =>
                new SetBudgetOrder
                {
                    Issuer = ruler,
                    Recipient = treasurer,
                    IssuedOn = state.Date,
                    Country = country,
                    TargetArmyFunding = country.ArmyFunding,
                    TargetAdministrationFunding = proposal.TargetValue,
                    TargetCourtFunding = country.CourtFunding
                },

            CabinetProposalType.RaiseCourtFunding
                when treasurer is not null =>
                new SetBudgetOrder
                {
                    Issuer = ruler,
                    Recipient = treasurer,
                    IssuedOn = state.Date,
                    Country = country,
                    TargetArmyFunding = country.ArmyFunding,
                    TargetAdministrationFunding =
                        country.AdministrationFunding,
                    TargetCourtFunding = proposal.TargetValue
                },

            CabinetProposalType.ImproveRelations
                when chancellor is not null &&
                     proposal.TargetCountry is not null =>
                new ImproveRelationsOrder
                {
                    Issuer = ruler,
                    Recipient = chancellor,
                    IssuedOn = state.Date,
                    SourceCountry = country,
                    TargetCountry = proposal.TargetCountry
                },

            CabinetProposalType.NegotiateTradeAgreement
                when chancellor is not null &&
                     proposal.TargetCountry is not null =>
                new NegotiateTradeAgreementOrder
                {
                    Issuer = ruler,
                    Recipient = chancellor,
                    IssuedOn = state.Date,
                    SourceCountry = country,
                    TargetCountry = proposal.TargetCountry
                },

            CabinetProposalType.DefensiveWarStance
                when marshal is not null &&
                     proposal.War is { Status: WarStatus.Active } defensiveWar =>
                new SetWarStanceOrder
                {
                    Issuer = ruler,
                    Recipient = marshal,
                    IssuedOn = state.Date,
                    Country = country,
                    War = defensiveWar,
                    RequestedStance = WarStance.Defensive
                },

            CabinetProposalType.AggressiveWarStance
                when marshal is not null &&
                     proposal.War is { Status: WarStatus.Active } aggressiveWar =>
                new SetWarStanceOrder
                {
                    Issuer = ruler,
                    Recipient = marshal,
                    IssuedOn = state.Date,
                    Country = country,
                    War = aggressiveWar,
                    RequestedStance = WarStance.Aggressive
                },

            CabinetProposalType.OfferWhitePeace
                when chancellor is not null &&
                     proposal.War is { Status: WarStatus.Active } peaceWar =>
                new OfferPeaceOrder
                {
                    Issuer = ruler,
                    Recipient = chancellor,
                    IssuedOn = state.Date,
                    Country = country,
                    War = peaceWar,
                    Terms = PeaceOfferTerms.WhitePeace
                },

            _ => null
        };
    }

    private static bool AreAtWar(
        GameState state,
        Countries.Country first,
        Countries.Country second)
    {
        return state.Wars.Any(war =>
            war.Status == WarStatus.Active &&
            war.IsParticipant(first) &&
            war.IsParticipant(second));
    }

    private static int MonthsBetween(GameDate start, GameDate end)
    {
        return (end.Year - start.Year) * 12 +
               end.Month -
               start.Month;
    }

    private sealed record ProposalCandidate(
        Character Advisor,
        CabinetProposalType Type,
        double Priority,
        Countries.Country? TargetCountry = null,
        War? War = null,
        decimal TargetValue = 0m,
        string? Rationale = null);
}
