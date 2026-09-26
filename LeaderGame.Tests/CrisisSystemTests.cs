using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Scenarios;
using LeaderGame.Simulation.Systems;

namespace LeaderGame.Tests;

public class CrisisSystemTests
{
    [Fact]
    public void SevereRegionalPressure_CreatesDecisionCrisis()
    {
        var state = DemoScenario.Create();
        var region = state.Player.Country.FindRegion("hochwald")!;

        region.Unrest = 82;
        region.CrownControl = 28;
        region.LocalElitePower = 86;

        var reports = CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis =>
                crisis.Status == PoliticalCrisisStatus.Active);

        Assert.Equal(
            PoliticalCrisisType.RegionalBreakdown,
            crisis.Type);
        Assert.Same(region, crisis.Region);
        Assert.True(crisis.AwaitingDecision);
        Assert.Contains(
            reports,
            report => report.Title.Contains(
                "Hochwald",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RegionalConcessions_BuyCalmByEntrenchingLocalPower()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("hochwald")!;

        region.Unrest = 82;
        region.CrownControl = 30;
        region.LocalElitePower = 85;

        CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis => crisis.Status ==
                PoliticalCrisisStatus.Active);

        var unrest = region.Unrest;
        var privileges = region.Privileges;
        var localPower = region.LocalElitePower;
        var control = region.CrownControl;

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.OfferRegionalConcessions);

