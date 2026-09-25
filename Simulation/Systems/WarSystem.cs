using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class WarSystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var reports = new List<SimulationReport>();
        var countriesAtWar = new HashSet<Country>();

        foreach (var war in state.Wars.Where(war => war.Status == WarStatus.Active).ToList())
        {
            countriesAtWar.Add(war.Attacker);
            countriesAtWar.Add(war.Defender);

            reports.AddRange(ProcessWarMonth(state, war));
        }

        foreach (var country in state.Countries.Where(country => !countriesAtWar.Contains(country)))
        {
            country.WarExhaustion -= 0.6;
        }

        return reports;
    }

    private static IEnumerable<SimulationReport> ProcessWarMonth(
        GameState state,
        War war)
    {
        war.MonthsActive++;

        var attackerArmyBefore = war.Attacker.ArmySize;
        var defenderArmyBefore = war.Defender.ArmySize;

        var attackerStrength = CalculateEffectiveStrength(
            state,
            war.Attacker,
            war.AttackerStance);
        var defenderStrength = CalculateEffectiveStrength(
            state,
            war.Defender,
            war.DefenderStance);

        var totalStrength = Math.Max(1.0, attackerStrength + defenderStrength);
        var margin = (attackerStrength - defenderStrength) / totalStrength;
        var battleNoise = (state.Random.NextDouble() - 0.5) * 6.0;

        var scoreChange =
            margin * 22.0 +
            battleNoise;

        war.WarScore += scoreChange;

        var attackerLossRate = CalculateLossRate(
            margin,
            war.AttackerStance,
            losingSide: margin < 0);
        var defenderLossRate = CalculateLossRate(
            -margin,
            war.DefenderStance,
            losingSide: margin > 0);

        var attackerLosses = CalculateLosses(
            state,
            attackerArmyBefore,
            attackerLossRate);
        var defenderLosses = CalculateLosses(
            state,
            defenderArmyBefore,
            defenderLossRate);

        war.Attacker.ArmySize = Math.Max(0, war.Attacker.ArmySize - attackerLosses);
        war.Defender.ArmySize = Math.Max(0, war.Defender.ArmySize - defenderLosses);

        ApplyCampaignStrain(
            war.Attacker,
            attackerArmyBefore,
            attackerLosses,
            war.AttackerStance);
        ApplyCampaignStrain(
            war.Defender,
            defenderArmyBefore,
            defenderLosses,
            war.DefenderStance);

        ApplyOperationalCost(war.Attacker, war.AttackerStance);
        ApplyOperationalCost(war.Defender, war.DefenderStance);

        var resolution = ResolveIfDecisive(state, war);

        if (resolution is not null)
            yield return resolution;
    }

    private static double CalculateEffectiveStrength(
        GameState state,
        Country country,
        WarStance stance)
    {
        var marshal = country.GetOfficeHolder(Position.Marshal);
        var commander = marshal ?? country.Ruler;

        var willingness = marshal is null
            ? 70.0
            : PoliticalCalculations.GetOrderWillingness(
                state,
                country,
                marshal,
                country.Ruler);

        var readinessFactor =
            0.45 + country.ArmyReadiness / 100.0 * 0.55;
        var fundingFactor =
            0.75 + (double)country.ArmyFunding * 0.25;
        var commanderFactor =
            0.70 + commander.Competence / 250.0;
        var willingnessFactor =
            0.80 + willingness / 500.0;
        var stanceFactor = stance switch
        {
            WarStance.Aggressive => 1.08,
            WarStance.Defensive => 0.94,
            _ => 1.0
        };

        var randomFactor =
            0.92 + state.Random.NextDouble() * 0.16;

        return Math.Max(
            0,
            country.ArmySize *
            readinessFactor *
            fundingFactor *
            commanderFactor *
            willingnessFactor *
            stanceFactor *
            randomFactor);
    }

    private static double CalculateLossRate(
        double disadvantage,
        WarStance stance,
        bool losingSide)
    {
        var rate =
            0.006 +
            Math.Max(0, -disadvantage) * 0.020;

        if (losingSide)
            rate += 0.004;

        rate += stance switch
        {
            WarStance.Aggressive => 0.004,
            WarStance.Defensive => -0.002,
            _ => 0
        };

        return Math.Clamp(rate, 0.002, 0.04);
    }

    private static int CalculateLosses(
        GameState state,
        int armySize,
        double lossRate)
    {
        if (armySize <= 0)
            return 0;

        var variation =
            0.85 + state.Random.NextDouble() * 0.30;

        return Math.Clamp(
            (int)Math.Round(armySize * lossRate * variation),
            1,
            armySize);
    }

    private static void ApplyCampaignStrain(
        Country country,
        int armyBefore,
        int losses,
        WarStance stance)
    {
        var readinessLoss = stance switch
        {
            WarStance.Aggressive => 2.5,
            WarStance.Defensive => 1.0,
            _ => 1.6
        };

        country.ArmyReadiness -= readinessLoss;

        var casualtyShare = armyBefore <= 0
            ? 0
            : losses / (double)armyBefore;

        var exhaustionGain =
            0.8 +
            casualtyShare * 30 +
            (country.LastMonthlyBalance < 0 ? 0.5 : 0) +
            (stance == WarStance.Aggressive ? 0.4 : 0);

        country.WarExhaustion += exhaustionGain;
        country.PublicUnrest += exhaustionGain * 0.15;
        country.Government.Stability -= exhaustionGain * 0.08;
    }

    private static void ApplyOperationalCost(
        Country country,
        WarStance stance)
    {
        var stanceMultiplier = stance switch
        {
            WarStance.Aggressive => 1.25m,
            WarStance.Defensive => 0.85m,
            _ => 1.0m
        };

        var cost =
            country.ArmySize *
            8m *
            stanceMultiplier;

        country.Treasury -= cost;

        if (country.Treasury >= 0)
            return;

        country.Debt += -country.Treasury;
        country.Treasury = 0;
    }

    private static SimulationReport? ResolveIfDecisive(
        GameState state,
        War war)
    {
        if (war.Attacker.ArmySize <= 0 || war.WarScore >= 100)
            return EndWar(state, war, attackerWon: true);

        if (war.Defender.ArmySize <= 0 || war.WarScore <= -100)
            return EndWar(state, war, attackerWon: false);

        if (war.MonthsActive >= 48 && Math.Abs(war.WarScore) < 15)
        {
            war.Status = WarStatus.WhitePeace;

            var relation = state.Diplomacy.GetOrCreate(war.Attacker, war.Defender);
            relation.Tension = Math.Max(65, relation.Tension - 20);

            return PlayerRelevantReport(
                state,
                war,
                "The war ends in white peace",
                $"{war.Attacker.Name} and {war.Defender.Name} end an inconclusive " +
                $"war after {war.MonthsActive} months.");
        }

        return null;
    }

    private static SimulationReport? EndWar(
        GameState state,
        War war,
        bool attackerWon)
    {
        var winner = attackerWon ? war.Attacker : war.Defender;
        var loser = attackerWon ? war.Defender : war.Attacker;

        war.Status = attackerWon
            ? WarStatus.AttackerVictory
            : WarStatus.DefenderVictory;

        var reparations = Math.Min(
            loser.Treasury,
            loser.Gdp * 0.01m);

        loser.Treasury -= reparations;
        winner.Treasury += reparations;

        winner.Government.Stability += 2;
        loser.Government.Stability -= 3;
        loser.PublicUnrest += 3;

        var relation = state.Diplomacy.GetOrCreate(winner, loser);
        relation.Relations = Math.Min(relation.Relations, -75);
        relation.Trust = Math.Min(relation.Trust, 10);
        relation.Tension = 80;

        return PlayerRelevantReport(
            state,
            war,
            $"{winner.Name} wins the war",
            $"{winner.Name} defeats {loser.Name} after {war.MonthsActive} months. " +
            $"The victor receives {reparations:N0} in reparations.");
    }

    private static SimulationReport? PlayerRelevantReport(
        GameState state,
        War war,
        string title,
        string details)
    {
        return war.IsParticipant(state.Player.Country)
            ? new SimulationReport(
                state.Date,
                ReportCategory.Military,
                title,
                details)
            : null;
    }
}
