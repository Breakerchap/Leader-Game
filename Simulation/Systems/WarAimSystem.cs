using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Politics;

namespace LeaderGame.Simulation.Systems;

internal static class WarAimSystem
{
    public static string Label(WarAim aim) =>
        aim switch
        {
            WarAim.Reparations => "Reparations",
            WarAim.HumiliateRival => "Humiliate rival regime",
            WarAim.CommercialAccess => "Force commercial access",
            _ => aim.ToString()
        };

    public static decimal DecisiveReparationRate(
        War war,
        bool attackerWon)
    {
        if (!attackerWon)
            return 0.010m;

        return war.AttackerAim switch
        {
            WarAim.Reparations => 0.015m,
            WarAim.HumiliateRival => 0.003m,
            WarAim.CommercialAccess => 0.003m,
            _ => 0.010m
        };
    }

    /// <summary>
    /// Applies the non-cash part of an attacker war aim once the attacker has
    /// secured a sufficiently favourable settlement.
    /// </summary>
    public static string ApplyAttackerAim(
        GameState state,
        War war)
    {
        return war.AttackerAim switch
        {
            WarAim.Reparations =>
                ApplyReparationsPoliticalEffects(
                    war),

            WarAim.HumiliateRival =>
                ApplyHumiliation(
                    war),

            WarAim.CommercialAccess =>
                ApplyCommercialAccess(
                    state,
                    war),

            _ => string.Empty
        };
    }

    private static string ApplyReparationsPoliticalEffects(
        War war)
    {
        war.Attacker.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Merchants,
            3);
        war.Attacker.Ruler.Legitimacy += 2;

        return
            " The victory is presented domestically as making the enemy pay for the war, strengthening the ruler with commercial interests.";
    }

    private static string ApplyHumiliation(
        War war)
    {
        war.Attacker.Ruler.Legitimacy += 7;
        war.Attacker.Government.Stability += 2;
        war.Attacker.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Military,
            5);

        war.Defender.Ruler.Legitimacy -= 10;
        war.Defender.Government.Stability -= 3;
        war.Defender.PublicUnrest += 2;

        return
            $" {war.Defender.Ruler.FullName}'s regime is publicly humiliated by the settlement. " +
            "The victor gains prestige and military backing, while the defeated ruler's legitimacy suffers.";
    }

    private static string ApplyCommercialAccess(
        GameState state,
        War war)
    {
        var relation =
            state.Diplomacy.GetOrCreate(
                war.Attacker,
                war.Defender);

        relation.HasTradeAgreement = true;
        relation.TradeAgreementStartedOn =
            state.Date;

        war.Attacker.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Merchants,
            7);
        war.Attacker.Government.Stability += 1;

        war.Defender.Ruler.ChangePowerBaseStanding(
            PowerBaseType.Merchants,
            -3);
        war.Defender.PublicUnrest += 1;

        return
            $" {war.Defender.Name} is forced to reopen commercial access to {war.Attacker.Name}. " +
            "The victor's merchants benefit, but the coerced arrangement begins with very little political trust.";
    }
}
