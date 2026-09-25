namespace LeaderGame.Simulation.Countries;

public class Government
{
    public GovernmentType Type { get; set; }
    public double Stability { get; set; } = 50;
}

public enum GovernmentType
{
    FeudalMonarchy,
    AbsoluteMonarchy,
    Republic
}