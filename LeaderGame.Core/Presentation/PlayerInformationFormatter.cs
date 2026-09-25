using LeaderGame.Simulation;
using LeaderGame.Simulation.Information;

namespace LeaderGame.Presentation;

internal static class PlayerInformationFormatter
{
    public static MetricCardView Metric(
        GameState state,
        InformationMetric metric,
        string subjectCountryId,
        string label,
        string? relatedCountryId = null)
    {
        var known = state.Knowledge.Get(
            metric,
            subjectCountryId,
            relatedCountryId);

        if (known is null)
        {
            return new MetricCardView(
                label,
                "Unknown",
                "No usable report has reached the ruler.",
                true);
        }

        var age = known.AgeInMonths(state.Date);
        var ageText = Age(age);
        var source = known.SourceAdvisorName;
        var stale = age >= 6;

        return new MetricCardView(
            label,
            $"~{Value(metric, known.Estimate)}",
            $"±{Margin(metric, known.Margin)} · {source} · {ageText} · apparent confidence {known.ReportedConfidence}%",
            stale);
    }

    public static string Value(InformationMetric metric, double value)
    {
        return metric switch
        {
            InformationMetric.Population or
            InformationMetric.Gdp or
            InformationMetric.Treasury or
            InformationMetric.Debt or
            InformationMetric.MonthlyTaxRevenue or
            InformationMetric.MonthlyTradeIncome or
            InformationMetric.MonthlyExpenses or
            InformationMetric.ArmySize =>
                $"{value:N0}",

            InformationMetric.MonthlyBalance =>
                $"{value:+#,##0;-#,##0;0}",

            InformationMetric.AdministrativeEfficiency or
            InformationMetric.ArmyReadiness or
            InformationMetric.WarExhaustion or
            InformationMetric.PublicUnrest or
            InformationMetric.GovernmentStability or
            InformationMetric.PoliticalBacking =>
                $"{value:F0}/100",

            InformationMetric.DiplomaticRelations or
            InformationMetric.WarScore =>
                $"{value:+0;-0;0}",

            InformationMetric.DiplomaticTrust or
            InformationMetric.DiplomaticTension =>
                $"{value:F0}/100",

            _ => $"{value:F0}"
        };
    }

    public static string Margin(InformationMetric metric, double margin)
    {
        margin = Math.Abs(margin);

        return metric switch
        {
            InformationMetric.Population or
            InformationMetric.Gdp or
            InformationMetric.Treasury or
            InformationMetric.Debt or
            InformationMetric.MonthlyTaxRevenue or
            InformationMetric.MonthlyTradeIncome or
            InformationMetric.MonthlyExpenses or
            InformationMetric.MonthlyBalance or
            InformationMetric.ArmySize =>
                $"{margin:N0}",

            _ => $"{margin:F0}"
        };
    }

    public static string MetricName(InformationMetric metric)
    {
        return metric switch
        {
            InformationMetric.Gdp => "GDP",
            InformationMetric.MonthlyTaxRevenue => "Monthly tax revenue",
            InformationMetric.MonthlyTradeIncome => "Monthly trade income",
            InformationMetric.MonthlyExpenses => "Monthly expenses",
            InformationMetric.MonthlyBalance => "Monthly balance",
            InformationMetric.AdministrativeEfficiency => "Administrative efficiency",
            InformationMetric.ArmySize => "Army strength",
            InformationMetric.ArmyReadiness => "Army readiness",
            InformationMetric.WarExhaustion => "War exhaustion",
            InformationMetric.WarScore => "Campaign position",
            InformationMetric.PublicUnrest => "Public unrest",
            InformationMetric.GovernmentStability => "Government stability",
            InformationMetric.PoliticalBacking => "Political backing",
            InformationMetric.DiplomaticRelations => "Relations",
            InformationMetric.DiplomaticTrust => "Diplomatic trust",
            InformationMetric.DiplomaticTension => "Diplomatic tension",
            _ => metric.ToString()
        };
    }

    public static string Age(int months) => months switch
    {
        <= 0 => "current-ish",
        1 => "1 month old",
        _ => $"{months} months old"
    };

    public static string Level(double value) => value switch
    {
        >= 85 => "Exceptional",
        >= 70 => "Strong",
        >= 55 => "Solid",
        >= 40 => "Mixed",
        >= 25 => "Weak",
        _ => "Very weak"
    };

    public static string Trust(int trust) => trust switch
    {
        >= 80 => "Very high",
        >= 65 => "High",
        >= 45 => "Mixed",
        >= 25 => "Low",
        _ => "Very low"
    };

    public static string Willingness(double willingness) => willingness switch
    {
        >= 80 => "Very likely",
        >= 65 => "Likely",
        >= 45 => "Uncertain",
        >= 25 => "Reluctant",
        _ => "Hostile"
    };

    public static string Threat(double threat) => threat switch
    {
        >= 80 => "Severe",
        >= 60 => "High",
        >= 40 => "Moderate",
        >= 20 => "Some",
        _ => "Low"
    };

    public static string Health(int health) => health switch
    {
        >= 90 => "good",
        >= 70 => "fair",
        >= 45 => "poor",
        >= 20 => "very poor",
        _ => "critical"
    };
}
