using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;

namespace LeaderGame.Simulation.Systems;

/// <summary>
/// Lightweight strategic adaptation for governments the player does not
/// control. This is not meant to be perfect optimisation: foreign governments
/// should have recognisable priorities and make imperfect policy choices.
/// </summary>
internal static class ForeignGovernmentSystem
{
    public static void ProcessMonth(GameState state)
    {
        if (state.Date.Month % 3 == 0)
        {
            foreach (var country in state.Countries.Where(country =>
                         !ReferenceEquals(country, state.Player.Country)))
            {
                AdaptDomesticPolicy(state, country);
            }
        }

        if (state.Date.Month % 6 == 0)
            AdaptAiTradeNetwork(state);
    }

    private static void AdaptDomesticPolicy(
        GameState state,
        Country country)
    {
        var atWar = state.Wars.Any(war =>
            war.Status == WarStatus.Active &&
            war.IsParticipant(country));

        var debtRatio = country.Gdp <= 0
            ? 0m
            : country.Debt / country.Gdp;

        if (atWar)
        {
            country.ArmyFunding = Math.Min(
                1.5m,
                country.ArmyFunding + 0.05m);

            if (country.ArmyReadiness < 55)
            {
                country.ArmyFunding = Math.Min(
                    1.5m,
                    country.ArmyFunding + 0.05m);
            }
        }
        else if (country.LastMonthlyBalance < 0 &&
                 country.ArmyReadiness >= 75 &&
                 country.ArmyFunding > 0.90m)
        {
            country.ArmyFunding = Math.Max(
                0.90m,
                country.ArmyFunding - 0.05m);
        }

        if (country.AdministrativeEfficiency < 0.68m)
        {
            country.AdministrationFunding = Math.Min(
                1.5m,
                country.AdministrationFunding + 0.05m);
        }
        else if (country.LastMonthlyBalance < 0 &&
                 country.AdministrativeEfficiency >= 0.82m &&
                 country.AdministrationFunding > 0.95m)
        {
            country.AdministrationFunding = Math.Max(
                0.95m,
                country.AdministrationFunding - 0.05m);
        }

        if (country.PublicUnrest >= 60)
        {
            country.TaxRate = Math.Max(
                0.04m,
                country.TaxRate - 0.005m);

            country.CourtFunding = Math.Min(
                1.5m,
                country.CourtFunding + 0.05m);
        }
        else if ((country.LastMonthlyBalance < 0 ||
                  debtRatio >= 0.18m) &&
                 country.TaxRate < 0.18m)
        {
            country.TaxRate = Math.Min(
                0.18m,
                country.TaxRate + 0.005m);
        }

        if (country.Government.Stability <= 40 &&
            country.CourtFunding < 1.20m)
        {
            country.CourtFunding = Math.Min(
                1.20m,
                country.CourtFunding + 0.05m);
        }
        else if (country.LastMonthlyBalance < 0 &&
                 country.Government.Stability >= 70 &&
                 country.CourtFunding > 0.90m)
        {
            country.CourtFunding = Math.Max(
                0.90m,
                country.CourtFunding - 0.05m);
        }
    }

    private static void AdaptAiTradeNetwork(GameState state)
    {
        var playerCountry = state.Player.Country;

        foreach (var relation in state.Diplomacy.All)
        {
            var first = state.FindCountry(relation.CountryAId);
            var second = state.FindCountry(relation.CountryBId);

            if (first is null ||
                second is null ||
                ReferenceEquals(first, playerCountry) ||
                ReferenceEquals(second, playerCountry))
            {
                continue;
            }

            var atWar = state.Wars.Any(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(first) &&
                war.IsParticipant(second));

            if (atWar)
            {
                relation.HasTradeAgreement = false;
                relation.TradeAgreementStartedOn = null;
                continue;
            }

            if (!relation.HasTradeAgreement &&
                relation.Relations >= 20 &&
                relation.Trust >= 45 &&
                relation.Tension <= 35)
            {
                relation.HasTradeAgreement = true;
                relation.TradeAgreementStartedOn = state.Date;
                relation.ChangeRelations(2);
                relation.ChangeTrust(2);
                continue;
            }

            if (relation.HasTradeAgreement &&
                (relation.Relations <= -40 ||
                 relation.Tension >= 70))
            {
                relation.HasTradeAgreement = false;
                relation.TradeAgreementStartedOn = null;
                relation.ChangeTrust(-4);
            }
        }
    }
}
