using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Player;

public class PlayerState
{
    public required Character CurrentCharacter { get; set; }

    public required Country Country { get; set; }

    public bool HasLost { get; set; }
}
