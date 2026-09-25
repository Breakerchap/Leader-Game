using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Politics;

namespace LeaderGame.Simulation.Systems;

internal static class PoliticalSystem
{
    public static void ProcessMonth(GameState state)
    {
        foreach (var country in state.Countries)
        {
            foreach (var character in country.PoliticalFigures.Where(character => character.IsAlive))
            {
                var target = PoliticalCalculations.GetInfluenceTarget(country, character);
                var step = character.IsPoliticallyActive ? 2 : 4;

                if (character.Influence < target)
                    character.Influence = Math.Min(target, character.Influence + step);
                else if (character.Influence > target)
                    character.Influence = Math.Max(target, character.Influence - step);
            }

            if (country.Ruler.IsAlive)
            {
                if (state.Date.Month % 3 == 0)
                    ApplyConditionDrivenBacking(country);

                var rulerBacking =
                    PoliticalCalculations.GetPowerBaseInfluence(country, country.Ruler);

                // Institutional and social support changes regime resilience slowly.
                // It should matter over time without replacing acute shocks from
                // debt, war, repression or unrest.
                country.Government.Stability +=
                    Math.Clamp((rulerBacking - 50) / 125.0, -0.4, 0.4);
            }
        }

        foreach (var relationship in state.Relationships.All)
        {
            if (relationship.Fear > 0)
                relationship.Fear--;

            if (state.Date.Month % 3 == 0)
            {
                relationship.Opinion = DriftTowards(relationship.Opinion, 0, 1);
                relationship.Trust = DriftTowards(relationship.Trust, 50, 1);
            }
        }
    }

    private static void ApplyConditionDrivenBacking(Country country)
    {
        var ruler = country.Ruler;

        ApplyFundingReaction(
            ruler,
            PowerBaseType.Military,
            country.ArmyFunding);

        ApplyFundingReaction(
            ruler,
            PowerBaseType.Bureaucracy,
            country.AdministrationFunding);

        var courtReaction = FundingReaction(country.CourtFunding);
        ruler.ChangePowerBaseStanding(PowerBaseType.Aristocracy, courtReaction);
        ruler.ChangePowerBaseStanding(PowerBaseType.Clergy, courtReaction);
        ruler.ChangePowerBaseStanding(PowerBaseType.RegionalElites, courtReaction);
        ruler.ChangePowerBaseStanding(PowerBaseType.RoyalFamily, courtReaction);

        if (country.TaxRate >= 0.16m)
        {
            ruler.ChangePowerBaseStanding(PowerBaseType.Merchants, -2);
            ruler.ChangePowerBaseStanding(PowerBaseType.Workers, -2);
            ruler.ChangePowerBaseStanding(PowerBaseType.Peasantry, -2);
        }
        else if (country.TaxRate <= 0.07m)
        {
            ruler.ChangePowerBaseStanding(PowerBaseType.Merchants, 1);
            ruler.ChangePowerBaseStanding(PowerBaseType.Workers, 1);
            ruler.ChangePowerBaseStanding(PowerBaseType.Peasantry, 1);
        }

        if (country.WarExhaustion >= 50)
        {
            ruler.ChangePowerBaseStanding(PowerBaseType.Military, -2);
            ruler.ChangePowerBaseStanding(PowerBaseType.Workers, -1);
            ruler.ChangePowerBaseStanding(PowerBaseType.Peasantry, -1);
        }

        if (country.PublicUnrest >= 60)
        {
            ruler.ChangePowerBaseStanding(PowerBaseType.Merchants, -1);
            ruler.ChangePowerBaseStanding(PowerBaseType.Bureaucracy, -1);
            ruler.ChangePowerBaseStanding(PowerBaseType.Party, -2);
            ruler.ChangePowerBaseStanding(PowerBaseType.RegionalElites, -1);
        }
    }

    private static void ApplyFundingReaction(
        Characters.Character ruler,
        PowerBaseType powerBase,
        decimal funding)
    {
        ruler.ChangePowerBaseStanding(powerBase, FundingReaction(funding));
    }

    private static int FundingReaction(decimal funding)
    {
        if (funding <= 0.75m)
            return -2;

        if (funding < 0.95m)
            return -1;

        if (funding >= 1.25m)
            return 2;

        if (funding > 1.05m)
            return 1;

        return 0;
    }

    private static int DriftTowards(int current, int target, int amount)
    {
        if (current < target)
            return Math.Min(target, current + amount);

        if (current > target)
            return Math.Max(target, current - amount);

        return current;
    }
}
