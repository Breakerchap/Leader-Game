using LeaderGame.Simulation.Characters;

namespace LeaderGame.Simulation.Countries;

public class Country
{
    private decimal _taxRate = 0.10m;
    private decimal _administrativeEfficiency = 0.75m;
    private decimal _debt;
    private decimal _armyFunding = 1m;
    private decimal _administrationFunding = 1m;
    private decimal _courtFunding = 1m;
    private double _armyReadiness = 75;
    private double _warExhaustion;
    private double _publicUnrest = 20;

    public required string Id { get; init; }
    public required string Name { get; set; }

    public long Population { get; set; }

    public decimal Gdp { get; set; }

    public decimal Treasury { get; set; }

    public decimal Debt
    {
        get => _debt;
        set => _debt = Math.Max(0m, value);
    }

    public int ArmySize { get; set; }

    public double ArmyReadiness
    {
        get => _armyReadiness;
        set => _armyReadiness = Math.Clamp(value, 0, 100);
    }

    public decimal TaxRate
    {
        get => _taxRate;
        set => _taxRate = Math.Clamp(value, 0m, 0.60m);
    }

    public decimal AdministrativeEfficiency
    {
        get => _administrativeEfficiency;
        set => _administrativeEfficiency = Math.Clamp(value, 0.25m, 1m);
    }

    public decimal ArmyFunding
    {
        get => _armyFunding;
        set => _armyFunding = Math.Clamp(value, 0.5m, 1.5m);
    }

    public decimal AdministrationFunding
    {
        get => _administrationFunding;
        set => _administrationFunding = Math.Clamp(value, 0.5m, 1.5m);
    }

    public decimal CourtFunding
    {
        get => _courtFunding;
        set => _courtFunding = Math.Clamp(value, 0.5m, 1.5m);
    }

    public double WarExhaustion
    {
        get => _warExhaustion;
        set => _warExhaustion = Math.Clamp(value, 0, 100);
    }

    public double PublicUnrest
    {
        get => _publicUnrest;
        set => _publicUnrest = Math.Clamp(value, 0, 100);
    }

    public decimal LastMonthlyTaxRevenue { get; internal set; }

    public decimal LastMonthlyTradeIncome { get; internal set; }

    public decimal LastMonthlyExpenses { get; internal set; }

    public decimal LastMonthlyDebtInterest { get; internal set; }

    public decimal LastMonthlyBalance { get; internal set; }

    public required Government Government { get; set; }

    public required Character Ruler { get; set; }

    /// <summary>
    /// Country IDs sharing a direct land or strategically immediate border.
    /// This is topology, not an alliance or relationship.
    /// </summary>
    public HashSet<string> NeighborIds { get; } = new(StringComparer.Ordinal);

    public List<Character> SuccessionOrder { get; } = [];

    public List<Character> PoliticalFigures { get; } = [];

    public IEnumerable<Character> ActiveAdvisors =>
        PoliticalFigures.Where(character =>
            character.IsPoliticallyActive &&
            !ReferenceEquals(character, Ruler) &&
            character.Position.HasValue);

    public IEnumerable<Character> AvailableAdvisors =>
        PoliticalFigures.Where(character =>
            character.IsPoliticallyActive &&
            !ReferenceEquals(character, Ruler) &&
            !character.Position.HasValue);

    public IEnumerable<Character> Prisoners =>
        PoliticalFigures.Where(character =>
            character.IsAlive &&
            character.Status == PoliticalStatus.Imprisoned);

    public Character? GetOfficeHolder(Position position)
    {
        return PoliticalFigures.FirstOrDefault(character =>
            character.IsPoliticallyActive &&
            !ReferenceEquals(character, Ruler) &&
            character.Position == position);
    }

    public bool ContainsPoliticalFigure(Character character) =>
        PoliticalFigures.Contains(character);

    public bool IsNeighbor(Country other) =>
        NeighborIds.Contains(other.Id);
}
