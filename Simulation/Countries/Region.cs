namespace LeaderGame.Simulation.Countries;

/// <summary>
/// A politically meaningful part of a country rather than a map tile.
/// Regions exist to create leader-level trade-offs around control, local
/// privilege, prosperity and disorder.
/// </summary>
public sealed class Region
{
    private double _crownControl = 50;
    private double _localElitePower = 50;
    private double _unrest = 20;
    private double _privileges = 50;
    private double _prosperity = 50;

    public required string Id { get; init; }

    public required string Name { get; set; }

    /// <summary>
    /// Share of the country's underlying economic output represented here.
    /// Scenario data should sum to roughly 1.0.
    /// </summary>
    public decimal EconomicShare { get; set; }

    /// <summary>
    /// Share of national population represented here.
    /// Scenario data should sum to roughly 1.0.
    /// </summary>
    public decimal PopulationShare { get; set; }

    /// <summary>
    /// How directly the central government can make orders stick.
    /// </summary>
    public double CrownControl
    {
        get => _crownControl;
        set => _crownControl = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Political leverage held by local magnates, councils or patricians.
    /// </summary>
    public double LocalElitePower
    {
        get => _localElitePower;
        set => _localElitePower = Math.Clamp(value, 0, 100);
    }

    public double Unrest
    {
        get => _unrest;
        set => _unrest = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Exemptions, customary rights and delegated authority retained locally.
    /// High privilege makes bargaining easier but weakens direct extraction.
    /// </summary>
    public double Privileges
    {
        get => _privileges;
        set => _privileges = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Long-run local economic health. This is intentionally broad rather
    /// than a second GDP system.
    /// </summary>
    public double Prosperity
    {
        get => _prosperity;
        set => _prosperity = Math.Clamp(value, 0, 100);
    }
}
