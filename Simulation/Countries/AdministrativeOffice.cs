using LeaderGame.Simulation.Characters;

namespace LeaderGame.Simulation.Countries;

public enum AdministrativeFunction
{
    Chancery,
    Revenue,
    LocalGovernment,
    MilitaryLogistics,
    ForeignAffairs
}

/// <summary>
/// A historically broad administrative organ rather than a modern ministry.
/// Depending on the regime this may be a chancery, fiscal chamber, household
/// office, board, or network of local office-holders.
/// </summary>
public sealed class AdministrativeOffice
{
    private int _capacity;
    private int _reach;
    private int _integrity;
    private int _workload;
    private int _patronageDependence;

    public AdministrativeFunction Function { get; init; }

    public required string Name { get; init; }

    public Position? ResponsiblePosition { get; init; }

    /// <summary>
    /// Staff, routines, records and institutional competence.
    /// </summary>
    public int Capacity
    {
        get => _capacity;
        set => _capacity = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// How far central decisions can reliably reach beyond the capital/court.
    /// </summary>
    public int Reach
    {
        get => _reach;
        set => _reach = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Resistance to leakage, embezzlement and private appropriation.
    /// </summary>
    public int Integrity
    {
        get => _integrity;
        set => _integrity = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Current pressure on the office from petitions, war, collection,
    /// correspondence and other business.
    /// </summary>
    public int Workload
    {
        get => _workload;
        set => _workload = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Dependence on personal clients, venal office-holding and local notables.
    /// High values make an office politically embedded but harder to professionalise.
    /// </summary>
    public int PatronageDependence
    {
        get => _patronageDependence;
        set => _patronageDependence = Math.Clamp(value, 0, 100);
    }
}
