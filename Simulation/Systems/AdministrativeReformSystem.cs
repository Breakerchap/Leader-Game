using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class AdministrativeReformSystem
{
    public static SimulationReport Process(
        GameState state,
        AdministrativeReformOrder order)
    {
        var country = order.Country;

        if (!ReferenceEquals(order.Issuer, country.Ruler) ||
            !country.ContainsPoliticalFigure(order.Issuer))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Administrative initiative rejected",
                "Only the ruler may direct a structural administrative initiative.");
        }

        var office = country.GetAdministrativeOffice(order.Function);

        if (office is null)
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Administrative initiative rejected",
                "The selected administrative organ does not exist in this state.");
        }

        var expectedRecipient = office.ResponsiblePosition is { } position
            ? country.GetOfficeHolder(position)
            : country.Ruler;

        if (expectedRecipient is not null &&
            !ReferenceEquals(order.Recipient, expectedRecipient))
        {
            order.Status = OrderStatus.Rejected;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                "Administrative initiative rejected",
                $"{office.Name} is not currently directed through {order.Recipient.FullName}.");
        }

        var administrator = expectedRecipient ?? country.Ruler;

        var willingness = ReferenceEquals(administrator, country.Ruler)
            ? 80.0
            : PoliticalCalculations.GetOrderWillingness(
                state,
                country,
                administrator,
                country.Ruler);

        if (willingness < 20)
        {
            order.Status = OrderStatus.Refused;
            return new SimulationReport(
                state.Date,
                ReportCategory.Order,
                $"{administrator.FullName} resists administrative changes",
                $"{administrator.FullName} refuses to carry the proposed changes through {office.Name}.");
        }

        var execution =
            Math.Clamp(
                0.55 +
                administrator.Competence / 250.0 +
                willingness / 500.0,
                0.65,
                1.10);

        var report = order.ReformType switch
        {
            AdministrativeReformType.AuditAccounts =>
                AuditAccounts(country, office, administrator, execution, state),

            AdministrativeReformType.StrengthenClerks =>
                StrengthenClerks(country, office, administrator, execution, state),

            AdministrativeReformType.ExtendCommissions =>
                ExtendCommissions(country, office, administrator, execution, state),

            AdministrativeReformType.DelegateToNotables =>
                DelegateToNotables(country, office, administrator, execution, state),

            _ => RejectUnknown(state, order)
        };

        if (order.Status == OrderStatus.Pending)
            order.Status = OrderStatus.Completed;

        return report;
    }

    public static string Describe(AdministrativeReformType type) => type switch
    {
        AdministrativeReformType.AuditAccounts =>
            "Audit accounts",
        AdministrativeReformType.StrengthenClerks =>
            "Strengthen clerks",
        AdministrativeReformType.ExtendCommissions =>
            "Extend commissions",
        AdministrativeReformType.DelegateToNotables =>
            "Delegate to local notables",
        _ => type.ToString()
    };

    private static SimulationReport AuditAccounts(
        Country country,
        AdministrativeOffice office,
        Character administrator,
        double execution,
        GameState state)
    {
        var integrityGain = Scale(4, execution);
        var patronageReduction = Scale(2, execution);

        office.Integrity += integrityGain;
        office.PatronageDependence -= patronageReduction;
        office.Workload += 10;

        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Bureaucracy,
            1);
        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.RegionalElites,
            office.PatronageDependence >= 60 ? -2 : -1);

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{office.Name} undergoes an audit",
            $"{administrator.FullName} orders accounts, arrears and office records reviewed. " +
            "The exercise tightens control over leakage, but burdens the office and irritates some interests accustomed to looser supervision.");
    }

    private static SimulationReport StrengthenClerks(
        Country country,
        AdministrativeOffice office,
        Character administrator,
        double execution,
        GameState state)
    {
        office.Capacity += Scale(4, execution);
        office.Integrity += Scale(2, execution);
        office.PatronageDependence -= Scale(2, execution);
        office.Workload += 6;

        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Bureaucracy,
            2);
        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Aristocracy,
            -1);

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{office.Name} strengthens its clerical machinery",
            $"{administrator.FullName} regularises records, expands trained clerical work and gives routine business firmer procedures. " +
            "Capacity improves, though the change slightly shifts influence away from purely personal and household channels.");
    }

    private static SimulationReport ExtendCommissions(
        Country country,
        AdministrativeOffice office,
        Character administrator,
        double execution,
        GameState state)
    {
        office.Reach += Scale(5, execution);
        office.Capacity += Scale(1, execution);
        office.Workload += 12;

        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Bureaucracy,
            1);
        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.RegionalElites,
            -2);
        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Aristocracy,
            -1);

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{office.Name} extends central commissions",
            $"{administrator.FullName} sends more officers, commissions and written instructions into the localities. " +
            "The centre's practical reach grows, but so does the office's workload and friction with established local interests.");
    }

    private static SimulationReport DelegateToNotables(
        Country country,
        AdministrativeOffice office,
        Character administrator,
        double execution,
        GameState state)
    {
        office.Reach += Scale(3, execution);
        office.Workload -= Scale(12, execution);
        office.PatronageDependence += Scale(5, execution);
        office.Integrity -= Scale(1, execution);

        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.RegionalElites,
            2);
        country.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Aristocracy,
            1);
        country.Government.Stability += 0.2;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            $"{office.Name} delegates more business locally",
            $"{administrator.FullName} relies more heavily on established local office-holders and patrons. " +
            "Business travels further with less pressure on the centre, but the government becomes more dependent on those intermediaries.");
    }

    private static SimulationReport RejectUnknown(
        GameState state,
        AdministrativeReformOrder order)
    {
        order.Status = OrderStatus.Rejected;

        return new SimulationReport(
            state.Date,
            ReportCategory.Order,
            "Administrative initiative rejected",
            "The selected administrative strategy is not recognised.");
    }

    private static int Scale(int amount, double factor) =>
        Math.Max(
            1,
            (int)Math.Round(
                amount * factor,
                MidpointRounding.AwayFromZero));
}
