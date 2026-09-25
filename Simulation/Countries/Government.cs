namespace LeaderGame.Simulation.Countries;

public class Government
{
    private double _stability = 50;

    public GovernmentType Type { get; set; }

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
