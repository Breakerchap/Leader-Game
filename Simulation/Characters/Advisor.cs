namespace LeaderGame.Simulation.Characters;

public class Advisor : Character
{
    public int Loyalty { get; set; }

    /// <summary>
    /// The office currently held. Null means the character is available for appointment.
    /// </summary>
    public Position? Position { get; set; }

    public bool IsInOffice => Position.HasValue;
}
