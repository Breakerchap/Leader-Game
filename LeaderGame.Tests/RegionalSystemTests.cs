using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;
using LeaderGame.Simulation.Systems;

namespace LeaderGame.Tests;

public class RegionalSystemTests
{
    [Fact]
    public void StartingCountries_HaveDistinctStrategicRegions()
    {
        var falken = DemoScenario.Create();
        var valeria = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);

        Assert.Equal(5, falken.Player.Country.Regions.Count);
        Assert.Equal(4, valeria.Player.Country.Regions.Count);
        Assert.Contains(
            falken.Player.Country.Regions,
            region => region.Name == "Hochwald" &&
                      region.LocalElitePower > region.CrownControl);
        Assert.Contains(
            valeria.Player.Country.Regions,
            region => region.Name == "Vieri Lagoon" &&
                      region.Prosperity >= 80);
    }

    [Fact]
    public void WeakRegionalControl_ReducesEffectiveTaxCollection()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        var baseline =
            EconomySystem.CalculateMonthlyTaxRevenue(country);

        var westmark = country.FindRegion("westmark")!;
        westmark.CrownControl = 5;
        westmark.Privileges = 95;
        westmark.Unrest = 90;

        var weakened =
            EconomySystem.CalculateMonthlyTaxRevenue(country);

        Assert.True(weakened < baseline);
    }

    [Fact]
    public void StrengtheningCentralAdministration_TradesOrderForControl()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("hochwald")!;
        var chancellor = country.GetOfficeHolder(
            Position.Chancellor)!;

        var control = region.CrownControl;
        var unrest = region.Unrest;
        var privileges = region.Privileges;

        var order = new RegionalActionOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Region = region,
            ActionType = RegionalActionType.AssertCentralAuthority
        };

        RegionalSystem.Process(state, order);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(region.CrownControl > control);
        Assert.True(region.Unrest > unrest);
        Assert.True(region.Privileges < privileges);
    }

    [Fact]
    public void LocalCompact_CalmsRegionButStrengthensLocalPower()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("eastern-marches")!;
        var chancellor = country.GetOfficeHolder(
            Position.Chancellor)!;

        var control = region.CrownControl;
        var unrest = region.Unrest;
        var localPower = region.LocalElitePower;

        var order = new RegionalActionOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Region = region,
            ActionType = RegionalActionType.BargainWithLocalElites
        };

        RegionalSystem.Process(state, order);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(region.Unrest < unrest);
        Assert.True(region.LocalElitePower > localPower);
        Assert.True(region.CrownControl < control);
    }

    [Fact]
    public void WeakEliteDominatedRegion_StrengthensRegionalElitePolitics()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("hochwald")!;

        state.Date = new GameDate(1450, 3);
        region.CrownControl = 30;
        region.LocalElitePower = 85;

        var oldStrength =
            country.GetPowerBaseStrength(
                PowerBaseType.RegionalElites);
        var oldStanding =
            country.Ruler.GetPowerBaseStanding(
                PowerBaseType.RegionalElites);

        RegionalSystem.ProcessMonth(state).ToList();

        Assert.Equal(
            oldStrength + 1,
            country.GetPowerBaseStrength(
                PowerBaseType.RegionalElites));
        Assert.Equal(
            oldStanding - 1,
            country.Ruler.GetPowerBaseStanding(
                PowerBaseType.RegionalElites));
    }

    [Fact]
    public void GameSession_AllowsOnlyOneRegionalInterventionPerMonth()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(
            new GameSimulation(state));

        session.TakeRegionalAction(
            "InvestInRegion",
            "westmark");
        session.TakeRegionalAction(
            "AssertCentralAuthority",
            "hochwald");

        var order = Assert.IsType<RegionalActionOrder>(
            Assert.Single(state.PendingOrders));

        Assert.Equal("westmark", order.Region.Id);
        Assert.False(
            session.View.Government.CanDirectRegions);
        Assert.Contains(
            "already",
            session.View.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegionsAndPendingRegionalAction_RoundTripThroughSave()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("westmark")!;
        var chancellor = country.GetOfficeHolder(
            Position.Chancellor)!;

        region.Unrest = 61;
        region.CrownControl = 47;

        state.PendingOrders.Add(new RegionalActionOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            Country = country,
            Region = region,
            ActionType = RegionalActionType.InvestInRegion
        });

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var restoredRegion =
            loaded.Player.Country.FindRegion("westmark")!;

        Assert.Equal(61, restoredRegion.Unrest);
        Assert.Equal(47, restoredRegion.CrownControl);

        var restoredOrder =
            Assert.IsType<RegionalActionOrder>(
                Assert.Single(loaded.PendingOrders));

        Assert.Same(restoredRegion, restoredOrder.Region);
        Assert.Equal(
            RegionalActionType.InvestInRegion,
            restoredOrder.ActionType);
    }
}
