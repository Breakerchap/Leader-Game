namespace LeaderGame.Simulation.Characters;

public class Character
{
    private int _competence;
    private int _ambition;
    private int _legitimacy;
    private int _influence;
    private int _health = 100;
    private readonly Dictionary<string, int> _allegiances =
        new(StringComparer.Ordinal);

    public required int Id { get; init; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public int Age { get; set; }

    /// <summary>
    /// General physical health from 0 to 100. Health below 90 represents some
    /// degree of illness or frailty and changes mortality risk.
    /// </summary>
    public int Health
    {
        get => _health;
        set => _health = Math.Clamp(value, 0, 100);
    }

    public int Competence
    {
        get => _competence;
        set => _competence = Math.Clamp(value, 0, 100);
    }

    public int Ambition
    {
        get => _ambition;
        set => _ambition = Math.Clamp(value, 0, 100);
    }

    public int Legitimacy
    {
        get => _legitimacy;
        set => _legitimacy = Math.Clamp(value, 0, 100);
    }

    public int Influence
    {
        get => _influence;
        set => _influence = Math.Clamp(value, 0, 100);
    }

    public Position? Position { get; set; }

    public PoliticalStatus Status { get; set; } = PoliticalStatus.Active;

    public bool IsAlive { get; set; } = true;

    public bool IsPoliticallyActive =>
        IsAlive && Status == PoliticalStatus.Active;

    public bool IsInOffice =>
        IsPoliticallyActive && Position.HasValue;

    public string FullName => $"{FirstName} {LastName}";

    public IReadOnlyDictionary<string, int> Allegiances => _allegiances;

    public int GetAllegiance(string key, int defaultValue = 50)
    {
        return _allegiances.TryGetValue(key, out var value)
            ? value
            : Math.Clamp(defaultValue, 0, 100);
    }

    public void SetAllegiance(string key, int value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _allegiances[key] = Math.Clamp(value, 0, 100);
    }

    public void ChangeAllegiance(string key, int delta)
    {
        SetAllegiance(key, GetAllegiance(key) + delta);
    }
}
