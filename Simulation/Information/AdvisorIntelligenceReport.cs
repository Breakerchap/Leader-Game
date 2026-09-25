using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Information;

public sealed class AdvisorIntelligenceReport
{
    public Guid Id { get; } = Guid.NewGuid();

    public required InformationTopic Topic { get; init; }

    public required Character Advisor { get; init; }

    public required Country SubjectCountry { get; init; }

    public Country? RelatedCountry { get; init; }

    public required GameDate ProducedOn { get; init; }

    public required GameDate DataAsOf { get; init; }

    public bool WasRequested { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public List<KnownInformation> Facts { get; } = [];
}
