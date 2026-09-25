using LeaderGame.Simulation.Characters;

namespace LeaderGame.Simulation.Countries;

public class Country
{
    private decimal _taxRate = 0.10m;
    private decimal _administrativeEfficiency = 0.75m;
    private double _publicUnrest = 20;

    public required string Id { get; init; }
    public required string Name { get; set; }

    public long Population { get; set; }

    /// <summary>
    /// Annual economic output in the scenario's abstract currency.
    /// </summary>
    public decimal Gdp { get; set; }

    public decimal Treasury { get; set; }

    public int ArmySize { get; set; }

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

    public double PublicUnrest
    {
        get => _publicUnrest;
        set => _publicUnrest = Math.Clamp(value, 0, 100);
    }

    public decimal LastMonthlyTaxRevenue { get; internal set; }

    public required Government Government { get; set; }

    public required Ruler Ruler { get; set; }

    public List<Advisor> Advisors { get; } = [];

    public Advisor? GetAdvisor(Position position)
    {
        return Advisors.FirstOrDefault(advisor => advisor.Position == position && advisor.IsAlive);
    }
}
