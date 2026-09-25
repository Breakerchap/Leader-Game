using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Politics;

namespace LeaderGame.Simulation.Diplomacy;

public static class DiplomaticCalculations
{
    public static double GetMissionEffectiveness(
        GameState state,
        Country sourceCountry,
        Character chancellor)
    {
        var willingness = PoliticalCalculations.GetOrderWillingness(
            state,
            sourceCountry,
            chancellor,
            sourceCountry.Ruler);

        return Math.Clamp(
            chancellor.Competence * 0.55 +
            willingness * 0.45,
            0,
            100);
    }

    public static double GetPersonalRulerModifier(
        GameState state,
        Country sourceCountry,
        Country targetCountry)
    {
        var targetToSource = state.Relationships.GetOrCreate(
            targetCountry.Ruler,
            sourceCountry.Ruler);

        var opinionContribution =
            targetToSource.Opinion / 100.0 * 12.0;
        var trustContribution =
            (targetToSource.Trust - 50) / 50.0 * 8.0;

        return Math.Clamp(
            opinionContribution + trustContribution,
            -20,
            20);
    }

    public static double GetTradeAcceptanceScore(
        GameState state,
        Country sourceCountry,
        Country targetCountry,
        Character sourceChancellor)
    {
        var relation = state.Diplomacy.GetOrCreate(sourceCountry, targetCountry);
        var missionEffectiveness = GetMissionEffectiveness(
            state,
            sourceCountry,
            sourceChancellor);

        var targetDiplomat =
            targetCountry.GetOfficeHolder(Position.Chancellor);
        var foreignAdministrativeJudgement =
            targetDiplomat?.Competence ??
            targetCountry.Ruler.Competence;

        var debtToGdp = targetCountry.Gdp <= 0
            ? 0
            : (double)(targetCountry.Debt / targetCountry.Gdp);

        var economicNeedBonus = Math.Min(10, debtToGdp * 40);
        var personalModifier = GetPersonalRulerModifier(
            state,
            sourceCountry,
            targetCountry);

        return Math.Clamp(
            40 +
            relation.Relations * 0.30 +
            relation.Trust * 0.20 -
            relation.Tension * 0.35 +
            missionEffectiveness * 0.15 +
            foreignAdministrativeJudgement * 0.10 +
            economicNeedBonus +
            personalModifier,
            0,
            100);
    }

    public static double GetTradeDesirabilityScore(
        GameState state,
        Country sourceCountry,
        Country targetCountry,
        Character sourceChancellor)
    {
        var relation = state.Diplomacy.GetOrCreate(sourceCountry, targetCountry);
        var rulerToTarget = state.Relationships.GetOrCreate(
            sourceCountry.Ruler,
            targetCountry.Ruler);

        var personalPreference =
            rulerToTarget.Opinion / 100.0 * 12.0 +
            (rulerToTarget.Trust - 50) / 50.0 * 8.0;

        var debtToGdp = sourceCountry.Gdp <= 0
            ? 0
            : (double)(sourceCountry.Debt / sourceCountry.Gdp);

        var economicNeedBonus = Math.Min(12, debtToGdp * 45);

        return Math.Clamp(
            42 +
            relation.Relations * 0.30 +
            relation.Trust * 0.20 -
            relation.Tension * 0.35 +
            sourceChancellor.Competence * 0.08 +
            personalPreference +
            economicNeedBonus,
            0,
            100);
    }

    public static double GetNonAggressionAcceptanceScore(
        GameState state,
        Country sourceCountry,
        Country targetCountry,
        Character sourceChancellor)
    {
        var relation = state.Diplomacy.GetOrCreate(sourceCountry, targetCountry);
        var missionEffectiveness = GetMissionEffectiveness(
            state,
            sourceCountry,
            sourceChancellor);
        var personalModifier = GetPersonalRulerModifier(
            state,
            sourceCountry,
            targetCountry);

        var sourcePower = GetMilitaryPower(sourceCountry);
        var targetPower = GetMilitaryPower(targetCountry);
        var strategicNeed = targetPower <= 0
            ? 0
            : Math.Clamp((sourcePower / targetPower - 1.0) * 12.0, -6, 10);

        return Math.Clamp(
            42 +
            relation.Relations * 0.25 +
            relation.Trust * 0.25 -
            relation.Tension * 0.18 -
            relation.BorderDisputeSeverity * 0.08 +
            missionEffectiveness * 0.10 +
            personalModifier +
            strategicNeed,
            0,
            100);
    }

    public static double GetNonAggressionDesirabilityScore(
        GameState state,
        Country sourceCountry,
        Country targetCountry,
        Character sourceChancellor)
    {
        var relation = state.Diplomacy.GetOrCreate(sourceCountry, targetCountry);
        var personalModifier = GetPersonalRulerModifier(
            state,
            targetCountry,
            sourceCountry);

        var sourcePower = GetMilitaryPower(sourceCountry);
        var targetPower = GetMilitaryPower(targetCountry);
        var vulnerability = sourcePower <= 0
            ? 0
            : Math.Clamp((targetPower / sourcePower - 1.0) * 14.0, -5, 12);

        var strategicFriction =
            Math.Min(15, relation.Tension * 0.08 +
                         relation.BorderDisputeSeverity * 0.08);

        return Math.Clamp(
            40 +
            relation.Relations * 0.22 +
            relation.Trust * 0.22 -
            relation.Tension * 0.10 -
            relation.BorderDisputeSeverity * 0.05 +
            sourceChancellor.Competence * 0.08 +
            personalModifier +
            vulnerability +
            strategicFriction,
            0,
            100);
    }

    public static double GetMilitaryPower(Country country)
    {
        return country.ArmySize *
               (0.50 + country.ArmyReadiness / 200.0);
    }


}
