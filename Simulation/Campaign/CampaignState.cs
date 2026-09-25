namespace LeaderGame.Simulation.Campaign;

public enum CampaignObjectiveType
{
    StableGovernment,
    SolventTreasury,
    TradeNetwork,
    DiplomaticStanding,
    PeacefulRealm,
    ElectoralMandate
}

public sealed class CampaignObjective
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required CampaignObjectiveType Type { get; init; }

    public double TargetValue { get; init; }

    public double SecondaryTargetValue { get; init; }

    public string? RelatedCountryId { get; init; }

    public int RequiredMonths { get; init; } = 1;

    public int ProgressMonths { get; set; }

    public bool IsCompleted { get; set; }

    public GameDate? CompletedOn { get; set; }
}

public sealed class CampaignState
{
    public required string ScenarioId { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required GameDate StartedOn { get; init; }

    public List<CampaignObjective> Objectives { get; } = [];
}
