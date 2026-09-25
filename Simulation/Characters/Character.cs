namespace LeaderGame.Simulation.Characters;

public class Character
{
    private int _competence;
    private int _ambition;
    private int _legitimacy;
    private int _influence;
    private readonly Dictionary<string, int> _allegiances =
        new(StringComparer.Ordinal);

    public required int Id { get; init; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public int Age { get; set; }

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

    /// <summary>
    /// Personal claim or perceived right to rule. It matters most while this
    /// character is ruler or a succession candidate, but remains attached to the person.
    /// </summary>
    public int Legitimacy
    {
        get => _legitimacy;
        set => _legitimacy = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Personal political weight: connections, clients, reputation and access to power.
    /// Office affects this over time, but influence remains attached to the person.
    /// </summary>
    public int Influence
    {
        get => _influence;
        set => _influence = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Advisory office currently held. Null means the character holds no advisory office.
    /// Rulership is tracked by Country.Ruler rather than as an advisory position.
    /// </summary>
    public Position? Position { get; set; }

    public bool IsAlive { get; set; } = true;

    public bool IsInOffice => Position.HasValue;

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
