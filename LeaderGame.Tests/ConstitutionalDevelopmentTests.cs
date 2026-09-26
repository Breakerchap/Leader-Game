using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;
using LeaderGame.Simulation.Systems;

namespace LeaderGame.Tests;

public class ConstitutionalDevelopmentTests
{
    [Fact]
    public void FiscalBargainingUnderPressure_StrengthensEstates()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        state.Date = new GameDate(1500, 1);
        country.LastMonthlyBalance = -100_000m;

        state.LegislativeProposals.Add(new LegislativeProposal
        {
            Country = country,
            Sponsor = country.Ruler,
            Drafter = treasurer,
            Type = LegislativeProposalType.TaxRate,
            CreatedOn = new GameDate(1499, 12),
            Status = LegislativeProposalStatus.Rejected,
            TargetTaxRate = 0.20m
        });

        var oldIndependence =
            country.Government.LegislativeIndependence;

        var reports =
            ConstitutionalDevelopmentSystem.ProcessMonth(state)
                .ToList();

        Assert.True(
            country.Government.LegislativeIndependence >
            oldIndependence);
        Assert.Contains(
            reports,
            report => report.Title.Contains(
                "gains constitutional leverage",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LongCentralisationWithoutBargaining_CanWeakenEstates()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        state.Date = new GameDate(1600, 1);
        country.AdministrativeDevelopment =
            AdministrativeDevelopment.CentralisingBureaucracy;
        country.Government.Stability = 85;

        foreach (var powerBase in new[]
                 {
                     PowerBaseType.Aristocracy,
                     PowerBaseType.Clergy,
                     PowerBaseType.RegionalElites,
                     PowerBaseType.Merchants
                 })
        {
            country.Ruler.SetPowerBaseStanding(
                powerBase,
                90);
        }

        var oldIndependence =
            country.Government.LegislativeIndependence;

        ConstitutionalDevelopmentSystem
            .ProcessMonth(state)
            .ToList();

        Assert.Equal(
            oldIndependence - 1,
            country.Government.LegislativeIndependence);
    }

    [Fact]
    public void GreatCouncil_DoesNotWitherSimplyBecauseCentralStateIsStrong()
    {
        var state = DemoScenario.Create(
            ScenarioCatalog.ValeriaId);
        var country = state.Player.Country;

        state.Date = new GameDate(1700, 1);
        country.AdministrativeDevelopment =
            AdministrativeDevelopment.FiscalMilitaryState;
        country.Government.Stability = 90;

        var oldIndependence =
            country.Government.LegislativeIndependence;

        ConstitutionalDevelopmentSystem
            .ProcessMonth(state)
            .ToList();

        Assert.Equal(
            oldIndependence,
            country.Government.LegislativeIndependence);
    }

    [Fact]
    public void ConstitutionalDevelopment_OnlyRunsAtYearBoundary()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var treasurer = country.GetOfficeHolder(Position.Treasurer)!;

        state.Date = new GameDate(1500, 6);
        country.LastMonthlyBalance = -100_000m;

        state.LegislativeProposals.Add(new LegislativeProposal
        {
            Country = country,
            Sponsor = country.Ruler,
            Drafter = treasurer,
            Type = LegislativeProposalType.TaxRate,
            CreatedOn = new GameDate(1500, 5),
            Status = LegislativeProposalStatus.Rejected,
            TargetTaxRate = 0.20m
        });

        var oldIndependence =
            country.Government.LegislativeIndependence;

        var reports =
            ConstitutionalDevelopmentSystem.ProcessMonth(state)
                .ToList();

        Assert.Equal(
            oldIndependence,
            country.Government.LegislativeIndependence);
        Assert.Empty(reports);
    }
}
