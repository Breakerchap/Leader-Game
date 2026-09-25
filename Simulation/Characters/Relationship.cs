namespace LeaderGame.Simulation.Characters;

/// <summary>
/// A directional relationship: how one character feels about another.
/// A can trust B while B distrusts A.
/// </summary>
public sealed class Relationship
{
    private int _opinion;
    private int _trust = 50;
    private int _fear;

    /// <summary>
    /// Affection or hostility, from -100 (hatred) to +100 (devotion).
    /// </summary>
    public int Opinion
    {
        get => _opinion;
        set => _opinion = Math.Clamp(value, -100, 100);
    }

    /// <summary>
    /// Confidence that the other person will behave predictably and honour commitments.
    /// </summary>
    public int Trust
    {
        get => _trust;
        set => _trust = Math.Clamp(value, 0, 100);
    }

    /// <summary>
    /// Fear of the other person's ability or willingness to punish disobedience.
    /// Fear may create compliance without friendship.
    /// </summary>
    public int Fear
    {
        get => _fear;
        set => _fear = Math.Clamp(value, 0, 100);
    }

    public void ChangeOpinion(int delta) => Opinion += delta;

    public void ChangeTrust(int delta) => Trust += delta;

    public void ChangeFear(int delta) => Fear += delta;
}
