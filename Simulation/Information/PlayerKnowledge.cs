using LeaderGame.Simulation.Characters;

namespace LeaderGame.Simulation.Information;

public sealed class KnownInformation
{
    public required InformationKey Key { get; init; }

    public required double Estimate { get; init; }

    /// <summary>
    /// Adviser-reported uncertainty around the estimate. This is not guaranteed
    /// to contain the truth: incompetent or dishonest advisers can be confident
    /// and wrong.
    /// </summary>
    public double Margin { get; init; }

    public int ReportedConfidence { get; init; }

    public required GameDate AsOf { get; init; }

    public required GameDate ReceivedOn { get; init; }

    public required int SourceAdvisorId { get; init; }

    public required string SourceAdvisorName { get; init; }

    public bool WasRequested { get; init; }

    public int AgeInMonths(GameDate currentDate) =>
        Math.Max(
            0,
            (currentDate.Year - AsOf.Year) * 12 +
            currentDate.Month -
            AsOf.Month);
}

public sealed class PlayerKnowledge
{
    private readonly Dictionary<InformationKey, KnownInformation> _latest = new();

    public IReadOnlyCollection<KnownInformation> Latest => _latest.Values;

    public KnownInformation? Get(
        InformationMetric metric,
        string subjectCountryId,
        string? relatedCountryId = null)
    {
        _latest.TryGetValue(
            new InformationKey(metric, subjectCountryId, relatedCountryId),
            out var information);

        return information;
    }

    public void Update(KnownInformation information)
    {
        _latest[information.Key] = information;
    }
}
