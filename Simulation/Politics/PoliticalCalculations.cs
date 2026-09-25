using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Politics;

public static class PoliticalCalculations
{
    /// <summary>
    /// How willing a character is to carry out this issuer's orders, from 0 to 100.
    /// This intentionally separates affection, trust, fear, patriotism and ambition.
    /// </summary>
    public static double GetOrderWillingness(
        GameState state,
        Country country,
        Character actor,
        Character issuer)
    {
        if (ReferenceEquals(actor, issuer))
            return 100;

        var relationship = state.Relationships.GetOrCreate(actor, issuer);
        var normalisedOpinion = (relationship.Opinion + 100) / 2.0;
        var countryAllegiance =
            actor.GetAllegiance(PoliticalKeys.Country(country.Id));

        var willingness =
            relationship.Trust * 0.30 +
            normalisedOpinion * 0.25 +
            relationship.Fear * 0.10 +
            countryAllegiance * 0.25 +
            (100 - actor.Ambition) * 0.10;

        return Math.Clamp(willingness, 0, 100);
    }

    public static decimal GetImplementationFactor(
        Character actor,
        double willingness)
    {
        var competence = Math.Clamp(actor.Competence, 0, 100) / 100m;
        var willingnessFraction = (decimal)Math.Clamp(willingness, 0, 100) / 100m;

        return Math.Clamp(
            0.35m +
            competence * 0.35m +
            willingnessFraction * 0.30m,
            0.35m,
            1m);
    }

    /// <summary>
    /// Rough measure of how dangerous this person is to the current ruler.
    /// It is not a coup probability: it is a pressure score used by political systems.
    /// </summary>
    public static double GetThreatScore(
        GameState state,
        Country country,
        Character character)
    {
        if (!character.IsAlive || ReferenceEquals(character, country.Ruler))
            return 0;

        var willingness = GetOrderWillingness(
            state,
            country,
            character,
            country.Ruler);

        var countryAllegiance =
            character.GetAllegiance(PoliticalKeys.Country(country.Id));

        var threat =
            character.Influence * 0.45 +
            character.Ambition * 0.35 +
            (100 - willingness) * 0.35 -
            countryAllegiance * 0.15;

        return Math.Clamp(threat, 0, 100);
    }

    public static int GetInfluenceTarget(Country country, Character character)
    {
        if (ReferenceEquals(character, country.Ruler))
            return 100;

        return character.Position switch
        {
            Position.Marshal => 80,
            Position.Chancellor => 75,
            Position.Treasurer => 65,
            _ => 20 + character.Ambition / 5
        };
    }
}
