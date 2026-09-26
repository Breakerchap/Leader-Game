using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal sealed record CrisisChoiceDefinition(
    PoliticalCrisisResponse Response,
    string Label,
    string Description);

internal sealed record CrisisAdviceDefinition(
    Character Advisor,
    PoliticalCrisisResponse Response,
    string Reason);

internal static class CrisisSystem
{
    private const int MonthsPerStage = 2;
    private const int MaxActiveCrises = 2;

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();
        var country = state.Player.Country;

        if (!state.Player.IsInPower)
        {
            foreach (var crisis in state.PoliticalCrises.Where(crisis =>
                         crisis.Status == PoliticalCrisisStatus.Active &&
                         ReferenceEquals(crisis.Country, country)))
            {
                crisis.Status = PoliticalCrisisStatus.Resolved;
                crisis.ResolvedOn = state.Date;

                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{Title(crisis)} passes to the new government",
                    $"The crisis itself has not necessarily vanished, but {state.Player.Lineage.Name} no longer controls the state and therefore no longer chooses the government's response. The underlying conditions remain part of the simulation."));
            }

            return reports;
        }

        foreach (var crisis in state.PoliticalCrises
                     .Where(crisis =>
                         crisis.Status == PoliticalCrisisStatus.Active &&
                         ReferenceEquals(crisis.Country, country))
                     .ToList())
        {
            ProcessActiveCrisis(state, crisis, reports);
        }

        TryTriggerCrises(state, country, reports);

        return reports;
    }

    public static SimulationReport Respond(
        GameState state,
        Guid crisisId,
        PoliticalCrisisResponse response)
    {
        if (!state.Player.IsInPower)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                "Government crisis response unavailable",
                $"{state.Player.Lineage.Name} is outside government and cannot choose the state's response to a governing crisis.");
        }

        var crisis = state.PoliticalCrises.FirstOrDefault(candidate =>
            candidate.Id == crisisId &&
            candidate.Status == PoliticalCrisisStatus.Active &&
            ReferenceEquals(candidate.Country, state.Player.Country));

        if (crisis is null)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                "Crisis decision unavailable",
                "That crisis is no longer active.");
        }

        if (!crisis.AwaitingDecision)
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                "Crisis response already chosen",
                "The government has already committed to a response at this stage. Its effects now need time to play out.");
        }

        if (!GetChoices(crisis).Any(choice =>
                choice.Response == response))
        {
            return new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                "Crisis response unavailable",
                "That response does not apply to this crisis.");
        }

        var adviceBeforeDecision =
            GetAdvice(state, crisis);

        var details = crisis.Type switch
        {
            PoliticalCrisisType.RegionalBreakdown =>
                ApplyRegionalResponse(state, crisis, response),
            PoliticalCrisisType.FiscalEmergency =>
                ApplyFiscalResponse(state, crisis, response),
            PoliticalCrisisType.PoliticalStandoff =>
                ApplyPoliticalResponse(state, crisis, response),
            PoliticalCrisisType.SuccessionDispute =>
                ApplySuccessionResponse(state, crisis, response),
            PoliticalCrisisType.WarEmergency =>
                ApplyWarResponse(state, crisis, response),
            _ => "No response was carried out."
        };

        details += ApplyAdviceReaction(
            state,
            crisis,
            response,
            adviceBeforeDecision);

        crisis.LastResponse = response;
        crisis.AwaitingDecision = false;
        crisis.MonthsAtCurrentStage = 0;

        if (UnderlyingProblemResolved(state, crisis))
        {
            crisis.Status = PoliticalCrisisStatus.Resolved;
            crisis.ResolvedOn = state.Date;

            details += " The underlying pressure has eased enough for the crisis to recede.";
        }
        else
        {
            details += " The immediate response buys political room, but the underlying crisis remains unresolved.";
        }

        return new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{Title(crisis)} — response ordered",
            details);
    }

    public static string Title(PoliticalCrisis crisis) =>
        crisis.Type switch
        {
            PoliticalCrisisType.RegionalBreakdown =>
                $"{crisis.Region?.Name ?? "A region"} in crisis",
            PoliticalCrisisType.FiscalEmergency =>
                "State finances under emergency pressure",
            PoliticalCrisisType.PoliticalStandoff =>
                "Government and opposition in open standoff",
            PoliticalCrisisType.SuccessionDispute =>
                "Succession factions are hardening",
            PoliticalCrisisType.WarEmergency =>
                "Military setbacks are becoming a government crisis",
            _ => "Political crisis"
        };

    public static string Summary(
        GameState state,
        PoliticalCrisis crisis)
    {
        return crisis.Type switch
        {
            PoliticalCrisisType.RegionalBreakdown =>
                $"{crisis.Region?.Name ?? "The region"} combines serious disorder with weak or contested central authority. " +
                "Any response will change the balance between immediate order and long-run control.",

            PoliticalCrisisType.FiscalEmergency =>
                $"Debt and recurring deficits are constraining the government. " +
                $"The current debt burden is roughly {DebtRatio(crisis.Country):P0} of annual economic output.",

            PoliticalCrisisType.PoliticalStandoff =>
                PoliticalStandoffSummary(state, crisis),

            PoliticalCrisisType.SuccessionDispute =>
                SuccessionSummary(crisis.Country),

            PoliticalCrisisType.WarEmergency =>
                WarEmergencySummary(state, crisis),

            _ => "The government faces an unresolved political emergency."
        };
    }

    public static string SeverityLabel(
        PoliticalCrisis crisis) =>
        crisis.Stage switch
        {
            <= 1 => "Serious",
            2 => "Acute",
            _ => "Breaking point"
        };

    public static IReadOnlyList<CrisisChoiceDefinition> GetChoices(
        PoliticalCrisis crisis) =>
        crisis.Type switch
        {
            PoliticalCrisisType.RegionalBreakdown =>
            [
                new(
                    PoliticalCrisisResponse.StrengthenRegionalControl,
                    "Strengthen central control",
                    "Push officials and enforcement into the region. Improves authority and future collection, but risks immediate backlash."),
                new(
                    PoliticalCrisisResponse.OfferRegionalConcessions,
                    "Offer local concessions",
                    "Trade privileges and autonomy for calm. Usually eases disorder quickly but entrenches local power."),
                new(
                    PoliticalCrisisResponse.FundRegionalRelief,
                    "Fund relief and repairs",
                    "Spend scarce money to reduce grievance and improve prosperity without surrendering as much authority.")
            ],

            PoliticalCrisisType.FiscalEmergency =>
            [
                new(
                    PoliticalCrisisResponse.RaiseEmergencyRevenue,
                    "Raise emergency revenue",
                    "Impose extraordinary levies and collections. Brings in cash now, but creates political and social resistance."),
                new(
                    PoliticalCrisisResponse.CutStateCommitments,
                    "Cut state commitments",
                    "Reduce court, military and administrative spending. Improves the monthly position but weakens important institutions and clients."),
                new(
                    PoliticalCrisisResponse.BorrowForTime,
                    "Borrow for time",
                    "Raise a large short-term loan. Protects the government today but makes the structural debt problem worse.")
            ],

            PoliticalCrisisType.PoliticalStandoff =>
            [
                new(
                    PoliticalCrisisResponse.CooptOpposition,
                    "Co-opt opposition leaders",
                    "Use patronage, access and concessions to split the coalition. Costs money and can make rival leaders more important."),
                new(
                    PoliticalCrisisResponse.ConstitutionalCompromise,
                    "Offer constitutional compromise",
                    "Give organised interests a stronger formal role. Stabilises bargaining at the price of permanently sharing more power."),
                new(
                    PoliticalCrisisResponse.ConfrontOpposition,
                    "Confront the opposition",
                    "Try to break the coalition through political pressure. Success can restore authority; failure can radicalise the crisis.")
            ],

            PoliticalCrisisType.SuccessionDispute =>
            [
                new(
                    PoliticalCrisisResponse.PubliclyNameSuccessor,
                    "Publicly name the successor",
                    "Commit the ruler's authority behind the first heir. Clarifies the succession quickly, but rivals and their allies may feel shut out."),
                new(
                    PoliticalCrisisResponse.BalanceSuccessionFactions,
                    "Balance the claimants",
                    "Keep rival factions inside the tent through offices and assurances. Reduces immediate tension but deliberately preserves ambiguity."),
                new(
                    PoliticalCrisisResponse.ConveneSuccessionSettlement,
                    "Convene a succession settlement",
                    "Bring leading institutions and elites into a formal settlement. Builds legitimacy around the heir but gives those institutions a stronger precedent.")
            ],

            PoliticalCrisisType.WarEmergency =>
            [
                new(
                    PoliticalCrisisResponse.EmergencyMobilisation,
                    "Emergency mobilisation",
                    "Spend heavily, call up more forces and restore readiness. Can change the military trajectory, but raises debt, exhaustion and domestic resentment."),
                new(
                    PoliticalCrisisResponse.DismissMarshal,
                    "Dismiss the Marshal",
                    "Make the high command carry the blame. May restore political confidence, but disrupts the army and leaves you needing a new commander."),
                new(
                    PoliticalCrisisResponse.SeekPeaceSettlement,
                    "Seek a negotiated peace",
                    "Send the Chancellor with real peace terms. The enemy can still refuse if it believes continued war is better.")
            ],

            _ => []
        };

    public static IReadOnlyList<CrisisAdviceDefinition> GetAdvice(
        GameState state,
        PoliticalCrisis crisis)
    {
        return crisis.Country.ActiveAdvisors
            .OrderBy(advisor => advisor.Position)
            .Select(advisor =>
                AdviceFor(
                    state,
                    crisis,
                    advisor))
            .Where(advice => advice is not null)
            .Select(advice => advice!)
            .ToList();
    }

    private static CrisisAdviceDefinition? AdviceFor(
        GameState state,
        PoliticalCrisis crisis,
        Character advisor)
    {
        var response = crisis.Type switch
        {
            PoliticalCrisisType.RegionalBreakdown =>
                RegionalAdvice(
                    crisis,
                    advisor),

            PoliticalCrisisType.FiscalEmergency =>
                FiscalAdvice(
                    crisis,
                    advisor),

            PoliticalCrisisType.PoliticalStandoff =>
                PoliticalAdvice(
                    crisis,
                    advisor),

            PoliticalCrisisType.SuccessionDispute =>
                SuccessionAdvice(
                    crisis,
                    advisor),

            PoliticalCrisisType.WarEmergency =>
                WarAdvice(
                    state,
                    crisis,
                    advisor),

            _ => null
        };

        return response is null
            ? null
            : new CrisisAdviceDefinition(
                advisor,
                response.Value,
                AdviceReason(
                    state,
                    crisis,
                    advisor,
                    response.Value));
    }

    private static PoliticalCrisisResponse? RegionalAdvice(
        PoliticalCrisis crisis,
        Character advisor)
    {
        var region = crisis.Region;

        if (region is null)
            return null;

        if (advisor.GetPowerBaseStanding(
                PowerBaseType.RegionalElites) >= 72)
        {
            return PoliticalCrisisResponse
                .OfferRegionalConcessions;
        }

        return advisor.Position switch
        {
            Position.Marshal =>
                PoliticalCrisisResponse
                    .StrengthenRegionalControl,

            Position.Treasurer
                when crisis.Country.Treasury >
                     crisis.Country.Gdp *
                     Math.Max(
                         0.05m,
                         region.EconomicShare) *
                     0.0015m =>
                PoliticalCrisisResponse
                    .FundRegionalRelief,

            Position.Treasurer =>
                PoliticalCrisisResponse
                    .OfferRegionalConcessions,

            Position.Chancellor
                when region.LocalElitePower >
                     region.CrownControl + 20 =>
                PoliticalCrisisResponse
                    .OfferRegionalConcessions,

            Position.Chancellor =>
                PoliticalCrisisResponse
                    .StrengthenRegionalControl,

            _ => PoliticalCrisisResponse
                .FundRegionalRelief
        };
    }

    private static PoliticalCrisisResponse? FiscalAdvice(
        PoliticalCrisis crisis,
        Character advisor)
    {
        if (advisor.GetPowerBaseStanding(
                PowerBaseType.Military) >= 78)
        {
            return PoliticalCrisisResponse
                .BorrowForTime;
        }

        if (advisor.GetPowerBaseStanding(
                PowerBaseType.Merchants) >= 78)
        {
            return PoliticalCrisisResponse
                .CutStateCommitments;
        }

        return advisor.Position switch
        {
            Position.Treasurer
                when crisis.Country.TaxRate <=
                     0.13m =>
                PoliticalCrisisResponse
                    .RaiseEmergencyRevenue,

            Position.Treasurer =>
                PoliticalCrisisResponse
                    .CutStateCommitments,

            Position.Marshal =>
                PoliticalCrisisResponse
                    .BorrowForTime,

            Position.Chancellor
                when crisis.Country.Government
                         .LegislativeBody !=
                     LegislativeBodyType.None =>
                PoliticalCrisisResponse
                    .RaiseEmergencyRevenue,

            _ => PoliticalCrisisResponse
                .CutStateCommitments
        };
    }

    private static PoliticalCrisisResponse? PoliticalAdvice(
        PoliticalCrisis crisis,
        Character advisor)
    {
        if (advisor.GetPowerBaseStanding(
                PowerBaseType.Military) >= 80)
        {
            return PoliticalCrisisResponse
                .ConfrontOpposition;
        }

        if (advisor.GetPowerBaseStanding(
                PowerBaseType.RegionalElites) >= 75 ||
            advisor.GetPowerBaseStanding(
                PowerBaseType.Aristocracy) >= 78)
        {
            return PoliticalCrisisResponse
                .CooptOpposition;
        }

        return advisor.Position switch
        {
            Position.Marshal =>
                PoliticalCrisisResponse
                    .ConfrontOpposition,

            Position.Chancellor =>
                PoliticalCrisisResponse
                    .ConstitutionalCompromise,

            Position.Treasurer
                when crisis.Country.Treasury >
                     crisis.Country.Gdp *
                     0.001m =>
                PoliticalCrisisResponse
                    .CooptOpposition,

            Position.Treasurer =>
                PoliticalCrisisResponse
                    .ConstitutionalCompromise,

            _ => PoliticalCrisisResponse
                .CooptOpposition
        };
    }

    private static PoliticalCrisisResponse? SuccessionAdvice(
        PoliticalCrisis crisis,
        Character advisor)
    {
        if (advisor.GetPowerBaseStanding(
                PowerBaseType.RoyalFamily) >= 75)
        {
            return PoliticalCrisisResponse
                .PubliclyNameSuccessor;
        }

        if (advisor.GetPowerBaseStanding(
                PowerBaseType.Aristocracy) >= 75 ||
            advisor.GetPowerBaseStanding(
                PowerBaseType.RegionalElites) >= 75)
        {
            return PoliticalCrisisResponse
                .ConveneSuccessionSettlement;
        }

        return advisor.Position switch
        {
            Position.Marshal =>
                PoliticalCrisisResponse
                    .PubliclyNameSuccessor,

            Position.Chancellor =>
                PoliticalCrisisResponse
                    .ConveneSuccessionSettlement,

            Position.Treasurer =>
                PoliticalCrisisResponse
                    .BalanceSuccessionFactions,

            _ => PoliticalCrisisResponse
                .BalanceSuccessionFactions
        };
    }

    private static PoliticalCrisisResponse? WarAdvice(
        GameState state,
        PoliticalCrisis crisis,
        Character advisor)
    {
        var war = FindRelatedWar(
            state,
            crisis);

        if (war is null)
            return null;

        var score =
            CountryWarScore(
                war,
                crisis.Country);

        if (advisor.GetPowerBaseStanding(
                PowerBaseType.Military) >= 78)
        {
            return PoliticalCrisisResponse
                .EmergencyMobilisation;
        }

        return advisor.Position switch
        {
            Position.Marshal =>
                PoliticalCrisisResponse
                    .EmergencyMobilisation,

            Position.Chancellor
                when score <= -30 ||
                     crisis.Country.WarExhaustion >= 50 =>
                PoliticalCrisisResponse
                    .SeekPeaceSettlement,

            Position.Chancellor =>
                PoliticalCrisisResponse
                    .DismissMarshal,

            Position.Treasurer
                when crisis.Country.LastMonthlyBalance < 0 ||
                     DebtRatio(crisis.Country) >= 0.15m =>
                PoliticalCrisisResponse
                    .SeekPeaceSettlement,

            Position.Treasurer =>
                PoliticalCrisisResponse
                    .DismissMarshal,

            _ => PoliticalCrisisResponse
                .SeekPeaceSettlement
        };
    }

    private static string AdviceReason(
        GameState state,
        PoliticalCrisis crisis,
        Character advisor,
        PoliticalCrisisResponse response)
    {
        return response switch
        {
            PoliticalCrisisResponse
                .StrengthenRegionalControl =>
                advisor.Position == Position.Marshal
                    ? "argues that visible weakness will encourage further defiance and wants the centre to demonstrate that its orders still matter."
                    : "believes the crisis is fundamentally a problem of weak state reach rather than insufficient concessions.",

            PoliticalCrisisResponse
                .OfferRegionalConcessions =>
                advisor.GetPowerBaseStanding(
                    PowerBaseType.RegionalElites) >= 72
                    ? "has strong ties to regional elites and argues that their cooperation is cheaper than trying to govern around them."
                    : "believes the government lacks the local leverage for a clean confrontation and should buy a workable settlement.",

            PoliticalCrisisResponse
                .FundRegionalRelief =>
                "argues that material grievances can be reduced without permanently surrendering as much political authority.",

            PoliticalCrisisResponse
                .RaiseEmergencyRevenue =>
                "prioritises keeping the machinery of government funded now, even at the cost of a fresh political fight over extraordinary levies.",

            PoliticalCrisisResponse
                .CutStateCommitments =>
                advisor.GetPowerBaseStanding(
                    PowerBaseType.Merchants) >= 78
                    ? "is closely aligned with commercial interests and strongly prefers spending cuts to another round of extraordinary collections."
                    : "argues that borrowing or new levies only postpone the need to bring recurring expenditure under control.",

            PoliticalCrisisResponse
                .BorrowForTime =>
                advisor.GetPowerBaseStanding(
                    PowerBaseType.Military) >= 78
                    ? "is strongly tied to the military establishment and resists cuts that would immediately weaken readiness."
                    : "argues that preserving state capacity through the immediate emergency matters more than the future debt burden.",

            PoliticalCrisisResponse
                .CooptOpposition =>
                "believes the coalition can be divided more cheaply than it can be defeated, though doing so will reward some of its leaders.",

            PoliticalCrisisResponse
                .ConstitutionalCompromise =>
                "argues that the opposition is expressing a durable balance of power and should be channelled into formal institutions rather than fought indefinitely.",

            PoliticalCrisisResponse
                .ConfrontOpposition =>
                "believes compromise will be read as weakness and wants the government to test whether the opposition coalition is really as strong as it appears.",

            PoliticalCrisisResponse
                .PubliclyNameSuccessor =>
                advisor.GetPowerBaseStanding(
                    PowerBaseType.RoyalFamily) >= 75
                    ? "is strongly tied to the ruling family and argues that uncertainty is more dangerous than disappointing rival claimants."
                    : "believes a clear chain of command matters more than preserving every faction's hopes.",

            PoliticalCrisisResponse
                .BalanceSuccessionFactions =>
                "wants to prevent a pre-emptive struggle at court and prefers to keep competing interests invested in the current regime, even if the eventual succession remains murky.",

            PoliticalCrisisResponse
                .ConveneSuccessionSettlement =>
                "argues that an heir will be safer if major institutions and magnates publicly bind themselves to the settlement before the throne becomes vacant.",

            PoliticalCrisisResponse
                .EmergencyMobilisation =>
                advisor.GetPowerBaseStanding(
                    PowerBaseType.Military) >= 78
                    ? "is closely tied to the military establishment and argues that the state must prove it can still sustain the war before enemies and domestic rivals smell collapse."
                    : "believes the military position is still recoverable if the government is willing to spend money and political capital immediately.",

            PoliticalCrisisResponse
                .DismissMarshal =>
                "argues that confidence in the present command has become part of the problem and that changing the high command may restore political credibility even at the cost of short-term disruption.",

            PoliticalCrisisResponse
                .SeekPeaceSettlement =>
                advisor.Position == Position.Treasurer
                    ? "warns that continuing the war is becoming a fiscal decision as much as a military one and prefers a negotiated loss to an uncontrolled financial collapse."
                    : "believes the military position no longer justifies the domestic cost and wants the Chancellor to test what settlement the enemy will actually accept.",

            _ => "offers no clear reasoning."
        };
    }

    private static string ApplyAdviceReaction(
        GameState state,
        PoliticalCrisis crisis,
        PoliticalCrisisResponse response,
        IReadOnlyList<CrisisAdviceDefinition> advice)
    {
        var supporters = new List<string>();

        foreach (var recommendation in advice)
        {
            var relationship =
                state.Relationships.GetOrCreate(
                    recommendation.Advisor,
                    crisis.Country.Ruler);

            if (recommendation.Response == response)
            {
                relationship.ChangeOpinion(2);
                relationship.ChangeTrust(1);
                supporters.Add(
                    recommendation.Advisor.FullName);
            }
            else if (recommendation.Advisor.Ambition >= 70)
            {
                relationship.ChangeOpinion(-1);
            }
        }

        return supporters.Count switch
        {
            0 => string.Empty,
            1 =>
                $" {supporters[0]} had argued for this course and gains some standing from the decision.",
            _ =>
                $" {string.Join(", ", supporters)} had argued for this course and gain some standing from the decision."
        };
    }

    private static void ProcessActiveCrisis(
        GameState state,
        PoliticalCrisis crisis,
        List<SimulationReport> reports)
    {
        crisis.MonthsActive++;
        crisis.MonthsAtCurrentStage++;

        ApplyOngoingPressure(state, crisis);

        if (UnderlyingProblemResolved(state, crisis))
        {
            crisis.Status = PoliticalCrisisStatus.Resolved;
            crisis.ResolvedOn = state.Date;

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{Title(crisis)} eases",
                "The pressures that sustained the crisis have fallen far enough that it is no longer dominating government attention."));
            return;
        }

        if (crisis.MonthsAtCurrentStage < MonthsPerStage)
            return;

        if (crisis.Stage < 3)
        {
            crisis.Stage++;
            crisis.MonthsAtCurrentStage = 0;
            crisis.AwaitingDecision = true;

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{Title(crisis)} escalates",
                $"The previous response has not solved the underlying problem. The crisis has entered an {SeverityLabel(crisis).ToLowerInvariant()} stage and demands another decision."));
            return;
        }

        BreakAgainstGovernment(state, crisis, reports);
    }

    private static void TryTriggerCrises(
        GameState state,
        Country country,
        List<SimulationReport> reports)
    {
        var activeCount = state.PoliticalCrises.Count(crisis =>
            crisis.Status == PoliticalCrisisStatus.Active &&
            ReferenceEquals(crisis.Country, country));

        if (activeCount >= MaxActiveCrises)
            return;

        var region = country.Regions
            .Where(candidate =>
                candidate.Unrest >= 68 &&
                (candidate.CrownControl < 50 ||
                 candidate.LocalElitePower >= 75))
            .OrderByDescending(candidate =>
                candidate.Unrest +
                candidate.LocalElitePower -
                candidate.CrownControl)
            .FirstOrDefault(candidate =>
                !HasRecentRegionalCrisis(
                    state,
                    country,
                    candidate));

        if (region is not null)
        {
            AddCrisis(
                state,
                new PoliticalCrisis
                {
                    Country = country,
                    Type = PoliticalCrisisType.RegionalBreakdown,
                    Region = region,
                    StartedOn = state.Date
                },
                reports);

            activeCount++;
        }

        if (activeCount >= MaxActiveCrises)
            return;

        if (ShouldTriggerFiscalCrisis(country) &&
            !HasRecentTypeCrisis(
                state,
                country,
                PoliticalCrisisType.FiscalEmergency,
                12))
        {
            AddCrisis(
                state,
                new PoliticalCrisis
                {
                    Country = country,
                    Type = PoliticalCrisisType.FiscalEmergency,
                    StartedOn = state.Date
                },
                reports);

            activeCount++;
        }

        if (activeCount >= MaxActiveCrises)
            return;

        var bloc = GetStandoffBloc(state, country);

        if (bloc is not null &&
            !HasRecentTypeCrisis(
                state,
                country,
                PoliticalCrisisType.PoliticalStandoff,
                12))
        {
            AddCrisis(
                state,
                new PoliticalCrisis
                {
                    Country = country,
                    Type = PoliticalCrisisType.PoliticalStandoff,
                    RelatedBlocId = bloc.Id,
                    StartedOn = state.Date
                },
                reports);

            activeCount++;
        }

        if (activeCount >= MaxActiveCrises)
            return;

        if (ShouldTriggerSuccessionCrisis(country) &&
            !HasRecentTypeCrisis(
                state,
                country,
                PoliticalCrisisType.SuccessionDispute,
                18))
        {
            AddCrisis(
                state,
                new PoliticalCrisis
                {
                    Country = country,
                    Type = PoliticalCrisisType.SuccessionDispute,
                    StartedOn = state.Date
                },
                reports);

            activeCount++;
        }

        if (activeCount >= MaxActiveCrises)
            return;

        var warEmergency =
            FindWarEmergency(state, country);

        if (warEmergency is not null &&
            !HasRecentWarCrisis(
                state,
                country,
                warEmergency))
        {
            AddCrisis(
                state,
                new PoliticalCrisis
                {
                    Country = country,
                    Type = PoliticalCrisisType.WarEmergency,
                    RelatedWarId = warEmergency.Id,
                    StartedOn = state.Date
                },
                reports);
        }
    }

    private static void AddCrisis(
        GameState state,
        PoliticalCrisis crisis,
        List<SimulationReport> reports)
    {
        state.PoliticalCrises.Add(crisis);

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            Title(crisis),
            Summary(state, crisis) +
            " The ruler must choose a response; doing nothing will allow the crisis to escalate."));
    }

    private static bool ShouldTriggerFiscalCrisis(
        Country country)
    {
        if (country.Gdp <= 0)
            return false;

        var debtRatio = DebtRatio(country);

        return
            debtRatio >= 0.20m &&
            country.LastMonthlyBalance < 0 ||
            country.Treasury <= 0 &&
            debtRatio >= 0.12m;
    }

    private static bool ShouldTriggerSuccessionCrisis(
        Country country)
    {
        if (country.Government.Type ==
            GovernmentType.Republic)
        {
            return false;
        }

        if (country.Ruler.Age < 58 &&
            country.Ruler.Health > 65)
        {
            return false;
        }

        var candidates =
            ActiveSuccessionCandidates(country);

        if (candidates.Count == 0)
            return false;

        if (candidates.Count == 1)
            return candidates[0].Legitimacy < 60;

        var first = candidates[0];
        var second = candidates[1];

        return
            first.Legitimacy < 75 ||
            first.Legitimacy - second.Legitimacy < 18;
    }

    private static PoliticalBloc? GetStandoffBloc(
        GameState state,
        Country country)
    {
        if (country.Government.Stability > 50)
            return null;

        var rulerBacking =
            PoliticalCalculations.GetPowerBaseInfluence(
                country,
                country.Ruler);

        if (rulerBacking >= 50)
            return null;

        return state.PoliticalBlocs
            .Where(bloc =>
                bloc.IsActive &&
                ReferenceEquals(bloc.Country, country) &&
                bloc.Cohesion >= 55)
            .OrderByDescending(bloc => bloc.Cohesion)
            .FirstOrDefault();
    }

    private static bool UnderlyingProblemResolved(
        GameState state,
        PoliticalCrisis crisis)
    {
        return crisis.Type switch
        {
            PoliticalCrisisType.RegionalBreakdown =>
                crisis.Region is null ||
                crisis.Region.Unrest < 50 ||
                crisis.Region.Unrest < 60 &&
                crisis.Region.CrownControl >= 60,

            PoliticalCrisisType.FiscalEmergency =>
                crisis.Country.Gdp <= 0 ||
                DebtRatio(crisis.Country) < 0.18m ||
                crisis.Country.LastMonthlyBalance >= 0 &&
                crisis.Country.Treasury > 0 &&
                DebtRatio(crisis.Country) < 0.24m,

            PoliticalCrisisType.PoliticalStandoff =>
                GetStandoffBloc(state, crisis.Country) is null ||
                crisis.Country.Government.Stability >= 60 ||
                PoliticalCalculations.GetPowerBaseInfluence(
                    crisis.Country,
                    crisis.Country.Ruler) >= 60,

            PoliticalCrisisType.SuccessionDispute =>
                SuccessionIsSettled(crisis.Country),

            PoliticalCrisisType.WarEmergency =>
                WarEmergencyResolved(
                    state,
                    crisis),

            _ => true
        };
    }

    private static void ApplyOngoingPressure(
        GameState state,
        PoliticalCrisis crisis)
    {
        var stage = Math.Max(1, crisis.Stage);

        switch (crisis.Type)
        {
            case PoliticalCrisisType.RegionalBreakdown:
                if (crisis.Region is not null)
                {
                    crisis.Region.Unrest += 0.6 * stage;
                    crisis.Country.Government.Stability -= 0.18 * stage;
                }
                break;

            case PoliticalCrisisType.FiscalEmergency:
                crisis.Country.Government.Stability -= 0.20 * stage;
                crisis.Country.PublicUnrest += 0.10 * stage;
                break;

            case PoliticalCrisisType.PoliticalStandoff:
                crisis.Country.Government.Stability -= 0.25 * stage;

                var bloc = FindRelatedBloc(state, crisis);

                if (bloc is not null)
                    bloc.Cohesion = Math.Clamp(
                        bloc.Cohesion + 1.5 * stage,
                        0,
                        100);
                break;

            case PoliticalCrisisType.SuccessionDispute:
                crisis.Country.Government.Stability -=
                    0.18 * stage;

                var candidates =
                    ActiveSuccessionCandidates(
                        crisis.Country);

                if (candidates.Count > 1)
                {
                    candidates[1].Influence =
                        Math.Min(
                            100,
                            candidates[1].Influence +
                            stage);
                }
                break;

            case PoliticalCrisisType.WarEmergency:
                crisis.Country.Government.Stability -=
                    0.22 * stage;
                crisis.Country.PublicUnrest +=
                    0.12 * stage;
                crisis.Country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.Military,
                    -stage);
                break;
        }
    }

    private static string ApplyRegionalResponse(
        GameState state,
        PoliticalCrisis crisis,
        PoliticalCrisisResponse response)
    {
        var region = crisis.Region ??
            throw new InvalidOperationException(
                "Regional crisis has no region.");

        return response switch
        {
            PoliticalCrisisResponse.StrengthenRegionalControl =>
                StrengthenRegionalControl(
                    crisis.Country,
                    region),

            PoliticalCrisisResponse.OfferRegionalConcessions =>
                OfferRegionalConcessions(
                    crisis.Country,
                    region),

            PoliticalCrisisResponse.FundRegionalRelief =>
                FundRegionalRelief(
                    crisis.Country,
                    region),

            _ => "No regional response was carried out."
        };
    }

    private static string StrengthenRegionalControl(
        Country country,
        Region region)
    {
        region.CrownControl += 10;
        region.LocalElitePower -= 6;
        region.Privileges -= 5;
        region.Unrest += 4;

        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.RegionalElites,
            -4);

        return
            $"The government doubles down on its own officers and enforcement in {region.Name}. Central authority improves, but the move angers local interests and raises immediate tension.";
    }

    private static string OfferRegionalConcessions(
        Country country,
        Region region)
    {
        region.Unrest -= 15;
        region.Privileges += 11;
        region.LocalElitePower += 7;
        region.CrownControl -= 4;

        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.RegionalElites,
            6);

        return
            $"The ruler offers a new local compact in {region.Name}. Disorder falls sharply and elite cooperation improves, but privileges and local political power become harder to unwind.";
    }

    private static string FundRegionalRelief(
        Country country,
        Region region)
    {
        var cost =
            country.Gdp *
            Math.Max(0.05m, region.EconomicShare) *
            0.0010m;

        PayFromTreasury(country, cost);

        region.Unrest -= 10;
        region.Prosperity += 7;
        region.CrownControl += 2;

        return
            $"The treasury commits roughly {cost:N0} to relief, repairs and local works in {region.Name}. Grievances ease and prosperity improves without a large formal concession of authority.";
    }

    private static string ApplyFiscalResponse(
        GameState state,
        PoliticalCrisis crisis,
        PoliticalCrisisResponse response)
    {
        var country = crisis.Country;

        switch (response)
        {
            case PoliticalCrisisResponse.RaiseEmergencyRevenue:
            {
                var receipts = country.Gdp * 0.006m;
                country.Treasury += receipts;
                country.PublicUnrest += 4;
                country.Government.Stability -= 1;
                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.Merchants,
                    -5);
                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.RegionalElites,
                    -4);

                if (country.Government.LegislativeBody !=
                    LegislativeBodyType.None)
                {
                    country.Government.LegislativeIndependence += 2;
                }

                return
                    $"Extraordinary collections and negotiated grants bring roughly {receipts:N0} into the treasury. The cash is immediate, but merchants and regional interests resent the burden and representative institutions gain leverage from being asked to consent.";
            }

            case PoliticalCrisisResponse.CutStateCommitments:
                country.CourtFunding -= 0.15m;
                country.ArmyFunding -= 0.10m;
                country.AdministrationFunding -= 0.10m;
                country.Government.Stability -= 2;
                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.Aristocracy,
                    -4);
                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.Military,
                    -3);
                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.Bureaucracy,
                    -3);

                return
                    "The government announces broad retrenchment. Future monthly spending falls, but courtiers, soldiers and officials all lose resources and political support.";

            case PoliticalCrisisResponse.BorrowForTime:
            {
                var loan = country.Gdp * 0.015m;
                country.Debt += loan;
                country.Treasury += loan;
                country.Government.Stability += 1;

                return
                    $"The government secures roughly {loan:N0} in new credit. Immediate obligations can be met, but the debt burden grows and the same fiscal crisis may return at a worse stage.";
            }

            default:
                return "No fiscal response was carried out.";
        }
    }

    private static string ApplyPoliticalResponse(
        GameState state,
        PoliticalCrisis crisis,
        PoliticalCrisisResponse response)
    {
        var country = crisis.Country;
        var bloc = FindRelatedBloc(state, crisis) ??
            GetStandoffBloc(state, country);

        if (bloc is null)
            return "The opposition coalition has already fragmented.";

        switch (response)
        {
            case PoliticalCrisisResponse.CooptOpposition:
            {
                var cost = country.Gdp * 0.0015m;
                PayFromTreasury(country, cost);

                bloc.Cohesion = Math.Max(
                    0,
                    bloc.Cohesion - 14);
                bloc.Leader.Influence =
                    Math.Min(
                        100,
                        bloc.Leader.Influence + 3);
                country.Government.Stability += 2;

                foreach (var powerBase in bloc.PowerBases)
                {
                    country.Ruler.ChangePowerBaseStanding(
                        powerBase,
                        5);
                }

                return
                    $"Patronage, access and targeted concessions cost roughly {cost:N0} and split parts of the opposition coalition. The government gains breathing room, although {bloc.Leader.FullName} also becomes more central to national politics.";
            }

            case PoliticalCrisisResponse.ConstitutionalCompromise:
                country.Government.LegislativeIndependence += 7;
                country.Government.Stability += 4;
                bloc.Cohesion = Math.Max(
                    0,
                    bloc.Cohesion - 12);

                foreach (var powerBase in bloc.PowerBases)
                {
                    country.Ruler.ChangePowerBaseStanding(
                        powerBase,
                        3);
                }

                return
                    $"The ruler offers organised interests a stronger formal role in government. The standoff cools, but the settlement permanently increases the leverage of {InstitutionSystem.BodyName(country.Government.LegislativeBody)} and future rulers will inherit that constraint.";

            case PoliticalCrisisResponse.ConfrontOpposition:
                return ConfrontOpposition(
                    state,
                    crisis,
                    bloc);

            default:
                return "No political response was carried out.";
        }
    }

    private static string ApplySuccessionResponse(
        GameState state,
        PoliticalCrisis crisis,
        PoliticalCrisisResponse response)
    {
        var country = crisis.Country;
        var candidates =
            ActiveSuccessionCandidates(country);

        if (candidates.Count == 0)
            return "No living recognised successor remains to settle.";

        var heir = candidates[0];
        var rival = candidates.Count > 1
            ? candidates[1]
            : null;

        switch (response)
        {
            case PoliticalCrisisResponse.PubliclyNameSuccessor:
                heir.Legitimacy += 13;
                heir.Influence += 5;
                country.Government.Stability += 2;

                if (rival is not null)
                {
                    rival.Legitimacy -= 3;

                    var rivalToRuler =
                        state.Relationships.GetOrCreate(
                            rival,
                            country.Ruler);
                    rivalToRuler.ChangeOpinion(-8);
                    rivalToRuler.ChangeTrust(-5);
                }

                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.RoyalFamily,
                    4);
                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.Aristocracy,
                    -2);

                return rival is null
                    ? $"The ruler publicly confirms {heir.FullName} as the successor. The clear designation strengthens the heir's legitimacy."
                    : $"The ruler publicly confirms {heir.FullName} as the successor. The designation strengthens the heir quickly, but {rival.FullName}'s faction loses hope of a negotiated opening.";

            case PoliticalCrisisResponse.BalanceSuccessionFactions:
                country.Government.Stability += 3;
                heir.Influence += 2;

                if (rival is not null)
                    rival.Influence += 3;

                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.Aristocracy,
                    3);
                country.Ruler.ChangePowerBaseStanding(
                    PowerBaseType.RegionalElites,
                    3);

                return
                    "The ruler distributes assurances and access across the competing succession factions. Court tension falls for now, but no claimant has been made strong enough to end the underlying uncertainty.";

            case PoliticalCrisisResponse.ConveneSuccessionSettlement:
                heir.Legitimacy += 9;
                heir.Influence += 3;
                country.Government.Stability += 4;

                if (rival is not null)
                {
                    rival.Legitimacy -= 2;

                    var rivalToHeir =
                        state.Relationships.GetOrCreate(
                            rival,
                            heir);
                    rivalToHeir.ChangeTrust(-2);
                }

                if (country.Government.LegislativeBody !=
                    LegislativeBodyType.None)
                {
                    country.Government.LegislativeIndependence += 5;
                }

                return
                    $"Leading institutions and magnates are assembled around a public settlement recognising {heir.FullName}. The heir gains broader legitimacy, but the bodies asked to guarantee the succession gain a durable precedent for involvement.";

            default:
                return "No succession response was carried out.";
        }
    }

    private static string ConfrontOpposition(
        GameState state,
        PoliticalCrisis crisis,
        PoliticalBloc bloc)
    {
        var country = crisis.Country;

        var governmentStrength =
            country.Government.Stability * 0.45 +
            PoliticalCalculations.GetPowerBaseInfluence(
                country,
                country.Ruler) * 0.35 +
            country.Ruler.Legitimacy * 0.20;

        var oppositionStrength =
            bloc.Cohesion * 0.45 +
            bloc.Leader.Influence * 0.30 +
            PoliticalCalculations.GetThreatScore(
                state,
                country,
                bloc.Leader) * 0.25;

        var noise =
            (state.Random.NextDouble() - 0.5) * 14;

        if (governmentStrength + noise >=
            oppositionStrength)
        {
            bloc.Cohesion = Math.Max(
                0,
                bloc.Cohesion - 22);
            bloc.Leader.Influence = Math.Max(
                0,
                bloc.Leader.Influence - 8);
            country.Government.Stability += 2;

            return
                $"The government successfully isolates {bloc.Leader.FullName} and forces wavering supporters away from the coalition. Authority improves, though the defeated opposition retains its underlying grievances.";
        }

        bloc.Cohesion = Math.Min(
            100,
            bloc.Cohesion + 14);
        bloc.Leader.Influence = Math.Min(
            100,
            bloc.Leader.Influence + 6);
        country.Government.Stability -= 5;
        country.PublicUnrest += 3;

        if (crisis.Stage >= 2 &&
            !state.Plots.Any(plot =>
                !plot.IsResolved &&
                ReferenceEquals(plot.Country, country) &&
                ReferenceEquals(
                    plot.Instigator,
                    bloc.Leader)))
        {
            state.Plots.Add(new PoliticalPlot
            {
                Country = country,
                Instigator = bloc.Leader,
                Progress = 30
            });
        }

        return
            $"The attempt to break the opposition backfires. {bloc.Leader.FullName}'s coalition closes ranks, the government looks weaker, and more radical action is now easier to organise.";
    }

    private static void BreakAgainstGovernment(
        GameState state,
        PoliticalCrisis crisis,
        List<SimulationReport> reports)
    {
        crisis.Status =
            PoliticalCrisisStatus.BrokeAgainstGovernment;
        crisis.ResolvedOn = state.Date;

        var details = crisis.Type switch
        {
            PoliticalCrisisType.RegionalBreakdown =>
                RegionalBreak(crisis),

            PoliticalCrisisType.FiscalEmergency =>
                FiscalBreak(crisis),

            PoliticalCrisisType.PoliticalStandoff =>
                PoliticalBreak(state, crisis),

            PoliticalCrisisType.SuccessionDispute =>
                SuccessionBreak(state, crisis),

            PoliticalCrisisType.WarEmergency =>
                WarEmergencyBreak(state, crisis),

            _ => "The crisis breaks against the government."
        };

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{Title(crisis)} reaches breaking point",
            details));
    }

    private static string RegionalBreak(
        PoliticalCrisis crisis)
    {
        var region = crisis.Region!;

        region.CrownControl -= 14;
        region.LocalElitePower += 10;
        region.Privileges += 10;
        region.Unrest -= 8;

        crisis.Country.Government.Stability -= 6;
        crisis.Country.SetPowerBaseStrength(
            PowerBaseType.RegionalElites,
            crisis.Country.GetPowerBaseStrength(
                PowerBaseType.RegionalElites) + 4);

        return
            $"{region.Name} does not leave the state, but the government is forced into a humiliating retreat. Local elites secure wider practical autonomy, disorder subsides on their terms, and the centre loses authority.";
    }

    private static string FiscalBreak(
        PoliticalCrisis crisis)
    {
        var country = crisis.Country;
        var emergencyCredit =
            country.Gdp * 0.010m;

        country.Treasury += emergencyCredit;
        country.Debt += country.Gdp * 0.030m;
        country.CourtFunding =
            Math.Min(country.CourtFunding, 0.85m);
        country.ArmyFunding =
            Math.Min(country.ArmyFunding, 0.90m);
        country.AdministrationFunding =
            Math.Min(country.AdministrationFunding, 0.90m);
        country.Government.Stability -= 6;

        return
            $"Creditors and office-holders force an emergency settlement on the government. New borrowing keeps the state operating, but spending is cut, debt rises sharply and the regime loses political authority.";
    }

    private static string PoliticalBreak(
        GameState state,
        PoliticalCrisis crisis)
    {
        var country = crisis.Country;
        var bloc = FindRelatedBloc(state, crisis) ??
            GetStandoffBloc(state, country);

        country.Government.Stability -= 8;
        country.Ruler.Legitimacy -= 8;

        if (bloc is not null)
        {
            bloc.Cohesion = Math.Min(
                100,
                bloc.Cohesion + 12);
            bloc.Leader.Influence = Math.Min(
                100,
                bloc.Leader.Influence + 10);

            if (!state.Plots.Any(plot =>
                    !plot.IsResolved &&
                    ReferenceEquals(plot.Country, country) &&
                    ReferenceEquals(
                        plot.Instigator,
                        bloc.Leader)))
            {
                state.Plots.Add(new PoliticalPlot
                {
                    Country = country,
                    Instigator = bloc.Leader,
                    Progress = 35
                });
            }

            return
                $"The government fails to settle the confrontation. {bloc.Leader.FullName} emerges as a stronger national alternative, the ruler's legitimacy falls, and a more dangerous extra-constitutional challenge begins to form.";
        }

        return
            "The confrontation badly damages the ruler's legitimacy and leaves the government politically fractured.";
    }

    private static string SuccessionBreak(
        GameState state,
        PoliticalCrisis crisis)
    {
        var country = crisis.Country;
        var candidates =
            ActiveSuccessionCandidates(country);

        country.Government.Stability -= 6;

        if (candidates.Count < 2)
        {
            if (candidates.Count == 1)
                candidates[0].Legitimacy -= 5;

            return
                "The court reaches the succession without a convincing settlement. Confidence in an orderly transfer of power deteriorates and the regime enters the future transition weakened.";
        }

        var heir = candidates[0];
        var rival = candidates[1];

        rival.Influence += 8;
        rival.Legitimacy += 5;
        heir.Legitimacy -= 6;

        var existingBloc =
            state.PoliticalBlocs.FirstOrDefault(bloc =>
                bloc.IsActive &&
                ReferenceEquals(
                    bloc.Country,
                    country) &&
                ReferenceEquals(
                    bloc.Leader,
                    rival));

        if (existingBloc is null &&
            rival.IsPoliticallyActive)
        {
            var bloc = new PoliticalBloc
            {
                Country = country,
                Leader = rival,
                Cohesion = 65
            };

            bloc.PowerBases.Add(
                PowerBaseType.Aristocracy);
            bloc.PowerBases.Add(
                PowerBaseType.RegionalElites);
            bloc.PowerBases.Add(
                PowerBaseType.RoyalFamily);

            state.PoliticalBlocs.Add(bloc);
        }

        return
            $"The ruler fails to impose a durable succession settlement. {rival.FullName} now leads a recognisable rival faction, while {heir.FullName}'s claim looks less inevitable. The eventual succession is likely to become a political struggle rather than a routine transfer.";
    }

    private static List<Character> ActiveSuccessionCandidates(
        Country country) =>
        country.SuccessionOrder
            .Where(candidate =>
                candidate.IsPoliticallyActive &&
                !ReferenceEquals(
                    candidate,
                    country.Ruler))
            .DistinctBy(candidate =>
                candidate.Id)
            .ToList();

    private static bool SuccessionIsSettled(
        Country country)
    {
        var candidates =
            ActiveSuccessionCandidates(country);

        if (candidates.Count == 0)
            return true;

        if (candidates.Count == 1)
            return candidates[0].Legitimacy >= 68;

        return
            candidates[0].Legitimacy >= 75 &&
            candidates[0].Legitimacy -
                candidates[1].Legitimacy >= 15;
    }

    private static string SuccessionSummary(
        Country country)
    {
        var candidates =
            ActiveSuccessionCandidates(country);

        if (candidates.Count == 0)
            return
                "The ruler is ageing or unwell, but no recognised living successor has enough standing to make the future transfer of power routine.";

        if (candidates.Count == 1)
            return
                $"{candidates[0].FullName} is the recognised successor, but the claim is not yet strong enough to prevent factions from preparing for uncertainty around the future transfer of power.";

        return
            $"{candidates[0].FullName} stands first in the succession, while {candidates[1].FullName} remains a plausible focus for rival factions. The problem is not the legal order alone: elites are deciding which future ruler they can live with.";
    }

    private static PoliticalBloc? FindRelatedBloc(
        GameState state,
        PoliticalCrisis crisis)
    {
        if (!crisis.RelatedBlocId.HasValue)
            return null;

        return state.PoliticalBlocs.FirstOrDefault(bloc =>
            bloc.Id == crisis.RelatedBlocId.Value &&
            bloc.IsActive);
    }

    private static bool HasRecentRegionalCrisis(
        GameState state,
        Country country,
        Region region)
    {
        return state.PoliticalCrises.Any(crisis =>
            ReferenceEquals(crisis.Country, country) &&
            crisis.Type ==
                PoliticalCrisisType.RegionalBreakdown &&
            crisis.Region?.Id == region.Id &&
            MonthsSinceEndOrStart(
                crisis,
                state.Date) < 9);
    }

    private static bool HasRecentTypeCrisis(
        GameState state,
        Country country,
        PoliticalCrisisType type,
        int cooldownMonths)
    {
        return state.PoliticalCrises.Any(crisis =>
            ReferenceEquals(crisis.Country, country) &&
            crisis.Type == type &&
            MonthsSinceEndOrStart(
                crisis,
                state.Date) < cooldownMonths);
    }

    private static int MonthsSinceEndOrStart(
        PoliticalCrisis crisis,
        GameDate now)
    {
        var date =
            crisis.ResolvedOn ??
            crisis.StartedOn;

        return
            (now.Year - date.Year) * 12 +
            now.Month -
            date.Month;
    }

    private static decimal DebtRatio(
        Country country) =>
        country.Gdp <= 0
            ? 0
            : country.Debt / country.Gdp;

    private static void PayFromTreasury(
        Country country,
        decimal amount)
    {
        if (country.Treasury >= amount)
        {
            country.Treasury -= amount;
            return;
        }

        var shortfall =
            amount - country.Treasury;
        country.Treasury = 0;
        country.Debt += shortfall;
    }

    private static string PoliticalStandoffSummary(
        GameState state,
        PoliticalCrisis crisis)
    {
        var bloc =
            FindRelatedBloc(state, crisis);

        return bloc is null
            ? "Organised opposition has turned a weak government into a sustained political confrontation."
            : $"{bloc.Leader.FullName}'s coalition is cohesive enough to challenge the government's authority openly. The ruler must split it, compromise with it or try to defeat it.";
    }
}
