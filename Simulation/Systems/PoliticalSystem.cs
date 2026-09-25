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

    private static int DriftTowards(int current, int target, int amount)
    {
        if (current < target)
            return Math.Min(target, current + amount);

        if (current > target)
            return Math.Max(target, current - amount);

        return current;
    }
}
