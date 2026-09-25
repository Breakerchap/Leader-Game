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

                if (character.Influence < target)
                    character.Influence = Math.Min(target, character.Influence + 2);
                else if (character.Influence > target)
                    character.Influence = Math.Max(target, character.Influence - 2);
            }
        }

        // Immediate fear fades. Opinion and trust are much stickier, but even a
        // bitter court gradually cools if nothing new happens.
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
