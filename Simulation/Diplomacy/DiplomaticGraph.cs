using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Diplomacy;

public sealed class DiplomaticGraph
{
    private readonly Dictionary<(string First, string Second), DiplomaticRelation> _relations = [];

    public IEnumerable<DiplomaticRelation> All => _relations.Values;

    public DiplomaticRelation GetOrCreate(Country first, Country second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (ReferenceEquals(first, second) || first.Id == second.Id)
            throw new ArgumentException("A country cannot have a diplomatic relation with itself.");

        var key = CanonicalKey(first.Id, second.Id);

        if (!_relations.TryGetValue(key, out var relation))
        {
            relation = new DiplomaticRelation
            {
                CountryAId = key.First,
                CountryBId = key.Second
            };

            _relations[key] = relation;
        }

        return relation;
    }

    public void Set(
        Country first,
        Country second,
        int relations,
        int trust,
        int tension,
        bool tradeAgreement = false,
        GameDate? tradeAgreementStartedOn = null)
    {
        var relation = GetOrCreate(first, second);
        relation.Relations = relations;
        relation.Trust = trust;
        relation.Tension = tension;
        relation.HasTradeAgreement = tradeAgreement;
        relation.TradeAgreementStartedOn =
            tradeAgreement ? tradeAgreementStartedOn : null;
    }

    public IEnumerable<DiplomaticRelation> ForCountry(Country country) =>
        _relations.Values.Where(relation => relation.Involves(country));

    private static (string First, string Second) CanonicalKey(
        string first,
        string second)
    {
        return string.CompareOrdinal(first, second) <= 0
            ? (first, second)
            : (second, first);
    }
}
