using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PatronagePoliticsTests
{
    [Fact]
    public void AppointingPopularFigure_CanBuySupportButStrengthensAppointee()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var currentMarshal = country.GetOfficeHolder(Position.Marshal)!;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);
        ruler.SetPowerBaseStanding(PowerBaseType.Military, 30);
        currentMarshal.SetPowerBaseStanding(PowerBaseType.Military, 20);
        lukas.SetPowerBaseStanding(PowerBaseType.Military, 90);

        var rulerBackingBefore =
            ruler.GetPowerBaseStanding(PowerBaseType.Military);
        var lukasBackingBefore =
            lukas.GetPowerBaseStanding(PowerBaseType.Military);

        simulation.SubmitOrder(new AppointAdvisorOrder
        {
            Issuer = ruler,
            Recipient = lukas,
            IssuedOn = state.Date,
            Country = country,
            Position = Position.Marshal
        });

        simulation.AdvanceMonth();

        Assert.Same(lukas, country.GetOfficeHolder(Position.Marshal));
        Assert.True(
            ruler.GetPowerBaseStanding(PowerBaseType.Military) >
            rulerBackingBefore);
        Assert.True(
            lukas.GetPowerBaseStanding(PowerBaseType.Military) >
            lukasBackingBefore);
    }

    [Fact]
    public void DismissingWellBackedOfficeHolder_CostsRulerSupport()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);
        ruler.SetPowerBaseStanding(PowerBaseType.Military, 60);
        marshal.SetPowerBaseStanding(PowerBaseType.Military, 90);

        var rulerBackingBefore =
            ruler.GetPowerBaseStanding(PowerBaseType.Military);
        var marshalBackingBefore =
            marshal.GetPowerBaseStanding(PowerBaseType.Military);

        simulation.SubmitOrder(new DismissAdvisorOrder
        {
            Issuer = ruler,
            Recipient = marshal,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        Assert.Null(marshal.Position);
        Assert.True(
            ruler.GetPowerBaseStanding(PowerBaseType.Military) <
            rulerBackingBefore);
        Assert.True(
            marshal.GetPowerBaseStanding(PowerBaseType.Military) >
            marshalBackingBefore);
    }
}
