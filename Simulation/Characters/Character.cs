namespace LeaderGame.Simulation.Characters;

public abstract class Character
{
    public required int Id { get; init; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public int Age { get; set; }

    public int Competence { get; set; }
    public int Ambition { get; set; }

    public bool IsAlive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}
