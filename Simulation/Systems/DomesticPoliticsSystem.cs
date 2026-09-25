using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal sealed record PowerBaseDemandResponse(
    Order? Order,
    SimulationReport Report);

internal static class DomesticPoliticsSystem
{
    private const int DemandStandingThreshold = 40;
    private const int BlocStandingThreshold = 35;
    private const int MinimumStructuralStrength = 40;
    private const int RejectedDemandCooldownMonths = 6;

    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var country in state.Countries)
        {
            var activeDemands = state.PowerBaseDemands
                .Where(demand =>
                    !demand.IsResolved &&
                    ReferenceEquals(demand.Country, country))
                .ToList();

            foreach (var demand in activeDemands)
                ProcessDemand(state, demand, reports);

            if (state.Date.Month % 3 == 0 && country.Ruler.IsAlive)
                CreateNextDemandIfNeeded(state, country, reports);

            UpdatePoliticalBloc(state, country, reports);
        }

        return reports;
    }

    private static void CreateNextDemandIfNeeded(
        GameState state,
        Countries.Country country,
        List<SimulationReport> reports)
    {
        var activeDemandBases = state.PowerBaseDemands
            .Where(demand =>
                !demand.IsResolved &&
                ReferenceEquals(demand.Country, country))
            .Select(demand => demand.PowerBase)
            .ToHashSet();

        var recentlyRejectedBases = state.PowerBaseDemands
            .Where(demand =>
                demand.IsResolved &&
                demand.IsRejected &&
                demand.ResolvedOn.HasValue &&
                ReferenceEquals(demand.Country, country) &&
                MonthsBetween(demand.ResolvedOn.Value, state.Date) <
                    RejectedDemandCooldownMonths)
            .Select(demand => demand.PowerBase)
            .ToHashSet();

        var candidate = Enum.GetValues<PowerBaseType>()
            .Select(powerBase => new
            {
                PowerBase = powerBase,
                Strength = country.GetPowerBaseStrength(powerBase),
                Standing = country.Ruler.GetPowerBaseStanding(powerBase)
            })
            .Where(entry =>
                entry.Strength >= MinimumStructuralStrength &&
                entry.Standing < DemandStandingThreshold &&
                !activeDemandBases.Contains(entry.PowerBase) &&
                !recentlyRejectedBases.Contains(entry.PowerBase))
            .OrderBy(entry => entry.Standing)
            .ThenByDescending(entry => entry.Strength)
            .FirstOrDefault();

        if (candidate is null)
            return;

        var demand = CreateDemand(state, country, candidate.PowerBase);
        demand.Spokesperson = FindSpokesperson(
            state,
            country,
            candidate.PowerBase);
        state.PowerBaseDemands.Add(demand);

        if (!ReferenceEquals(country, state.Player.Country))
            return;

        var spokespersonText = demand.Spokesperson is null
            ? $"{FormatPowerBase(candidate.PowerBase)} demands action"
            : $"{demand.Spokesperson.FullName} presents " +
              $"{FormatPowerBase(candidate.PowerBase).ToLowerInvariant()} demands";

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            spokespersonText,
            DescribeDemand(demand, initial: true) +
            (demand.Spokesperson is null
                ? string.Empty
                : $" {demand.Spokesperson.FullName} has become the visible advocate for the demand.")));
    }

    private static void ProcessDemand(
        GameState state,
        PowerBaseDemand demand,
        List<SimulationReport> reports)
    {
        if (demand.Spokesperson is not null &&
            !demand.Spokesperson.IsPoliticallyActive)
        {
            demand.Spokesperson = FindSpokesperson(
                state,
                demand.Country,
                demand.PowerBase);
        }

        if (IsSatisfied(state, demand))
        {
            demand.IsResolved = true;
            demand.IsRejected = false;
            demand.ResolvedOn = state.Date;
            demand.Country.Ruler.ChangePowerBaseStanding(demand.PowerBase, 10);
            demand.Country.Government.Stability += 1.5;

            if (demand.Spokesperson is not null)
            {
                demand.Spokesperson.ChangePowerBaseStanding(
                    demand.PowerBase,
                    2);
                demand.Spokesperson.Influence = Math.Min(
                    100,
                    demand.Spokesperson.Influence + 1);

                var towardRuler = state.Relationships.GetOrCreate(
                    demand.Spokesperson,
                    demand.Country.Ruler);
                towardRuler.ChangeOpinion(5);
                towardRuler.ChangeTrust(4);
            }

            if (ReferenceEquals(demand.Country, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"{FormatPowerBase(demand.PowerBase)} demand satisfied",
                    $"{DescribeDemand(demand, initial: false)} The concession improves " +
                    $"the ruler's standing with the {FormatPowerBase(demand.PowerBase).ToLowerInvariant()} " +
                    "and eases political pressure." +
                    (demand.Spokesperson is null
                        ? string.Empty
                        : $" {demand.Spokesperson.FullName} receives political credit for securing it.")));
            }

            return;
        }

        demand.MonthsOpen++;

        if (demand.MonthsOpen % 3 != 0)
            return;

        demand.EscalationLevel++;

        var strength = demand.Country.GetPowerBaseStrength(demand.PowerBase);
        var severity = Math.Max(1, demand.EscalationLevel);

        demand.Country.Ruler.ChangePowerBaseStanding(
            demand.PowerBase,
            -(4 + severity * 2));
        demand.Country.PublicUnrest += strength / 40.0 * severity;
        demand.Country.Government.Stability -= strength / 50.0 * severity;

        var rival = demand.Spokesperson is { IsPoliticallyActive: true }
            ? demand.Spokesperson
            : FindBestRival(state, demand.Country, demand.PowerBase);

        if (rival is not null)
        {
            rival.ChangePowerBaseStanding(
                demand.PowerBase,
                3 + severity * 2);
            rival.Influence = Math.Min(
                100,
                rival.Influence + 1);

            var towardRuler = state.Relationships.GetOrCreate(
                rival,
                demand.Country.Ruler);
            towardRuler.ChangeOpinion(-(3 + severity));
            towardRuler.ChangeTrust(-2);
        }

        if (!ReferenceEquals(demand.Country, state.Player.Country))
            return;

        var rivalText = rival is null
            ? string.Empty
            : $" {rival.FullName} is increasingly benefiting from the group's frustration.";

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{FormatPowerBase(demand.PowerBase)} pressure escalates",
            $"{DescribeDemand(demand, initial: false)} It has now gone unanswered for " +
            $"{demand.MonthsOpen} months. Political stability is deteriorating.{rivalText}"));
    }

    public static PowerBaseDemandResponse RespondToDemand(
        GameState state,
        Guid demandId,
        bool concede)
    {
        var demand = state.PowerBaseDemands.FirstOrDefault(candidate =>
            candidate.Id == demandId &&
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Country, state.Player.Country));

        if (demand is null)
        {
            return new PowerBaseDemandResponse(
                null,
                new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    "Political demand unavailable",
                    "That political demand is no longer active."));
        }

        return concede
            ? ConcedeDemand(state, demand)
            : RejectDemand(state, demand);
    }

    private static PowerBaseDemandResponse ConcedeDemand(
        GameState state,
        PowerBaseDemand demand)
    {
        var pending = FindPendingConcessionOrder(state, demand);

        if (pending is not null)
        {
            return new PowerBaseDemandResponse(
                null,
                new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    "Concession already in motion",
                    $"The ruler has already ordered action on the " +
                    $"{FormatPowerBase(demand.PowerBase).ToLowerInvariant()} demand. " +
                    "The group will judge the government by what is actually implemented."));
        }

        var order = CreateConcessionOrder(state, demand);

        if (order is null)
        {
            return new PowerBaseDemandResponse(
                null,
                new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    "Concession cannot be carried out",
                    $"The ruler is willing to concede, but the responsible office or " +
                    "political situation does not currently allow a valid order to be issued."));
        }

        if (!demand.AcknowledgedByRuler)
        {
            demand.AcknowledgedByRuler = true;
            demand.Country.Ruler.ChangePowerBaseStanding(
                demand.PowerBase,
                2);

            if (demand.Spokesperson is not null)
            {
                var towardRuler = state.Relationships.GetOrCreate(
                    demand.Spokesperson,
                    demand.Country.Ruler);
                towardRuler.ChangeOpinion(3);
                towardRuler.ChangeTrust(2);
            }
        }

        return new PowerBaseDemandResponse(
            order,
            new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{FormatPowerBase(demand.PowerBase)} concession promised",
                $"The ruler agrees to pursue the requested concession: " +
                $"{DescribeDemandRequest(demand)} A formal order has been queued, " +
                "but the demand remains active until the policy is actually delivered."));
    }

    private static PowerBaseDemandResponse RejectDemand(
        GameState state,
        PowerBaseDemand demand)
    {
        demand.IsResolved = true;
        demand.IsRejected = true;
        demand.ResolvedOn = state.Date;

        var strength =
            demand.Country.GetPowerBaseStrength(demand.PowerBase);
        var severity = 1 + Math.Max(0, demand.EscalationLevel);

        demand.Country.Ruler.ChangePowerBaseStanding(
            demand.PowerBase,
            -(8 + demand.EscalationLevel * 2));

        demand.Country.Government.Stability -=
            Math.Max(1.0, strength / 45.0) +
            demand.EscalationLevel * 0.75;

        demand.Country.PublicUnrest +=
            Math.Max(1.0, strength / 35.0) *
            (1.0 + demand.EscalationLevel * 0.35);

        if (demand.Spokesperson is not null)
        {
            demand.Spokesperson.ChangePowerBaseStanding(
                demand.PowerBase,
                5 + severity * 2);
            demand.Spokesperson.Influence = Math.Min(
                100,
                demand.Spokesperson.Influence + 2);

            var towardRuler = state.Relationships.GetOrCreate(
                demand.Spokesperson,
                demand.Country.Ruler);
            towardRuler.ChangeOpinion(-(10 + severity * 3));
            towardRuler.ChangeTrust(-(6 + severity * 2));
        }

        var spokespersonText = demand.Spokesperson is null
            ? string.Empty
            : $" {demand.Spokesperson.FullName} emerges from the refusal with a " +
              "stronger claim to speak for the group.";

        return new PowerBaseDemandResponse(
            null,
            new SimulationReport(
                state.Date,
                ReportCategory.Politics,
                $"{FormatPowerBase(demand.PowerBase)} demand rejected",
                $"The ruler explicitly refuses the demand: {DescribeDemandRequest(demand)} " +
                "The immediate petition is closed, but the refusal damages support, " +
                "raises unrest and gives the group a fresh grievance." +
                spokespersonText));
    }

    private static Order? FindPendingConcessionOrder(
        GameState state,
        PowerBaseDemand demand)
    {
        return state.PendingOrders.FirstOrDefault(order =>
            demand.Type switch
            {
                PowerBaseDemandType.LowerTaxes =>
                    order is ChangeTaxOrder tax &&
                    ReferenceEquals(tax.Country, demand.Country) &&
                    tax.TargetTaxRate <= demand.TargetValue,

                PowerBaseDemandType.RaiseArmyFunding =>
                    order is SetBudgetOrder budget &&
                    ReferenceEquals(budget.Country, demand.Country) &&
                    budget.TargetArmyFunding >= demand.TargetValue,

                PowerBaseDemandType.RaiseAdministrationFunding =>
                    order is SetBudgetOrder budget &&
                    ReferenceEquals(budget.Country, demand.Country) &&
                    budget.TargetAdministrationFunding >= demand.TargetValue,

                PowerBaseDemandType.RaiseCourtFunding =>
                    order is SetBudgetOrder budget &&
                    ReferenceEquals(budget.Country, demand.Country) &&
                    budget.TargetCourtFunding >= demand.TargetValue,

                PowerBaseDemandType.EndWar =>
                    order is OfferPeaceOrder peace &&
                    ReferenceEquals(peace.Country, demand.Country),

                _ => false
            });
    }

    private static Order? CreateConcessionOrder(
        GameState state,
        PowerBaseDemand demand)
    {
        var country = demand.Country;
        var ruler = country.Ruler;

        switch (demand.Type)
        {
            case PowerBaseDemandType.LowerTaxes:
            {
                var treasurer = country.GetOfficeHolder(Position.Treasurer);
                return treasurer is null
                    ? null
                    : new ChangeTaxOrder
                    {
                        Issuer = ruler,
                        Recipient = treasurer,
                        IssuedOn = state.Date,
                        Country = country,
                        TargetTaxRate = demand.TargetValue
                    };
            }

            case PowerBaseDemandType.RaiseArmyFunding:
            case PowerBaseDemandType.RaiseAdministrationFunding:
            case PowerBaseDemandType.RaiseCourtFunding:
            {
                var treasurer = country.GetOfficeHolder(Position.Treasurer);
                if (treasurer is null)
                    return null;

                return new SetBudgetOrder
                {
                    Issuer = ruler,
                    Recipient = treasurer,
                    IssuedOn = state.Date,
                    Country = country,
                    TargetArmyFunding =
                        demand.Type == PowerBaseDemandType.RaiseArmyFunding
                            ? demand.TargetValue
                            : country.ArmyFunding,
                    TargetAdministrationFunding =
                        demand.Type == PowerBaseDemandType.RaiseAdministrationFunding
                            ? demand.TargetValue
                            : country.AdministrationFunding,
                    TargetCourtFunding =
                        demand.Type == PowerBaseDemandType.RaiseCourtFunding
                            ? demand.TargetValue
                            : country.CourtFunding
                };
            }

            case PowerBaseDemandType.EndWar:
            {
                var chancellor =
                    country.GetOfficeHolder(Position.Chancellor);
                var war = state.Wars
                    .Where(candidate =>
                        candidate.Status == WarStatus.Active &&
                        candidate.IsParticipant(country))
                    .OrderByDescending(candidate => candidate.MonthsActive)
                    .FirstOrDefault();

                if (chancellor is null || war is null)
                    return null;

                return new OfferPeaceOrder
                {
                    Issuer = ruler,
                    Recipient = chancellor,
                    IssuedOn = state.Date,
                    Country = country,
                    War = war,
                    Terms = PeaceOfferTerms.WhitePeace
                };
            }

            default:
                return null;
        }
    }

    private static void UpdatePoliticalBloc(
        GameState state,
        Countries.Country country,
        List<SimulationReport> reports)
    {
        var bloc = state.PoliticalBlocs.FirstOrDefault(candidate =>
            candidate.IsActive &&
            ReferenceEquals(candidate.Country, country));

        if (bloc is not null &&
            (!bloc.Leader.IsPoliticallyActive ||
             ReferenceEquals(bloc.Leader, country.Ruler)))
        {
            DissolveBloc(state, bloc, reports, "its leader can no longer organise opposition");
            bloc = null;
        }

        var disaffectedBases = GetDisaffectedPowerBases(state, country);

        if (bloc is null)
        {
            if (disaffectedBases.Count < 2)
                return;

            var leader = FindBlocLeader(state, country, disaffectedBases);
            if (leader is null)
                return;

            var supportiveBases = disaffectedBases
                .Where(powerBase =>
                    leader.GetPowerBaseStanding(powerBase) >=
                    country.Ruler.GetPowerBaseStanding(powerBase) + 10)
                .ToList();

            if (supportiveBases.Count < 2)
                return;

            bloc = new PoliticalBloc
            {
                Country = country,
                Leader = leader
            };

            bloc.PowerBases.UnionWith(supportiveBases);
            UpdateBlocMembership(state, bloc);
            bloc.Cohesion = CalculateBlocCohesion(bloc);
            state.PoliticalBlocs.Add(bloc);

            if (ReferenceEquals(country, state.Player.Country))
            {
                reports.Add(new SimulationReport(
                    state.Date,
                    ReportCategory.Politics,
                    $"Opposition coalesces around {leader.FullName}",
                    $"{leader.FullName} is now coordinating an organised opposition bloc " +
                    $"supported by {FormatPowerBaseList(bloc.PowerBases)}. This is not " +
                    "yet a coup, but coordinated opposition makes the government more vulnerable."));
            }

            return;
        }

        bloc.MonthsActive++;

        var qualifyingBases = disaffectedBases
            .Where(powerBase =>
                bloc.Leader.GetPowerBaseStanding(powerBase) >=
                country.Ruler.GetPowerBaseStanding(powerBase) + 5)
            .ToList();

        if (qualifyingBases.Count < 2)
        {
            DissolveBloc(state, bloc, reports, "too few important groups remain committed to it");
            return;
        }

        bloc.PowerBases.Clear();
        bloc.PowerBases.UnionWith(qualifyingBases);
        UpdateBlocMembership(state, bloc);
        bloc.Cohesion = CalculateBlocCohesion(bloc);
    }

    private static List<PowerBaseType> GetDisaffectedPowerBases(
        GameState state,
        Countries.Country country)
    {
        return Enum.GetValues<PowerBaseType>()
            .Where(powerBase =>
                country.GetPowerBaseStrength(powerBase) >= MinimumStructuralStrength &&
                IsPowerBaseDisaffected(state, country, powerBase))
            .ToList();
    }

    private static bool IsPowerBaseDisaffected(
        GameState state,
        Countries.Country country,
        PowerBaseType powerBase)
    {
        if (country.Ruler.GetPowerBaseStanding(powerBase) < BlocStandingThreshold)
            return true;

        return state.PowerBaseDemands.Any(demand =>
            !demand.IsResolved &&
            demand.EscalationLevel > 0 &&
            ReferenceEquals(demand.Country, country) &&
            demand.PowerBase == powerBase);
    }

    private static Character? FindBlocLeader(
        GameState state,
        Countries.Country country,
        IReadOnlyCollection<PowerBaseType> disaffectedBases)
    {
        var rulerAverage = disaffectedBases.Average(powerBase =>
            country.Ruler.GetPowerBaseStanding(powerBase));

        return country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, country.Ruler) &&
                character.Ambition >= 40)
            .Select(character => new
            {
                Character = character,
                AverageBacking = disaffectedBases.Average(powerBase =>
                    character.GetPowerBaseStanding(powerBase)),
                Willingness = PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    character,
                    country.Ruler)
            })
            .Where(entry =>
                entry.AverageBacking >= rulerAverage + 10)
            .OrderByDescending(entry =>
                entry.AverageBacking * 0.45 +
                entry.Character.Ambition * 0.20 +
                entry.Character.Influence * 0.20 +
                (100 - entry.Willingness) * 0.15)
            .Select(entry => entry.Character)
            .FirstOrDefault();
    }

    private static void UpdateBlocMembership(
        GameState state,
        PoliticalBloc bloc)
    {
        bloc.MemberIds.Clear();

        foreach (var character in bloc.Country.PoliticalFigures)
        {
            if (!character.IsPoliticallyActive ||
                ReferenceEquals(character, bloc.Country.Ruler) ||
                ReferenceEquals(character, bloc.Leader))
            {
                continue;
            }

            var towardLeader = state.Relationships.GetOrCreate(
                character,
                bloc.Leader);
            var towardRuler = state.Relationships.GetOrCreate(
                character,
                bloc.Country.Ruler);

            var normalisedLeaderOpinion = (towardLeader.Opinion + 100) / 2.0;
            var backing = bloc.PowerBases.Average(powerBase =>
                character.GetPowerBaseStanding(powerBase));

            var membershipScore =
                towardLeader.Trust * 0.25 +
                normalisedLeaderOpinion * 0.20 +
                (100 - towardRuler.Trust) * 0.15 +
                backing * 0.25 +
                character.Ambition * 0.15;

            if (membershipScore >= 60)
                bloc.MemberIds.Add(character.Id);
        }
    }

    private static double CalculateBlocCohesion(PoliticalBloc bloc)
    {
        var ruler = bloc.Country.Ruler;

        var grievance = bloc.PowerBases.Average(powerBase =>
            100 - ruler.GetPowerBaseStanding(powerBase));
        var leaderBacking = bloc.PowerBases.Average(powerBase =>
            bloc.Leader.GetPowerBaseStanding(powerBase));

        return Math.Clamp(
            grievance * 0.55 +
            leaderBacking * 0.45,
            0,
            100);
    }

    private static void DissolveBloc(
        GameState state,
        PoliticalBloc bloc,
        List<SimulationReport> reports,
        string reason)
    {
        bloc.IsActive = false;

        if (!ReferenceEquals(bloc.Country, state.Player.Country))
            return;

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.Politics,
            $"{bloc.Leader.FullName}'s opposition bloc fractures",
            $"The organised opposition around {bloc.Leader.FullName} has broken apart because {reason}."));
    }

    private static Character? FindSpokesperson(
        GameState state,
        Countries.Country country,
        PowerBaseType powerBase)
    {
        return country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, country.Ruler))
            .Select(character => new
            {
                Character = character,
                Standing = character.GetPowerBaseStanding(powerBase),
                OfficeBonus = RelevantOfficeBonus(
                    character.Position,
                    powerBase),
                Willingness = PoliticalCalculations.GetOrderWillingness(
                    state,
                    country,
                    character,
                    country.Ruler)
            })
            .Where(entry => entry.Standing >= 55)
            .OrderByDescending(entry =>
                entry.Standing * 0.50 +
                entry.Character.Influence * 0.20 +
                entry.Character.Ambition * 0.15 +
                (100 - entry.Willingness) * 0.10 +
                entry.OfficeBonus)
            .Select(entry => entry.Character)
            .FirstOrDefault();
    }

    private static int RelevantOfficeBonus(
        Position? position,
        PowerBaseType powerBase)
    {
        return (position, powerBase) switch
        {
            (Position.Marshal, PowerBaseType.Military) => 12,
            (Position.Treasurer, PowerBaseType.Merchants) => 10,
            (Position.Treasurer, PowerBaseType.Bureaucracy) => 8,
            (Position.Chancellor, PowerBaseType.Bureaucracy) => 10,
            (Position.Chancellor, PowerBaseType.Party) => 10,
            (Position.Chancellor, PowerBaseType.RegionalElites) => 8,
            (Position.Chancellor, PowerBaseType.Aristocracy) => 6,
            _ => 0
        };
    }

    private static PowerBaseDemand CreateDemand(
        GameState state,
        Countries.Country country,
        PowerBaseType powerBase)
    {
        var activeWar = state.Wars.Any(war =>
            war.Status == WarStatus.Active &&
            war.IsParticipant(country));

        if (powerBase == PowerBaseType.Military &&
            activeWar &&
            country.WarExhaustion >= 45)
        {
            return NewDemand(country, powerBase, PowerBaseDemandType.EndWar);
        }

        return powerBase switch
        {
            PowerBaseType.Military =>
                NewDemand(
                    country,
                    powerBase,
                    PowerBaseDemandType.RaiseArmyFunding,
                    Math.Min(1.5m, Math.Max(1.2m, country.ArmyFunding + 0.20m))),

            PowerBaseType.Bureaucracy =>
                NewDemand(
                    country,
                    powerBase,
                    PowerBaseDemandType.RaiseAdministrationFunding,
                    Math.Min(1.5m, Math.Max(1.15m, country.AdministrationFunding + 0.15m))),

            PowerBaseType.Aristocracy or
            PowerBaseType.Clergy or
            PowerBaseType.RegionalElites or
            PowerBaseType.RoyalFamily =>
                NewDemand(
                    country,
                    powerBase,
                    PowerBaseDemandType.RaiseCourtFunding,
                    Math.Min(1.5m, Math.Max(1.15m, country.CourtFunding + 0.15m))),

            _ =>
                NewDemand(
                    country,
                    powerBase,
                    PowerBaseDemandType.LowerTaxes,
                    Math.Max(0.02m, country.TaxRate - 0.02m))
        };
    }

    private static PowerBaseDemand NewDemand(
        Countries.Country country,
        PowerBaseType powerBase,
        PowerBaseDemandType type,
        decimal targetValue = 0m)
    {
        return new PowerBaseDemand
        {
            Country = country,
            PowerBase = powerBase,
            Type = type,
            TargetValue = targetValue
        };
    }

    private static bool IsSatisfied(GameState state, PowerBaseDemand demand)
    {
        var country = demand.Country;

        return demand.Type switch
        {
            PowerBaseDemandType.LowerTaxes =>
                country.TaxRate <= demand.TargetValue,

            PowerBaseDemandType.RaiseArmyFunding =>
                country.ArmyFunding >= demand.TargetValue,

            PowerBaseDemandType.RaiseAdministrationFunding =>
                country.AdministrationFunding >= demand.TargetValue,

            PowerBaseDemandType.RaiseCourtFunding =>
                country.CourtFunding >= demand.TargetValue,

            PowerBaseDemandType.EndWar =>
                !state.Wars.Any(war =>
                    war.Status == WarStatus.Active &&
                    war.IsParticipant(country)),

            _ => false
        };
    }

    private static Character? FindBestRival(
        GameState state,
        Countries.Country country,
        PowerBaseType powerBase)
    {
        var blocLeader = state.PoliticalBlocs
            .Where(bloc =>
                bloc.IsActive &&
                ReferenceEquals(bloc.Country, country) &&
                bloc.PowerBases.Contains(powerBase) &&
                bloc.Leader.IsPoliticallyActive)
            .OrderByDescending(bloc => bloc.Cohesion)
            .Select(bloc => bloc.Leader)
            .FirstOrDefault();

        if (blocLeader is not null)
            return blocLeader;

        return country.PoliticalFigures
            .Where(character =>
                character.IsPoliticallyActive &&
                !ReferenceEquals(character, country.Ruler))
            .OrderByDescending(character =>
                character.GetPowerBaseStanding(powerBase))
            .ThenByDescending(character => character.Ambition)
            .FirstOrDefault();
    }

    public static string DescribeDemandRequest(
        PowerBaseDemand demand)
    {
        return demand.Type switch
        {
            PowerBaseDemandType.LowerTaxes =>
                $"reduce the tax rate to {demand.TargetValue:P0} or lower.",

            PowerBaseDemandType.RaiseArmyFunding =>
                $"raise army funding to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.RaiseAdministrationFunding =>
                $"raise administration funding to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.RaiseCourtFunding =>
                $"raise court and patronage funding to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.EndWar =>
                "end the government's current war.",

            _ => "make a political concession."
        };
    }

    public static string DescribeDemand(
        PowerBaseDemand demand,
        bool initial)
    {
        var prefix = initial
            ? $"The {FormatPowerBase(demand.PowerBase).ToLowerInvariant()} are dissatisfied with the government and "
            : $"The {FormatPowerBase(demand.PowerBase).ToLowerInvariant()} demand remains unresolved: ";

        var request = demand.Type switch
        {
            PowerBaseDemandType.LowerTaxes =>
                $"want the tax rate reduced to {demand.TargetValue:P0} or lower.",

            PowerBaseDemandType.RaiseArmyFunding =>
                $"want army funding raised to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.RaiseAdministrationFunding =>
                $"want administration funding raised to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.RaiseCourtFunding =>
                $"want court and patronage funding raised to at least {demand.TargetValue:P0}.",

            PowerBaseDemandType.EndWar =>
                "want the government to end its current war.",

            _ => "want a political concession."
        };

        return prefix + request;
    }

    public static string FormatPowerBase(PowerBaseType powerBase)
    {
        return powerBase switch
        {
            PowerBaseType.RegionalElites => "Regional elites",
            PowerBaseType.RoyalFamily => "Royal family",
            _ => powerBase.ToString()
        };
    }

    private static int MonthsBetween(
        GameDate earlier,
        GameDate later)
    {
        return Math.Max(
            0,
            (later.Year - earlier.Year) * 12 +
            later.Month -
            earlier.Month);
    }

    private static string FormatPowerBaseList(IEnumerable<PowerBaseType> powerBases)
    {
        var names = powerBases
            .Select(FormatPowerBase)
            .OrderBy(name => name)
            .ToList();

        if (names.Count == 0)
            return "no major groups";

        if (names.Count == 1)
            return names[0];

        return string.Join(", ", names.Take(names.Count - 1)) +
               " and " + names[^1];
    }
}
