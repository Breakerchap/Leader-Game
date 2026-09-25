using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Information;

namespace LeaderGame.Simulation.Orders;

public sealed class RequestReportOrder : Order
{
    public required Country Country { get; init; }

    public required InformationTopic Topic { get; init; }

    public required Country SubjectCountry { get; init; }

    public Country? RelatedCountry { get; init; }
}
