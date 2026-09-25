using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PowerBaseTests
{
    [Fact]
    public void BackingFromDominantPowerBase_RaisesInfluenceTarget()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);

        lukas.SetPowerBaseStanding(PowerBaseType.Military, 10);
        var weakTarget = PoliticalCalculations.GetInfluenceTarget(country, lukas);

        lukas.SetPowerBaseStanding(PowerBaseType.Military, 90);
        var strongTarget = PoliticalCalculations.GetInfluenceTarget(country, lukas);

        Assert.True(strongTarget > weakTarget);
    }

    [Fact]
    public void StrongPowerBaseBacking_RaisesPoliticalThreat()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var lukas = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
            country.SetPowerBaseStrength(powerBase, 0);

        country.SetPowerBaseStrength(PowerBaseType.Military, 100);

        lukas.SetPowerBaseStanding(PowerBaseType.Military, 0);
        var unsupportedThreat =
            PoliticalCalculations.GetThreatScore(state, country, lukas);

        lukas.SetPowerBaseStanding(PowerBaseType.Military, 100);
        var supportedThreat =
            PoliticalCalculations.GetThreatScore(state, country, lukas);

        Assert.True(supportedThreat > unsupportedThreat);
    }

    [Fact]
    public void TaxIncrease_CostsRulerBackingAmongEconomicBases()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        var merchantStanding =
            ruler.GetPowerBaseStanding(PowerBaseType.Merchants);
        var peasantStanding =
            ruler.GetPowerBaseStanding(PowerBaseType.Peasantry);

        simulation.SubmitOrder(new ChangeTaxOrder
        {
            Issuer = ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = 0.15m
        });

        simulation.AdvanceMonth();

        Assert.True(
            ruler.GetPowerBaseStanding(PowerBaseType.Merchants) <
            merchantStanding);
        Assert.True(
            ruler.GetPowerBaseStanding(PowerBaseType.Peasantry) <
            peasantStanding);
    }

    [Fact]
    public void ArmyBudgetIncrease_ImprovesMilitaryBacking()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var ruler = country.Ruler;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;
        var oldStanding =
            ruler.GetPowerBaseStanding(PowerBaseType.Military);

        simulation.SubmitOrder(new SetBudgetOrder
        {
            Issuer = ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetArmyFunding = 1.5m,
            TargetAdministrationFunding = country.AdministrationFunding,
            TargetCourtFunding = country.CourtFunding
        });

        simulation.AdvanceMonth();

        Assert.True(
            ruler.GetPowerBaseStanding(PowerBaseType.Military) >
            oldStanding);
    }

    [Fact]
    public void DemoGovernments_HaveDifferentPowerStructures()
    {
        var state = DemoScenario.Create();
        var falkenreich = state.FindCountry("falkenreich")!;
        var valeria = state.FindCountry("valeria")!;

        Assert.True(
            falkenreich.GetPowerBaseStrength(PowerBaseType.Aristocracy) >
            falkenreich.GetPowerBaseStrength(PowerBaseType.Merchants));

        Assert.True(
            valeria.GetPowerBaseStrength(PowerBaseType.Merchants) >
            valeria.GetPowerBaseStrength(PowerBaseType.Aristocracy));
    }
}
