namespace LeaderGame.Simulation.Countries;

public class Government
{
    private double _stability = 50;

    public GovernmentType Type { get; set; }

    public LegislativeBodyType LegislativeBody { get; set; }

    private int _legislativeIndependence;

    /// <summary>
    /// How capable the formal lawmaking body is of resisting the executive.
    /// Zero means effectively executive rule; 100 means a highly independent
    /// institution. This is structural power, not current political support.
    /// </summary>
    public int LegislativeIndependence
    {
        get => _legislativeIndependence;
        set => _legislativeIndependence = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Length of an elected term. Zero means this government does not hold
    /// scheduled elections.
    /// </summary>
    public int ElectionIntervalMonths { get; set; }

    /// <summary>
    /// Public constitutional calendar until the next scheduled election.
    /// </summary>
    public int MonthsUntilElection { get; set; }

    public int ElectionCampaignMonths { get; set; } = 6;

    public bool HoldsScheduledElections =>
        Type == GovernmentType.Republic &&
        ElectionIntervalMonths > 0;

    public double Stability
    {
        get => _stability;
        set => _stability = Math.Clamp(value, 0, 100);
    }
}

public enum GovernmentType
{
    FeudalMonarchy,
    AbsoluteMonarchy,
    Republic
}


public enum LegislativeBodyType
{
    None,
    RoyalCouncil,
    EstatesAssembly,
    GreatCouncil,
    Assembly
}
