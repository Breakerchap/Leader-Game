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
}
