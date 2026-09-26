using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class RegionalSystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();

        foreach (var country in state.Countries)
        {
            if (country.Regions.Count == 0)
                continue;

            var localAdministration =
                AdministrativeSystem.GetPerformance(
                    country,
                    AdministrativeFunction.LocalGovernment);

            foreach (var region in country.Regions)
            {
                ApplyMonthlyDrift(country, region, localAdministration);

                if (region.Unrest >= 70)
                    country.Government.Stability -= 0.10;

                if (ReferenceEquals(country, state.Player.Country) &&
                    state.Date.Month % 3 == 0 &&
                    region.Unrest >= 75)
                {
                    reports.Add(new SimulationReport(
                        state.Date,
                        ReportCategory.Politics,
                        $"{region.Name} is becoming dangerous",
                        $"Reports from {region.Name} describe serious disorder. " +
                        "The government must choose whether to spend political capital, bargain locally or invest in the region."));
                }
            }

            UpdateNationalUnrest(country);
        }

        return reports;
    }

    public static SimulationReport Process(
        GameState state,
        RegionalActionOrder order)
    {
        if (!order.Country.Regions.Contains(order.Region))
            return Reject(state, order, "That region is no longer part of the country.");

        var chancellor =
            order.Country.GetOfficeHolder(
                Characters.Position.Chancellor);

        if (chancellor is null ||
            !ReferenceEquals(chancellor, order.Recipient))
        {
            return Reject(
                state,
                order,
                "A serving Chancellor must coordinate major regional policy.");
        }

        var willingness =
            PoliticalCalculations.GetOrderWillingness(
                state,
                order.Country,
                chancellor,
                order.Issuer);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                $"{chancellor.FullName} refuses the regional directive",
                $"{chancellor.FullName} will not commit the government to a major intervention in {order.Region.Name}.");
        }

        var implementation =
            Math.Clamp(
                (double)PoliticalCalculations.GetImplementationFactor(
                    chancellor,
                    willingness),
                0.45,
                1.10);

        var details = order.ActionType switch
        {
            RegionalActionType.AssertCentralAuthority =>
                StrengthenAdministration(order.Country, order.Region, implementation),
            RegionalActionType.BargainWithLocalElites =>
                NegotiateLocalCompact(order.Country, order.Region, implementation),
            RegionalActionType.InvestInRegion =>
                Invest(order.Country, order.Region, implementation),
            _ => "No regional action was taken."
        };

        order.Status = OrderStatus.Completed;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{order.Region.Name}: {ActionLabel(order.ActionType)}",
            details);
    }

    public static decimal GetTaxCollectionFactor(Region region)
    {
        var control =
            0.72m + (decimal)region.CrownControl / 360m;
        var privilegeReduction =
            1m - (decimal)region.Privileges / 650m;
        var unrestReduction =
            1m - (decimal)Math.Max(0, region.Unrest - 45) / 500m;

        return Math.Clamp(
            control * privilegeReduction * unrestReduction,
            0.45m,
            1.08m);
    }

    public static string ActionLabel(RegionalActionType action) =>
        action switch
        {
            RegionalActionType.AssertCentralAuthority => "Strengthen central administration",
            RegionalActionType.BargainWithLocalElites => "Negotiate local compact",
            RegionalActionType.InvestInRegion => "Invest in region",
            _ => action.ToString()
        };

    private static string StrengthenAdministration(
        Country country,
        Region region,
        double implementation)
    {
        region.CrownControl += 8 * implementation;
        region.LocalElitePower -= 4 * implementation;
        region.Privileges -= 4 * implementation;
        region.Unrest += 5 * implementation;

        var office =
            country.GetAdministrativeOffice(
                AdministrativeFunction.LocalGovernment);

        if (office is not null)
            office.Workload += 6;

        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.RegionalElites,
            -(int)Math.Round(3 * implementation));

        return
            $"The government extends its own officials and procedures in {region.Name}. " +
            "Direct control improves, but local interests become more resistant.";
    }

    private static string NegotiateLocalCompact(
        Country country,
        Region region,
        double implementation)
    {
        region.Privileges += 9 * implementation;
        region.LocalElitePower += 6 * implementation;
        region.Unrest -= 10 * implementation;
        region.CrownControl -= 4 * implementation;

        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.RegionalElites,
            (int)Math.Round(5 * implementation));

        return
            $"The government trades exemptions and local discretion for cooperation in {region.Name}. " +
            "Tension falls quickly, but the centre gives up some direct leverage.";
    }

    private static string Invest(
        Country country,
        Region region,
        double implementation)
    {
        var cost =
            country.Gdp *
            Math.Max(0.05m, region.EconomicShare) *
            0.00065m;

        if (country.Treasury >= cost)
        {
            country.Treasury -= cost;
        }
        else
        {
            country.Debt += cost - country.Treasury;
            country.Treasury = 0;
        }

        region.Prosperity += 8 * implementation;
        region.Unrest -= 5 * implementation;
        region.CrownControl += 2 * implementation;

        return
            $"The government spends roughly {cost:N0} on practical improvements in {region.Name}. " +
            "Prosperity and goodwill improve, but the treasury bears the cost.";
    }

    private static void ApplyMonthlyDrift(
        Country country,
        Region region,
        double localAdministration)
    {
        region.CrownControl +=
            (localAdministration - 50) / 280.0 -
            (region.LocalElitePower + region.Privileges - 100) / 700.0;

        var effectiveBurden =
            (double)country.TaxRate * 100 *
            (1 - region.Privileges / 180.0);

        if (effectiveBurden > 10)
            region.Unrest += (effectiveBurden - 10) * 0.025;

        if (localAdministration < 35)
            region.Unrest += 0.10;
        else if (localAdministration > 70)
            region.Unrest -= 0.06;

        if (region.Prosperity < 35)
            region.Unrest += 0.08;
        else if (region.Prosperity > 70)
            region.Unrest -= 0.05;
    }

    private static void UpdateNationalUnrest(Country country)
    {
        var weight = country.Regions.Sum(region =>
            Math.Max(0.01m, region.PopulationShare));

        var average = country.Regions.Sum(region =>
            region.Unrest *
            (double)Math.Max(0.01m, region.PopulationShare)) /
            (double)weight;

        country.PublicUnrest =
            country.PublicUnrest * 0.85 +
            average * 0.15;
    }

    private static SimulationReport Reject(
        GameState state,
        RegionalActionOrder order,
        string details)
    {
        order.Status = OrderStatus.Rejected;
        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            "Regional directive rejected",
            details);
    }
}
