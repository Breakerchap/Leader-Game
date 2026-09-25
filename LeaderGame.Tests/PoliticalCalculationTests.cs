using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PoliticalCalculationTests
{
    [Fact]
    public void DangerousAmbitiousOutsider_HasHigherThreatThanLoyalCourtier()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        var marta = country.PoliticalFigures.Single(character =>
            character.FullName == "Marta Vogel");
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        var martaThreat = PoliticalCalculations.GetThreatScore(state, country, marta);
        var lukasThreat = PoliticalCalculations.GetThreatScore(state, country, lukas);

        Assert.True(lukasThreat > martaThreat);
        Assert.True(lukasThreat > 60);
    }

    [Fact]
    public void Fear_CanIncreaseComplianceWithoutImprovingOpinion()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");
        var ruler = country.Ruler;

        var relationship = state.Relationships.GetOrCreate(lukas, ruler);
        var originalOpinion = relationship.Opinion;
        var withoutFear = PoliticalCalculations.GetOrderWillingness(
            state,
            country,
            lukas,
            ruler);

        relationship.Fear = 100;

        var withFear = PoliticalCalculations.GetOrderWillingness(
            state,
            country,
            lukas,
            ruler);

        Assert.True(withFear > withoutFear);
        Assert.Equal(originalOpinion, relationship.Opinion);
    }
}
