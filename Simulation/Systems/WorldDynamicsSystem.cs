using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Politics;

namespace LeaderGame.Simulation.Systems;

/// <summary>
/// Slow structural change beneath month-to-month politics. These changes are
/// intentionally gradual: player actions and crises should bend the trajectory,
/// not cause GDP or social structure to teleport.
/// </summary>
internal static class WorldDynamicsSystem
{
    public static void ProcessMonth(GameState state)
    {
        foreach (var country in state.Countries)
        {
            var atWar = state.Wars.Any(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(country));

            var tradeAgreements = state.Diplomacy
                .ForCountry(country)
                .Count(relation => relation.HasTradeAgreement);

            ApplyEconomicChange(
                country,
                atWar,
                tradeAgreements);

            ApplyPopulationChange(
                country,
                atWar);

            if (state.Date.Month % 3 == 0)
            {
                ApplyStructuralPoliticalChange(
                    country,
                    atWar,
                    tradeAgreements);
            }
        }
    }

    private static void ApplyEconomicChange(
        Country country,
        bool atWar,
        int tradeAgreements)
    {
        if (country.Gdp <= 0)
            return;

        var debtRatio = country.Debt / country.Gdp;

        var regionalProsperity =
            GetRegionalProsperity(country);

        var monthlyRate =
            0.0008 +
            (regionalProsperity - 55) * 0.000015 +
            ((double)country.AdministrativeEfficiency - 0.70) * 0.0030 +
            (country.Government.Stability - 50) * 0.000015 -
            country.PublicUnrest * 0.000012 +
            tradeAgreements * 0.00035 -
            (double)Math.Max(0m, debtRatio - 0.15m) * 0.0035;

        if (atWar)
        {
            monthlyRate -=
                0.0010 +
                country.WarExhaustion * 0.000012;
        }

        monthlyRate = Math.Clamp(
            monthlyRate,
            -0.0040,
            0.0035);

        country.Gdp *= 1m + (decimal)monthlyRate;
    }

    private static double GetRegionalProsperity(
        Country country)
    {
        if (country.Regions.Count == 0)
            return 55;

        var totalWeight =
            country.Regions.Sum(region =>
                Math.Max(0.01m, region.EconomicShare));

        if (totalWeight <= 0)
            return 55;

        return country.Regions.Sum(region =>
            region.Prosperity *
            (double)Math.Max(0.01m, region.EconomicShare)) /
            (double)totalWeight;
    }

    private static void ApplyPopulationChange(
        Country country,
        bool atWar)
    {
        if (country.Population <= 0)
            return;

        var monthlyRate =
            0.00055 +
            (country.Government.Stability - 50) * 0.000004 -
            country.PublicUnrest * 0.000003;

        if (atWar)
        {
            monthlyRate -=
                0.00055 +
                country.WarExhaustion * 0.000003;
        }

        monthlyRate = Math.Clamp(
            monthlyRate,
            -0.0015,
            0.0012);

        country.Population = Math.Max(
            1,
            (long)Math.Round(
                country.Population *
                (1.0 + monthlyRate)));
    }

    private static void ApplyStructuralPoliticalChange(
        Country country,
        bool atWar,
        int tradeAgreements)
    {
        var merchantChange = 0;

        if (tradeAgreements > 0 &&
            country.TaxRate <= 0.13m)
        {
            merchantChange++;
        }

        if (country.TaxRate >= 0.18m || atWar)
            merchantChange--;

        country.SetPowerBaseStrength(
            PowerBaseType.Merchants,
            country.GetPowerBaseStrength(PowerBaseType.Merchants) +
            merchantChange);

        var bureaucracyChange =
            country.AdministrationFunding >= 1.10m &&
            country.AdministrativeEfficiency >= 0.75m
                ? 1
                : country.AdministrationFunding <= 0.80m
                    ? -1
                    : 0;

        country.SetPowerBaseStrength(
            PowerBaseType.Bureaucracy,
            country.GetPowerBaseStrength(PowerBaseType.Bureaucracy) +
            bureaucracyChange);

        var militaryChange =
            atWar || country.ArmyFunding >= 1.20m
                ? 1
                : country.ArmyFunding <= 0.75m
                    ? -1
                    : 0;

        country.SetPowerBaseStrength(
            PowerBaseType.Military,
            country.GetPowerBaseStrength(PowerBaseType.Military) +
            militaryChange);

        var courtChange =
            country.CourtFunding >= 1.20m
                ? 1
                : country.CourtFunding <= 0.75m
                    ? -1
                    : 0;

        country.SetPowerBaseStrength(
            PowerBaseType.Aristocracy,
            country.GetPowerBaseStrength(PowerBaseType.Aristocracy) +
            courtChange);
        country.SetPowerBaseStrength(
            PowerBaseType.Clergy,
            country.GetPowerBaseStrength(PowerBaseType.Clergy) +
            courtChange);

        if (country.Government.Type == GovernmentType.Republic)
        {
            var partyChange =
                country.Government.Stability >= 65
                    ? 1
                    : country.Government.Stability <= 40
                        ? -1
                        : 0;

            country.SetPowerBaseStrength(
                PowerBaseType.Party,
                country.GetPowerBaseStrength(PowerBaseType.Party) +
                partyChange);
        }
        else
        {
            country.SetPowerBaseStrength(
                PowerBaseType.RoyalFamily,
                country.GetPowerBaseStrength(PowerBaseType.RoyalFamily) +
                courtChange);
        }
    }
}
