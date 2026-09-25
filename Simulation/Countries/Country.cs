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
    private double _publicUnrest = 20;

    public required string Id { get; init; }
    public required string Name { get; set; }

    public long Population { get; set; }

    /// <summary>
    /// Annual economic output in the scenario's abstract currency.
    /// </summary>
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

    /// <summary>
    /// Budget multipliers. 1.0 is normal funding; 0.5 is severe austerity;
    /// 1.5 is heavy overfunding.
    /// </summary>
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

    public double PublicUnrest
    {
        get => _publicUnrest;
        set => _publicUnrest = Math.Clamp(value, 0, 100);
    }

    public decimal LastMonthlyTaxRevenue { get; internal set; }

    public decimal LastMonthlyExpenses { get; internal set; }

    public decimal LastMonthlyDebtInterest { get; internal set; }

    public decimal LastMonthlyBalance { get; internal set; }

    public required Government Government { get; set; }

    public required Character Ruler { get; set; }

    public List<Character> SuccessionOrder { get; } = [];

    public List<Character> PoliticalFigures { get; } = [];

    public IEnumerable<Character> ActiveAdvisors =>
        PoliticalFigures.Where(character =>
            character.IsAlive &&
            !ReferenceEquals(character, Ruler) &&
            character.Position.HasValue);

    public IEnumerable<Character> AvailableAdvisors =>
        PoliticalFigures.Where(character =>
            character.IsAlive &&
            !ReferenceEquals(character, Ruler) &&
            !character.Position.HasValue);

    public Character? GetOfficeHolder(Position position)
    {
        return PoliticalFigures.FirstOrDefault(character =>
            character.IsAlive &&
            !ReferenceEquals(character, Ruler) &&
            character.Position == position);
    }

    public bool ContainsPoliticalFigure(Character character) =>
        PoliticalFigures.Contains(character);
}
