using LeaderGame.Presentation;
using LeaderGame.Simulation;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Persistence;
using LeaderGame.Simulation.Scenarios;
using LeaderGame.Simulation.Systems;

namespace LeaderGame.Tests;

public class WarAimTests
{
    [Fact]
    public void GameSession_QueuesChosenWarAim()
    {
        var state = DemoScenario.Create();
        var session = new GameSession(
            new GameSimulation(state));

        session.DeclareWar(
            "nordmark",
            "Humiliate rival");

        var order =
            Assert.IsType<DeclareWarOrder>(
                Assert.Single(
                    state.PendingOrders));

        Assert.Equal(
            WarAim.HumiliateRival,
            order.Aim);
    }

    [Fact]
    public void Declaration_CarriesWarAimIntoWar()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var target = state.FindCountry("nordmark")!;
        var chancellor =
            country.GetOfficeHolder(
                Simulation.Characters.Position.Chancellor)!;

        var order = new DeclareWarOrder
        {
            Issuer = country.Ruler,
            Recipient = chancellor,
            IssuedOn = state.Date,
            SourceCountry = country,
            TargetCountry = target,
            Aim = WarAim.CommercialAccess
        };

        OrderProcessor.Process(state, order);

        var war = Assert.Single(
            state.Wars);

        Assert.Equal(
            WarAim.CommercialAccess,
            war.AttackerAim);
    }

    [Fact]
    public void HumiliationVictory_DamagesDefeatedRulerLegitimacy()
    {
        var state = DemoScenario.Create();
        var attacker = state.Player.Country;
        var defender = state.FindCountry("nordmark")!;

        var defenderLegitimacy =
            defender.Ruler.Legitimacy;
        var attackerLegitimacy =
            attacker.Ruler.Legitimacy;

        var war = new War
        {
            Attacker = attacker,
            Defender = defender,
            StartedOn = state.Date,
            AttackerAim = WarAim.HumiliateRival,
            WarScore = 99
        };
        defender.ArmySize = 0;
        state.Wars.Add(war);

        WarSystem.ProcessMonth(state).ToList();

        Assert.Equal(
            WarStatus.AttackerVictory,
            war.Status);
        Assert.True(
            defender.Ruler.Legitimacy <
            defenderLegitimacy);
        Assert.True(
            attacker.Ruler.Legitimacy >
            attackerLegitimacy);
    }

    [Fact]
    public void CommercialAccessVictory_ForcesTradeOpen()
    {
        var state = DemoScenario.Create();
        var attacker = state.Player.Country;
        var defender = state.FindCountry("nordmark")!;
        var relation =
            state.Diplomacy.GetOrCreate(
                attacker,
                defender);
        relation.HasTradeAgreement = false;

        var war = new War
        {
            Attacker = attacker,
            Defender = defender,
            StartedOn = state.Date,
            AttackerAim = WarAim.CommercialAccess,
            WarScore = 99
        };
        defender.ArmySize = 0;
        state.Wars.Add(war);

        WarSystem.ProcessMonth(state).ToList();

        Assert.Equal(
            WarStatus.AttackerVictory,
            war.Status);
        Assert.True(relation.HasTradeAgreement);
        Assert.NotNull(
            relation.TradeAgreementStartedOn);
        Assert.True(relation.Relations < 0);
    }

    [Fact]
    public void ReparationsAim_PaysMoreThanPoliticalWarAim()
    {
        static decimal VictoryGain(
            WarAim aim)
        {
            var state = DemoScenario.Create();
            var attacker =
                state.Player.Country;
            var defender =
                state.FindCountry("nordmark")!;

            defender.Treasury =
                10_000_000m;

            var before =
                attacker.Treasury;

            var war = new War
            {
                Attacker = attacker,
                Defender = defender,
                StartedOn = state.Date,
                AttackerAim = aim,
                WarScore = 99
            };

            defender.ArmySize = 0;
            state.Wars.Add(war);

            WarSystem.ProcessMonth(state)
                .ToList();

            return
                attacker.Treasury -
                before;
        }

        var reparations =
            VictoryGain(
                WarAim.Reparations);
        var humiliation =
            VictoryGain(
                WarAim.HumiliateRival);

        Assert.True(
            reparations >
            humiliation);
    }

    [Fact]
    public void WarAim_RoundTripsForActiveWarAndPendingDeclaration()
    {
        var state = DemoScenario.Create();
        var country = state.Player.Country;
        var target = state.FindCountry("nordmark")!;
        var chancellor =
            country.GetOfficeHolder(
                Simulation.Characters.Position.Chancellor)!;

        state.Wars.Add(new War
        {
            Attacker = target,
            Defender = country,
            StartedOn = state.Date,
            AttackerAim = WarAim.HumiliateRival
        });

        state.PendingOrders.Add(
            new DeclareWarOrder
            {
                Issuer = country.Ruler,
                Recipient = chancellor,
                IssuedOn = state.Date,
                SourceCountry = country,
                TargetCountry =
                    state.FindCountry("valeria")!,
                Aim = WarAim.CommercialAccess
            });

        var loaded =
            GameSaveService.Deserialize(
                GameSaveService.Serialize(state));

        Assert.Equal(
            WarAim.HumiliateRival,
            Assert.Single(
                loaded.Wars).AttackerAim);

        Assert.Equal(
            WarAim.CommercialAccess,
            Assert.IsType<DeclareWarOrder>(
                Assert.Single(
                    loaded.PendingOrders)).Aim);
    }
}
