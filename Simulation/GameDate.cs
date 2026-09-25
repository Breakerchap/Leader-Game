namespace LeaderGame.Simulation;

public readonly record struct GameDate
{
    public int Year { get; }
    public int Month { get; }

    public GameDate(int year, int month)
    {
        if (year < 1)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be positive.");

        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        Year = year;
        Month = month;
    }

    public GameDate NextMonth()
    {
        return Month == 12
            ? new GameDate(Year + 1, 1)
            : new GameDate(Year, Month + 1);
    }

    public override string ToString() => $"{Month:00}/{Year}";
}
