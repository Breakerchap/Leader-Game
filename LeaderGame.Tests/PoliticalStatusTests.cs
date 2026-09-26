using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Tests;

public class PoliticalStatusTests
{
    [Fact]
    public void CrediblePlotter_CanBeArrestedAndPlotIsSuppressed()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var subject = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        var plot = new PoliticalPlot
        {
            Country = country,
            Instigator = subject,
            Progress = 75
        };
        state.Plots.Add(plot);

        simulation.AdvanceMonth();

        Assert.Equal(2, plot.DiscoveryStage);

        simulation.SubmitOrder(new ArrestCharacterOrder
        {
            Issuer = country.Ruler,
            Recipient = marshal,
            Subject = subject,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        Assert.Equal(PoliticalStatus.Imprisoned, subject.Status);
        Assert.True(plot.IsResolved);
        Assert.False(plot.Succeeded);
        Assert.Equal(0, PoliticalCalculations.GetThreatScore(state, country, subject));
    }

    [Fact]
    public void ArrestWithoutEvidence_CostsLegitimacyAndStability()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var subject = country.PoliticalFigures.Single(character =>
            character.FullName == "Marta Vogel");

        var legitimacy = country.Ruler.Legitimacy;
        var stability = country.Government.Stability;

        simulation.SubmitOrder(new ArrestCharacterOrder
        {
            Issuer = country.Ruler,
            Recipient = marshal,
            Subject = subject,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        Assert.Equal(PoliticalStatus.Imprisoned, subject.Status);
        Assert.True(country.Ruler.Legitimacy < legitimacy);
        Assert.True(country.Government.Stability < stability);
    }

    [Fact]
    public void FailedArrest_CreatesOrAcceleratesCoupPlot()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(Position.Marshal)!;
        var subject = country.PoliticalFigures.Single(character =>
            character.FullName == "Lukas Hartmann");

        country.ArmyReadiness = 0;
        marshal.Competence = 0;

        var marshalToRuler = state.Relationships.GetOrCreate(marshal, country.Ruler);
        marshalToRuler.Opinion = 0;
        marshalToRuler.Trust = 50;
        marshalToRuler.Fear = 0;
        marshal.Ambition = 50;
        marshal.SetAllegiance(PoliticalKeys.Country(country.Id), 50);

        simulation.SubmitOrder(new ArrestCharacterOrder
        {
            Issuer = country.Ruler,
            Recipient = marshal,
            Subject = subject,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        var plot = state.Plots.Single(candidate =>
            !candidate.IsResolved &&
            ReferenceEquals(candidate.Instigator, subject));

        Assert.Equal(PoliticalStatus.Active, subject.Status);
        Assert.True(plot.Progress >= 30);
        Assert.True(country.PublicUnrest > 20);
    }

    [Fact]
    public void ReleasedPrisoner_ReturnsToPoliticalLife()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var subject = country.PoliticalFigures.Single(character =>
            character.FullName == "Marta Vogel");

        subject.Status = PoliticalStatus.Imprisoned;

        simulation.SubmitOrder(new ReleasePrisonerOrder
        {
            Issuer = country.Ruler,
            Recipient = subject,
            IssuedOn = state.Date,
            Country = country
        });

        simulation.AdvanceMonth();

        Assert.Equal(PoliticalStatus.Active, subject.Status);
        Assert.Contains(subject, country.AvailableAdvisors);
    }

    [Fact]
    public void ImprisonedHeir_IsSkippedBySuccession()
    {
        var state = DemoScenario.Create();
        var simulation = new GameSimulation(state);
        var country = state.Player.Country;
        var oldRuler = country.Ruler;
        var heir = state.Player.Lineage.Members.Single(character =>
            !ReferenceEquals(character, oldRuler));
        var outsider = country.SuccessionOrder.First(character =>
            !state.Player.Lineage.Contains(character));

        heir.Status = PoliticalStatus.Imprisoned;
        oldRuler.IsAlive = false;

        simulation.AdvanceMonth();

        Assert.Same(outsider, country.Ruler);
        Assert.False(state.Player.HasLost);
        Assert.Same(heir, state.Player.CurrentCharacter);
        Assert.False(state.Player.IsInPower);
        Assert.Equal(PoliticalStatus.Imprisoned, heir.Status);
    }
}
