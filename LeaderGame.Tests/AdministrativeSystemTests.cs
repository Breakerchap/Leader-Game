using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Scenarios;
using LeaderGame.Simulation.Systems;

namespace LeaderGame.Tests;

public class AdministrativeSystemTests
{
    [Fact]
    public void DemoRegimes_HaveDistinctPeriodAdministrativeNetworks()
    {
        var falkenState = DemoScenario.Create();
        var valeriaState = DemoScenario.Create(ScenarioCatalog.ValeriaId);

        var falkenreich = falkenState.Player.Country;
        var valeria = valeriaState.Player.Country;

        Assert.Contains(
            falkenreich.AdministrativeOffices,
            office => office.Name == "Royal Chancery");
        Assert.Contains(
            falkenreich.AdministrativeOffices,
            office => office.Name == "Bailiffs and Seignorial Officers");
        Assert.Contains(
            valeria.AdministrativeOffices,
            office => office.Name == "Fiscal Chamber");
        Assert.Contains(
            valeria.AdministrativeOffices,
            office => office.Name == "Rectors and Civic Officers");

        Assert.True(
            valeria.GetAdministrativeOffice(AdministrativeFunction.Revenue)!.Capacity >
            falkenreich.GetAdministrativeOffice(AdministrativeFunction.Revenue)!.Capacity);
        Assert.True(
            valeria.GetAdministrativeOffice(AdministrativeFunction.LocalGovernment)!.Reach >
            falkenreich.GetAdministrativeOffice(AdministrativeFunction.LocalGovernment)!.Reach);
    }

    [Fact]
    public void SustainedAdministrativeUnderfunding_ErodesOfficeCapacityAndStateEfficiency()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        country.AdministrationFunding = 0.5m;

        var office = country.GetAdministrativeOffice(
            AdministrativeFunction.Revenue)!;
        var oldCapacity = office.Capacity;
        var oldEfficiency = country.AdministrativeEfficiency;

        var simulation = new GameSimulation(state);
        simulation.AdvanceMonth();
        simulation.AdvanceMonth();

        Assert.True(office.Capacity < oldCapacity);
        Assert.True(country.AdministrativeEfficiency < oldEfficiency);
    }

    [Fact]
    public void WeakRevenueMachinery_MakesTaxDirectiveHarderToExecute()
    {
        var normalState = DemoScenario.Create();
        var weakState = DemoScenario.Create();

        var weakRevenue = weakState.Player.Country.GetAdministrativeOffice(
            AdministrativeFunction.Revenue)!;
        weakRevenue.Capacity = 0;
        weakRevenue.Reach = 0;
        weakRevenue.Integrity = 20;
        weakRevenue.Workload = 100;

        IssueTaxDirective(normalState, 0.14m);
        IssueTaxDirective(weakState, 0.14m);

        new GameSimulation(normalState).AdvanceMonth();
        new GameSimulation(weakState).AdvanceMonth();

        Assert.True(
            weakState.Player.Country.TaxRate <
            normalState.Player.Country.TaxRate);
    }

    [Fact]
    public void AdministrativeNetwork_RoundTripsThroughSave()
    {
        var state = DemoScenario.Create();
        var office = state.Player.Country.GetAdministrativeOffice(
            AdministrativeFunction.LocalGovernment)!;

        office.Capacity = 37;
        office.Reach = 31;
        office.Integrity = 41;
        office.Workload = 82;

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var restored = loaded.Player.Country.GetAdministrativeOffice(
            AdministrativeFunction.LocalGovernment)!;

        Assert.Equal(office.Name, restored.Name);
        Assert.Equal(37, restored.Capacity);
        Assert.Equal(31, restored.Reach);
        Assert.Equal(41, restored.Integrity);
        Assert.Equal(82, restored.Workload);
        Assert.Equal(
            office.PatronageDependence,
            restored.PatronageDependence);
    }

    [Fact]
    public void AdministrativeDevelopment_IsCapabilityGatedNotJustDateGated()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        foreach (var office in country.AdministrativeOffices)
        {
            office.Capacity = 90;
            office.Reach = 90;
            office.Integrity = 90;
            office.Workload = 0;
            office.PatronageDependence = 20;
        }

        country.AdministrationFunding = 1.25m;

        state.Date = new GameDate(1499, 12);
        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(
            AdministrativeDevelopment.PatrimonialCourt,
            country.AdministrativeDevelopment);

        state.Date = new GameDate(1500, 1);
        new GameSimulation(state).AdvanceMonth();

        Assert.Equal(
            AdministrativeDevelopment.CentralisingBureaucracy,
            country.AdministrativeDevelopment);
        Assert.Contains(
            state.Reports,
            report => report.Title.Contains(
                "administration changes character",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AdministrativeDevelopment_RoundTripsThroughSave()
    {
        var state = DemoScenario.Create();
        state.Player.Country.AdministrativeDevelopment =
            AdministrativeDevelopment.FiscalMilitaryState;

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        Assert.Equal(
            AdministrativeDevelopment.FiscalMilitaryState,
            loaded.Player.Country.AdministrativeDevelopment);
    }

    [Fact]
    public void LaterAdministrativeForms_CostMoreAndUsePeriodAppropriateOfficeNames()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        var patrimonialCostMultiplier =
            AdministrativeSystem.GetAdministrationCostMultiplier(
                AdministrativeDevelopment.PatrimonialCourt);
        var professionalCostMultiplier =
            AdministrativeSystem.GetAdministrationCostMultiplier(
                AdministrativeDevelopment.ProfessionalCivilService);

        Assert.True(
            professionalCostMultiplier >
            patrimonialCostMultiplier);

        country.AdministrativeDevelopment =
            AdministrativeDevelopment.ProfessionalCivilService;

        var revenueOffice = country.GetAdministrativeOffice(
            AdministrativeFunction.Revenue)!;
        var militaryOffice = country.GetAdministrativeOffice(
            AdministrativeFunction.MilitaryLogistics)!;

        Assert.Equal(
            "Treasury",
            AdministrativeSystem.DisplayName(
                country,
                revenueOffice));
        Assert.Equal(
            "War Ministry",
            AdministrativeSystem.DisplayName(
                country,
                militaryOffice));
    }

    [Fact]
    public void GovernmentView_ReportsAdministrationInCoarsePoliticalTerms()
    {
        var session = new GameSession(
            new GameSimulation(DemoScenario.Create()));

        var view = session.View.Government;

        Assert.NotEmpty(view.AdministrativeOffices);
        Assert.Equal(
            "Patrimonial court administration",
            view.AdministrativeDevelopment);
        Assert.Contains(
            view.AdministrativeOffices,
            office => office.Name == "Royal Exchequer");
        Assert.All(
            view.AdministrativeOffices,
            office => Assert.DoesNotContain(
                "%",
                office.Performance,
                StringComparison.Ordinal));
    }

    private static void IssueTaxDirective(
        GameState state,
        decimal target)
    {
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        state.PendingOrders.Add(new ChangeTaxOrder
        {
            Issuer = country.Ruler,
            Recipient = treasurer,
            IssuedOn = state.Date,
            Country = country,
            TargetTaxRate = target
        });
    }
}
