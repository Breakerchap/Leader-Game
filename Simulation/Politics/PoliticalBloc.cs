using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Politics;

/// <summary>
/// An organised domestic opposition coalition. A bloc is not itself a coup:
/// it represents groups and political figures coordinating around an alternative
/// leader inside the normal political system.
/// </summary>
public sealed class PoliticalBloc
{
    public Guid Id { get; } = Guid.NewGuid();

    public required Country Country { get; init; }

    public required Character Leader { get; init; }

    public HashSet<PowerBaseType> PowerBases { get; } = [];

    public HashSet<int> MemberIds { get; } = [];

    public int MonthsActive { get; set; }

    public double Cohesion { get; set; }

    public bool IsActive { get; set; } = true;
}
