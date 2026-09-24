namespace LeaderGame.Simulation;

public struct GameDate
{
    public int Year { get; private set; }
    public int Month { get; private set; }

    public GameDate(int year, int month)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month));

        Year = year;
        Month = month;
    }

    public void AdvanceMonth()
    {
        Month++;

        if (Month > 12)
        {
            Month = 1;
            Year++;
        }
    }

    public override string ToString()
    {
        return $"{Month:00}/{Year}";
    }
}