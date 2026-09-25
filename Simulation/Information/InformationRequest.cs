using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Information;

public enum InformationRequestStatus
{
    Pending,
    Refused,
    Completed,
    Cancelled
}

public sealed class InformationRequest
{
    public Guid Id { get; } = Guid.NewGuid();

    public required InformationTopic Topic { get; init; }

    public required Character Advisor { get; init; }

    public required Country SubjectCountry { get; init; }

    public Country? RelatedCountry { get; init; }

    public required GameDate RequestedOn { get; init; }

    public int RemainingMonths { get; set; }

    public double WillingnessAtRequest { get; init; }

    public InformationRequestStatus Status { get; set; } =
        InformationRequestStatus.Pending;
}
