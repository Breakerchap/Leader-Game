using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Military;

public enum WarStatus
{
    Active,
    AttackerVictory,
    DefenderVictory,
    WhitePeace
}

public enum WarStance
{
    Defensive,
    Balanced,
    Aggressive
}

public enum WarGoalType
{
    Reparations,
    SettleBorderDispute,
    HumiliateRival
}

public enum PeaceSettlementType
{
    WhitePeace,
    AttackerWarGoal,
    DefenderTerms
}

public sealed class War
{
    private double _warScore;

    public Guid Id { get; } = Guid.NewGuid();

    public required Country Attacker { get; init; }

    public required Country Defender { get; init; }

    public required GameDate StartedOn { get; init; }

    public WarGoalType AttackerGoal { get; init; } = WarGoalType.Reparations;

    public PeaceSettlementType? Settlement { get; internal set; }

    /// <summary>
    /// -100 means decisive defender advantage; +100 means decisive attacker advantage.
    /// </summary>
    public double WarScore
    {
        get => _warScore;
        set => _warScore = Math.Clamp(value, -100, 100);
    }

    public int MonthsActive { get; internal set; }

    public WarStance AttackerStance { get; set; } = WarStance.Balanced;

    public WarStance DefenderStance { get; set; } = WarStance.Balanced;

    public WarStatus Status { get; internal set; } = WarStatus.Active;

    public bool IsParticipant(Country country) =>
        ReferenceEquals(Attacker, country) ||
        ReferenceEquals(Defender, country);

    public Country OpponentOf(Country country)
    {
        if (ReferenceEquals(Attacker, country))
            return Defender;

        if (ReferenceEquals(Defender, country))
            return Attacker;

        throw new ArgumentException(
            $"{country.Name} is not participating in this war.",
            nameof(country));
    }

    public WarStance GetStance(Country country)
    {
        if (ReferenceEquals(Attacker, country))
            return AttackerStance;

        if (ReferenceEquals(Defender, country))
            return DefenderStance;

        throw new ArgumentException(
            $"{country.Name} is not participating in this war.",
            nameof(country));
    }

    public void SetStance(Country country, WarStance stance)
    {
        if (ReferenceEquals(Attacker, country))
            AttackerStance = stance;
        else if (ReferenceEquals(Defender, country))
            DefenderStance = stance;
        else
            throw new ArgumentException(
                $"{country.Name} is not participating in this war.",
                nameof(country));
    }
}
