using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Military;

public static class PeaceCalculations
{
    public static double GetAcceptanceScore(
        GameState state,
        War war,
        Country proposer,
        PeaceOfferTerms terms)
    {
        if (war.Status != WarStatus.Active || !war.IsParticipant(proposer))
            return 0;

        var target = war.OpponentOf(proposer);
        var proposerScore = ReferenceEquals(proposer, war.Attacker)
            ? war.WarScore
            : -war.WarScore;

        var targetToProposer = state.Relationships.GetOrCreate(
            target.Ruler,
            proposer.Ruler);

        var personalModifier =
            targetToProposer.Opinion / 100.0 * 4.0 +
            (targetToProposer.Trust - 50) / 50.0 * 3.0;

        var debtToGdp = target.Gdp <= 0
            ? 0
            : (double)(target.Debt / target.Gdp);

        var economicPressure =
            Math.Min(12, debtToGdp * 35);

        var termModifier = terms switch
        {
            PeaceOfferTerms.WhitePeace => 0,
            PeaceOfferTerms.DemandReparations => -25,
            PeaceOfferTerms.OfferReparations => 25,
            _ => 0
        };

        return Math.Clamp(
            35 +
            proposerScore * 0.55 +
            target.WarExhaustion * 0.45 +
            (100 - target.ArmyReadiness) * 0.12 +
            economicPressure +
            personalModifier +
            termModifier,
            0,
            100);
    }
}
