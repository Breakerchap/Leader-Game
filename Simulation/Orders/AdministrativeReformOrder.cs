using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Orders;

public enum AdministrativeReformType
{
    AuditAccounts,
    StrengthenClerks,
    ExtendCommissions,
    DelegateToNotables
}

/// <summary>
/// A deliberate attempt to change how one administrative organ works.
/// The names describe late-medieval/early-modern practices rather than
/// a modern civil-service reform programme.
/// </summary>
public sealed class AdministrativeReformOrder : Order
{
    public required Country Country { get; init; }

    public AdministrativeFunction Function { get; init; }

    public AdministrativeReformType ReformType { get; init; }
}
