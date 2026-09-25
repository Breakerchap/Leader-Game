using LeaderGame.Simulation;

namespace LeaderGame.Tests;

public class GameDateTests
{
    [Fact]
    public void NextMonth_AdvancesWithinYear()
    {
        var date = new GameDate(1450, 4);

        Assert.Equal(new GameDate(1450, 5), date.NextMonth());
    }

    [Fact]
    public void NextMonth_RollsDecemberIntoNextYear()
    {
        var date = new GameDate(1450, 12);

        Assert.Equal(new GameDate(1451, 1), date.NextMonth());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Constructor_RejectsInvalidMonth(int month)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameDate(1450, month));
    }
}