        Assert.True(region.Unrest < unrest);
        Assert.True(region.Privileges > privileges);
        Assert.True(region.LocalElitePower > localPower);
        Assert.True(region.CrownControl < control);
    }

    [Fact]
    public void ResponseThatDoesNotSolveProblem_ReturnsAtHigherStage()
    {
        var state = DemoScenario.Create();
        var region = state.Player.Country.FindRegion("hochwald")!;

        region.Unrest = 85;
        region.CrownControl = 25;
        region.LocalElitePower = 88;

        CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis => crisis.Status ==
                PoliticalCrisisStatus.Active);

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.StrengthenRegionalControl);

        Assert.False(crisis.AwaitingDecision);

        CrisisSystem.ProcessMonth(state).ToList();
        CrisisSystem.ProcessMonth(state).ToList();

        Assert.Equal(2, crisis.Stage);
        Assert.True(crisis.AwaitingDecision);
    }

    [Fact]
    public void IgnoredBreakingPoint_ProducesCostlyButRecoverableOutcome()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("eastern-marches")!;

        region.Unrest = 90;
        region.CrownControl = 25;
        region.LocalElitePower = 90;

        var crisis = new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.RegionalBreakdown,
            Region = region,
            StartedOn = state.Date,
            Stage = 3,
            MonthsAtCurrentStage = 1,
            AwaitingDecision = true
        };

        state.PoliticalCrises.Add(crisis);

        var stability = country.Government.Stability;
        var control = region.CrownControl;

        CrisisSystem.ProcessMonth(state).ToList();

        Assert.Equal(
            PoliticalCrisisStatus.BrokeAgainstGovernment,
            crisis.Status);
        Assert.True(country.Government.Stability < stability);
        Assert.True(region.CrownControl < control);
        Assert.False(state.Player.HasLost);
    }

    [Fact]
    public void BorrowingDuringFiscalCrisis_BuysCashButDeepensDebt()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        country.Debt = country.Gdp * 0.25m;
        country.Treasury = 0;
        country.LastMonthlyBalance = -100_000m;

        CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis => crisis.Type ==
                PoliticalCrisisType.FiscalEmergency &&
                crisis.Status ==
                PoliticalCrisisStatus.Active);

        var debt = country.Debt;
        var treasury = country.Treasury;

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.BorrowForTime);

        Assert.True(country.Debt > debt);
        Assert.True(country.Treasury > treasury);
        Assert.Equal(
            PoliticalCrisisStatus.Active,
            crisis.Status);
    }

    [Fact]
    public void ConstitutionalCompromise_CoolsStandoffBySharingPower()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var leader = country.PoliticalFigures.First(character =>
            character.Id == 6);

        country.Government.Stability = 40;

        var bloc = new PoliticalBloc
        {
            Country = country,
            Leader = leader,
            Cohesion = 80
        };
        bloc.PowerBases.Add(
            PowerBaseType.RegionalElites);
        state.PoliticalBlocs.Add(bloc);

        var crisis = new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.PoliticalStandoff,
            RelatedBlocId = bloc.Id,
            StartedOn = state.Date
        };
        state.PoliticalCrises.Add(crisis);

        var independence =
            country.Government.LegislativeIndependence;
        var cohesion = bloc.Cohesion;

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.ConstitutionalCompromise);

        Assert.Equal(
            independence + 7,
            country.Government.LegislativeIndependence);
        Assert.True(bloc.Cohesion < cohesion);
        Assert.True(country.Government.Stability > 40);
    }

    [Fact]
    public void FiscalCrisis_ProducesConflictingInterestShapedAdvice()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        var crisis = new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.FiscalEmergency,
            StartedOn = state.Date
        };

        var advice = CrisisSystem.GetAdvice(
            state,
            crisis);

        Assert.Equal(3, advice.Count);
        Assert.Equal(
            3,
            advice.Select(item => item.Response)
                .Distinct()
                .Count());

        Assert.Contains(
            advice,
            item =>
                item.Advisor.Position == Position.Treasurer &&
                item.Response ==
                    PoliticalCrisisResponse.CutStateCommitments);

        Assert.Contains(
            advice,
            item =>
                item.Advisor.Position == Position.Marshal &&
                item.Response ==
                    PoliticalCrisisResponse.BorrowForTime);

        Assert.Contains(
            advice,
            item =>
                item.Advisor.Position == Position.Chancellor &&
                item.Response ==
                    PoliticalCrisisResponse.RaiseEmergencyRevenue);
    }

    [Fact]
    public void FollowingAdviserCrisisAdvice_ImprovesThatRelationship()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var marshal = country.GetOfficeHolder(
            Position.Marshal)!;

        country.Debt = country.Gdp * 0.25m;
        country.LastMonthlyBalance = -100_000m;

        var crisis = new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.FiscalEmergency,
            StartedOn = state.Date
        };
        state.PoliticalCrises.Add(crisis);

        var relationship =
            state.Relationships.GetOrCreate(
                marshal,
                country.Ruler);

        var opinion = relationship.Opinion;
        var trust = relationship.Trust;

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.BorrowForTime);

        Assert.True(relationship.Opinion > opinion);
        Assert.True(relationship.Trust > trust);
    }

    [Fact]
    public void LeavingGovernment_EndsPlayerControlOfGovernmentCrisis()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("hochwald")!;

        region.Unrest = 82;
        region.CrownControl = 28;

        var crisis = new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.RegionalBreakdown,
            Region = region,
            StartedOn = state.Date
        };
        state.PoliticalCrises.Add(crisis);

        country.Ruler = country.PoliticalFigures.First(character =>
            character.Id == 6);

        Assert.False(state.Player.IsInPower);

        var reports = CrisisSystem.ProcessMonth(state).ToList();

        Assert.Equal(
            PoliticalCrisisStatus.Resolved,
            crisis.Status);
        Assert.Contains(
            reports,
            report => report.Title.Contains(
                "passes to the new government",
                StringComparison.OrdinalIgnoreCase));

        var response = CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.FundRegionalRelief);

        Assert.Contains(
            "outside government",
            response.Details,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AgeingMonarch_WithUnsettledHeir_TriggersSuccessionCrisis()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        country.Ruler.Age = 66;
        country.Ruler.Health = 72;

        var reports =
            CrisisSystem.ProcessMonth(state)
                .ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis =>
                crisis.Type ==
                    PoliticalCrisisType.SuccessionDispute &&
                crisis.Status ==
                    PoliticalCrisisStatus.Active);

        Assert.True(crisis.AwaitingDecision);
        Assert.Contains(
            reports,
            report => report.Title.Contains(
                "succession",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PubliclyNamingStrongHeir_CanSettleSuccessionCrisis()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var heir = country.SuccessionOrder[0];

        country.Ruler.Age = 66;

        CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis => crisis.Type ==
                PoliticalCrisisType.SuccessionDispute);

        var legitimacy = heir.Legitimacy;

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.PubliclyNameSuccessor);

        Assert.True(heir.Legitimacy > legitimacy);
        Assert.Equal(
            PoliticalCrisisStatus.Resolved,
            crisis.Status);
    }

    [Fact]
    public void SuccessionCrisis_ProducesCompetingCabinetAdvice()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;

        var crisis = new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.SuccessionDispute,
            StartedOn = state.Date
        };

        var advice =
            CrisisSystem.GetAdvice(
                state,
                crisis);

        Assert.Contains(
            advice,
            item =>
                item.Advisor.Position ==
                    Position.Marshal &&
                item.Response ==
                    PoliticalCrisisResponse.PubliclyNameSuccessor);

        Assert.Contains(
            advice,
            item =>
                item.Advisor.Position ==
                    Position.Chancellor &&
                item.Response ==
                    PoliticalCrisisResponse.ConveneSuccessionSettlement);

        Assert.Contains(
            advice,
            item =>
                item.Advisor.Position ==
                    Position.Treasurer &&
                item.Response ==
                    PoliticalCrisisResponse.BalanceSuccessionFactions);
    }

    [Fact]
    public void FailedSuccessionSettlement_CreatesRivalFaction()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var rival = country.SuccessionOrder[1];

        country.Ruler.Age = 66;

        var crisis = new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.SuccessionDispute,
            StartedOn = state.Date,
            Stage = 3,
            MonthsAtCurrentStage = 1,
            AwaitingDecision = true
        };
        state.PoliticalCrises.Add(crisis);

        CrisisSystem.ProcessMonth(state).ToList();

        Assert.Equal(
            PoliticalCrisisStatus.BrokeAgainstGovernment,
            crisis.Status);

        Assert.Contains(
            state.PoliticalBlocs,
            bloc =>
                bloc.IsActive &&
                ReferenceEquals(
                    bloc.Leader,
                    rival));

        Assert.False(state.Player.HasLost);
    }

    [Fact]
    public void LosingWar_CanBecomeDomesticMilitaryCrisis()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var opponent = state.FindCountry("nordmark")!;

        var war = new War
        {
            Attacker = country,
            Defender = opponent,
            StartedOn = state.Date,
            WarScore = -42
        };
        war.MonthsActive = 5;
        state.Wars.Add(war);

        var reports =
            CrisisSystem.ProcessMonth(state)
                .ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis =>
                crisis.Type ==
                    PoliticalCrisisType.WarEmergency &&
                crisis.Status ==
                    PoliticalCrisisStatus.Active);

        Assert.Equal(war.Id, crisis.RelatedWarId);
        Assert.Contains(
            reports,
            report => report.Title.Contains(
                "military",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EmergencyMobilisation_TradesMoneyAndUnrestForMilitaryCapacity()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var opponent = state.FindCountry("nordmark")!;

        var war = new War
        {
            Attacker = country,
            Defender = opponent,
            StartedOn = state.Date,
            WarScore = -45
        };
        war.MonthsActive = 5;
        state.Wars.Add(war);

        CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis => crisis.Type ==
                PoliticalCrisisType.WarEmergency);

        var army = country.ArmySize;
        var readiness = country.ArmyReadiness;
        var unrest = country.PublicUnrest;
        var treasury = country.Treasury;
        var debt = country.Debt;

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.EmergencyMobilisation);

        Assert.True(country.ArmySize > army);
        Assert.True(country.ArmyReadiness > readiness);
        Assert.True(country.PublicUnrest > unrest);
        Assert.True(
            country.Treasury < treasury ||
            country.Debt > debt);
    }

    [Fact]
    public void DismissingMarshal_ShiftsBlameButCreatesCommandVacancy()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var opponent = state.FindCountry("nordmark")!;
        var marshal = country.GetOfficeHolder(
            Position.Marshal)!;

        var war = new War
        {
            Attacker = country,
            Defender = opponent,
            StartedOn = state.Date,
            WarScore = -45
        };
        war.MonthsActive = 5;
        state.Wars.Add(war);

        CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis => crisis.Type ==
                PoliticalCrisisType.WarEmergency);

        var readiness = country.ArmyReadiness;

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.DismissMarshal);

        Assert.Null(marshal.Position);
        Assert.Null(
            country.GetOfficeHolder(
                Position.Marshal));
        Assert.True(country.ArmyReadiness < readiness);
    }

    [Fact]
    public void PeaceCrisisChoice_UsesRealPeaceAcceptance()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var opponent = state.FindCountry("nordmark")!;

        var war = new War
        {
            Attacker = country,
            Defender = opponent,
            StartedOn = state.Date,
            WarScore = -55
        };
        war.MonthsActive = 8;
        opponent.WarExhaustion = 90;
        opponent.ArmyReadiness = 25;
        state.Wars.Add(war);

        CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis => crisis.Type ==
                PoliticalCrisisType.WarEmergency);

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.SeekPeaceSettlement);

        Assert.NotEqual(
            WarStatus.Active,
            war.Status);
        Assert.Equal(
            PoliticalCrisisStatus.Resolved,
            crisis.Status);
    }

    [Fact]
    public void FailedPeaceAttempt_LeavesWarCrisisAlive()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var opponent = state.FindCountry("nordmark")!;

        var war = new War
        {
            Attacker = country,
            Defender = opponent,
            StartedOn = state.Date,
            WarScore = -55
        };
        war.MonthsActive = 5;
        opponent.WarExhaustion = 0;
        opponent.ArmyReadiness = 95;
        state.Wars.Add(war);

        CrisisSystem.ProcessMonth(state).ToList();

        var crisis = Assert.Single(
            state.PoliticalCrises,
            crisis => crisis.Type ==
                PoliticalCrisisType.WarEmergency);

        CrisisSystem.Respond(
            state,
            crisis.Id,
            PoliticalCrisisResponse.SeekPeaceSettlement);

        Assert.Equal(
            WarStatus.Active,
            war.Status);
        Assert.Equal(
            PoliticalCrisisStatus.Active,
            crisis.Status);
        Assert.False(crisis.AwaitingDecision);
    }

    [Fact]
    public void MilitaryBreakingPoint_CreatesPressureToEndWar()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var opponent = state.FindCountry("nordmark")!;

        var war = new War
        {
            Attacker = country,
            Defender = opponent,
            StartedOn = state.Date,
            WarScore = -70
        };
        war.MonthsActive = 10;
        state.Wars.Add(war);

        var crisis = new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.WarEmergency,
            RelatedWarId = war.Id,
            StartedOn = state.Date,
            Stage = 3,
            MonthsAtCurrentStage = 1
        };
        state.PoliticalCrises.Add(crisis);

        CrisisSystem.ProcessMonth(state).ToList();

        Assert.Equal(
            PoliticalCrisisStatus.BrokeAgainstGovernment,
            crisis.Status);
        Assert.Equal(
            WarStance.Defensive,
            war.GetStance(country));
        Assert.Contains(
            state.PowerBaseDemands,
            demand =>
                !demand.IsResolved &&
                demand.Type ==
                    PowerBaseDemandType.EndWar);
        Assert.False(state.Player.HasLost);
    }

    [Fact]
    public void CrisisState_RoundTripsThroughSave()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var region = country.FindRegion("westmark")!;

        state.PoliticalCrises.Add(new PoliticalCrisis
        {
            Country = country,
            Type = PoliticalCrisisType.RegionalBreakdown,
            Region = region,
            StartedOn = state.Date,
            Stage = 2,
            MonthsActive = 4,
            MonthsAtCurrentStage = 1,
            AwaitingDecision = false,
            LastResponse =
                PoliticalCrisisResponse.FundRegionalRelief
        });

        var loaded = GameSaveService.Deserialize(
            GameSaveService.Serialize(state));

        var restored = Assert.Single(
            loaded.PoliticalCrises);

        Assert.Equal(2, restored.Stage);
        Assert.Equal(4, restored.MonthsActive);
        Assert.False(restored.AwaitingDecision);
        Assert.Equal(
            PoliticalCrisisResponse.FundRegionalRelief,
            restored.LastResponse);
        Assert.Same(
            loaded.Player.Country.FindRegion("westmark"),
            restored.Region);
    }

    [Fact]
    public void GameSession_ExposesCrisisChoicesAndAcceptsResponse()
    {
        var state = DemoScenario.Create();
        var region = state.Player.Country.FindRegion("hochwald")!;

        region.Unrest = 82;
        region.CrownControl = 28;
        region.LocalElitePower = 86;

        CrisisSystem.ProcessMonth(state).ToList();

        var session = new GameSession(
            new GameSimulation(state));

        var view = Assert.Single(session.View.Crises);

        Assert.True(view.CanRespond);
        Assert.Equal(3, view.Choices.Count);

        session.RespondToCrisis(
            view.Id,
            view.Choices[1].Response);

        Assert.False(
            Assert.Single(
                session.View.Crises).CanRespond);
    }
}
