namespace LeaderGame.Simulation.Information;

public enum InformationTopic
{
    Economy,
    Military,
    ForeignAffairs,
    DomesticPolitics
}

public enum InformationMetric
{
    Population,
    Gdp,
    Treasury,
    Debt,
    MonthlyTaxRevenue,
    MonthlyTradeIncome,
    MonthlyExpenses,
    MonthlyBalance,
    AdministrativeEfficiency,
    ArmySize,
    ArmyReadiness,
    WarExhaustion,
    WarScore,
    PublicUnrest,
    GovernmentStability,
    PoliticalBacking,
    DiplomaticRelations,
    DiplomaticTrust,
    DiplomaticTension
}

public readonly record struct InformationKey(
    InformationMetric Metric,
    string SubjectCountryId,
    string? RelatedCountryId = null);
