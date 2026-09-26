using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class CourtActionSystem
{
    public static SimulationReport Process(
        GameState state,
        CourtActionOrder order)
    {
        var country = order.Country;
        var subject = order.Subject;

        if (!ReferenceEquals(
                order.Issuer,
                country.Ruler))
        {
            return Reject(
                state,
                order,
                "Only the current ruler can use personal court authority this way.");
        }

        if (!subject.IsPoliticallyActive ||
            ReferenceEquals(
                subject,
                country.Ruler) ||
            !country.PoliticalFigures.Contains(
                subject))
        {
            return Reject(
                state,
                order,
                "That person is not currently available for a direct court intervention.");
        }

        var relationship =
            state.Relationships.GetOrCreate(
                subject,
                country.Ruler);

        var details =
            order.ActionType switch
            {
                CourtActionType.PrivateAudience =>
                    PrivateAudience(
                        state,
                        country,
                        subject,
                        relationship),

                CourtActionType.GrantPatronage =>
                    GrantPatronage(
                        state,
                        country,
                        subject,
                        relationship),

                CourtActionType.PublicRebuke =>
                    PublicRebuke(
                        state,
                        country,
                        subject,
                        relationship),

                _ => "No personal political action was carried out."
            };

        order.Status =
            OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Personal,
            $"{ActionLabel(order.ActionType)}: {subject.FullName}",
            details);
    }

    public static string ActionLabel(
        CourtActionType action) =>
        action switch
        {
            CourtActionType.PrivateAudience =>
                "Private audience",
            CourtActionType.GrantPatronage =>
                "Grant patronage",
            CourtActionType.PublicRebuke =>
                "Public rebuke",
            _ => action.ToString()
        };

    private static string PrivateAudience(
        GameState state,
        Country country,
        Character subject,
        Relationship relationship)
    {
        var oldThreat =
            PoliticalCalculations.GetThreatScore(
                state,
                country,
                subject);

        var warmth =
            subject.Ambition >= 80
                ? 4
                : 7;

        relationship.ChangeOpinion(
            warmth);
        relationship.ChangeTrust(5);
        relationship.ChangeFear(-4);

        subject.Influence =
            Math.Min(
                100,
                subject.Influence + 2);

        var newThreat =
            PoliticalCalculations.GetThreatScore(
                state,
                country,
                subject);

        return
            $"The ruler gives {subject.FullName} unusual private access. The personal relationship improves and trust grows, but visible access to the ruler also increases {subject.FullName}'s political importance." +
            (newThreat > oldThreat
                ? " The audience therefore makes the figure slightly harder to ignore in future court politics."
                : string.Empty);
    }

    private static string GrantPatronage(
        GameState state,
        Country country,
        Character subject,
        Relationship relationship)
    {
        var cost =
            Math.Max(
                4_000m,
                country.Gdp *
                0.00020m);

        Pay(
            country,
            cost);

        relationship.ChangeOpinion(14);
        relationship.ChangeTrust(9);
        relationship.ChangeFear(-3);

        subject.Influence =
            Math.Min(
                100,
                subject.Influence + 5);

        var strongestBase =
            Enum.GetValues<PowerBaseType>()
                .OrderByDescending(type =>
                    subject.GetPowerBaseStanding(
                        type))
                .First();

        subject.ChangePowerBaseStanding(
            strongestBase,
            4);

        foreach (var rival in country.PoliticalFigures.Where(character =>
                     character.IsPoliticallyActive &&
                     !ReferenceEquals(
                         character,
                         subject) &&
                     !ReferenceEquals(
                         character,
                         country.Ruler) &&
                     character.Ambition >= 75))
        {
            var rivalRelationship =
                state.Relationships.GetOrCreate(
                    rival,
                    country.Ruler);

            if (rival.GetPowerBaseStanding(
                    strongestBase) >= 65)
            {
                rivalRelationship.ChangeOpinion(-2);
            }
        }

        return
            $"About {cost:N0} in offices, gifts, access and favour is directed toward {subject.FullName}. Loyalty improves substantially, but the recipient gains influence and stronger standing with {FormatPowerBase(strongestBase)}. Ambitious rivals may resent the preference.";
    }

    private static string PublicRebuke(
        GameState state,
        Country country,
        Character subject,
        Relationship relationship)
    {
        relationship.ChangeFear(18);
        relationship.ChangeOpinion(-12);
        relationship.ChangeTrust(-9);

        subject.Influence =
            Math.Max(
                0,
                subject.Influence - 5);

        var strongestBase =
            Enum.GetValues<PowerBaseType>()
                .OrderByDescending(type =>
                    subject.GetPowerBaseStanding(
                        type))
                .First();

        var backing =
            subject.GetPowerBaseStanding(
                strongestBase);

        if (backing >= 70)
        {
            country.Ruler.ChangePowerBaseStanding(
                strongestBase,
                -3);
        }

        var plot =
            state.Plots
                .Where(candidate =>
                    !candidate.IsResolved &&
                    ReferenceEquals(
                        candidate.Country,
                        country) &&
                    ReferenceEquals(
                        candidate.Instigator,
                        subject))
                .OrderByDescending(candidate =>
                    candidate.Progress)
                .FirstOrDefault();

        if (plot is not null &&
            plot.DiscoveryStage > 0)
        {
            plot.Progress =
                Math.Max(
                    0,
                    plot.Progress - 8);
        }

        return
            $"The ruler humiliates {subject.FullName} publicly. The figure loses immediate influence and becomes more fearful of open defiance, but trust and personal loyalty deteriorate." +
            (backing >= 70
                ? $" Supporters among the {FormatPowerBase(strongestBase)} also resent the treatment."
                : string.Empty) +
            (plot is not null &&
             plot.DiscoveryStage > 0
                ? " Known conspiratorial activity is disrupted for now."
                : string.Empty);
    }

    private static void Pay(
        Country country,
        decimal amount)
    {
        if (country.Treasury >= amount)
        {
            country.Treasury -=
                amount;
            return;
        }

        country.Debt +=
            amount -
            country.Treasury;
        country.Treasury = 0;
    }

    private static SimulationReport Reject(
        GameState state,
        CourtActionOrder order,
        string details)
    {
        order.Status =
            OrderStatus.Rejected;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            "Court action rejected",
            details);
    }

    private static string FormatPowerBase(
        PowerBaseType powerBase) =>
        powerBase switch
        {
            PowerBaseType.RoyalFamily =>
                "royal family",
            PowerBaseType.RegionalElites =>
                "regional elites",
            _ =>
                powerBase.ToString().ToLowerInvariant()
        };
}
