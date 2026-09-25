namespace LeaderGame.Simulation.Characters;

public sealed class RelationshipGraph
{
    private readonly Dictionary<(int FromId, int ToId), Relationship> _relationships = [];

    public IEnumerable<Relationship> All => _relationships.Values;

    public IEnumerable<(int FromId, int ToId, Relationship Relationship)> Entries =>
        _relationships.Select(entry => (
            entry.Key.FromId,
            entry.Key.ToId,
            entry.Value));

    public Relationship GetOrCreate(Character from, Character to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        if (ReferenceEquals(from, to) || from.Id == to.Id)
            throw new ArgumentException("A character cannot have a relationship with themselves.");

        var key = (from.Id, to.Id);

        if (!_relationships.TryGetValue(key, out var relationship))
        {
            relationship = new Relationship();
            _relationships[key] = relationship;
        }

        return relationship;
    }

    public void Set(
        Character from,
        Character to,
        int opinion,
        int trust,
        int fear)
    {
        var relationship = GetOrCreate(from, to);
        relationship.Opinion = opinion;
        relationship.Trust = trust;
        relationship.Fear = fear;
    }
}
