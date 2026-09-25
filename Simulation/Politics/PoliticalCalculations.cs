using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;

namespace LeaderGame.Simulation.Politics;

public static class PoliticalCalculations
{
    public static double GetOrderWillingness(
        GameState state,
        Country country,
        Character actor,
        Character issuer)
    {
        if (!actor.IsPoliticallyActive)
            return 0;

        if (ReferenceEquals(actor, issuer))
            return 100;

        var relationship = state.Relationships.GetOrCreate(actor, issuer);
        var normalisedOpinion = (relationship.Opinion + 100) / 2.0;
        var countryAllegiance =
            actor.GetAllegiance(PoliticalKeys.Country(country.Id));

        var oppositionPenalty = GetOppositionOrderPenalty(
            state,
            country,
            actor,
            issuer);

        var willingness =
            relationship.Trust * 0.30 +
            normalisedOpinion * 0.25 +
            relationship.Fear * 0.10 +
            countryAllegiance * 0.25 +
            (100 - actor.Ambition) * 0.10 -
            oppositionPenalty;

        return Math.Clamp(willingness, 0, 100);
    }

    private static double GetOppositionOrderPenalty(
        GameState state,
        Country country,
        Character actor,
        Character issuer)
    {
        if (!ReferenceEquals(issuer, country.Ruler))
            return 0;

        var bloc = state.PoliticalBlocs.FirstOrDefault(candidate =>
            candidate.IsActive &&
            ReferenceEquals(candidate.Country, country) &&
            (ReferenceEquals(candidate.Leader, actor) ||
             candidate.MemberIds.Contains(actor.Id)));

        if (bloc is null)
            return 0;

        if (ReferenceEquals(bloc.Leader, actor))
            return Math.Min(20, 10 + bloc.Cohesion / 10.0);

        return Math.Min(12, 5 + bloc.Cohesion / 20.0);
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

    public static double GetPowerBaseInfluence(
        Country country,
        Character character)
    {
        double weightedStanding = 0;
        double totalStrength = 0;

        foreach (var powerBase in Enum.GetValues<PowerBaseType>())
        {
            var strength = country.GetPowerBaseStrength(powerBase);

            if (strength <= 0)
                continue;

            weightedStanding +=
                strength * character.GetPowerBaseStanding(powerBase);
            totalStrength += strength;
        }

        return totalStrength <= 0
            ? 50
            : Math.Clamp(weightedStanding / totalStrength, 0, 100);
    }

    public static double GetThreatScore(
        GameState state,
        Country country,
        Character character)
    {
        if (!character.IsPoliticallyActive ||
            ReferenceEquals(character, country.Ruler))
        {
            return 0;
        }

        var willingness = GetOrderWillingness(
            state,
            country,
            character,
            country.Ruler);

        var countryAllegiance =
            character.GetAllegiance(PoliticalKeys.Country(country.Id));

        var powerBaseInfluence = GetPowerBaseInfluence(country, character);
        var blocBonus = GetBlocThreatBonus(state, country, character);

        var threat =
            character.Influence * 0.45 +
            character.Ambition * 0.35 +
            (100 - willingness) * 0.35 -
            countryAllegiance * 0.15 +
            (powerBaseInfluence - 50) * 0.20 +
            blocBonus;

        return Math.Clamp(threat, 0, 100);
    }

    public static double GetConspiracyAffinity(
        GameState state,
        Country country,
        Character supporter,
        Character instigator)
    {
        if (!supporter.IsPoliticallyActive ||
            !instigator.IsPoliticallyActive ||
            ReferenceEquals(supporter, country.Ruler) ||
            ReferenceEquals(supporter, instigator))
        {
            return 0;
        }

        var towardInstigator = state.Relationships.GetOrCreate(
            supporter,
            instigator);
        var normalisedOpinion = (towardInstigator.Opinion + 100) / 2.0;
        var rulerWillingness = GetOrderWillingness(
            state,
            country,
            supporter,
            country.Ruler);

        var blocBonus = GetBlocAffinityBonus(
            state,
            country,
            supporter,
            instigator);

        return Math.Clamp(
            towardInstigator.Trust * 0.40 +
            normalisedOpinion * 0.30 +
            (100 - rulerWillingness) * 0.30 +
            blocBonus,
            0,
            100);
    }

    private static double GetBlocThreatBonus(
        GameState state,
        Country country,
        Character character)
    {
        var bloc = state.PoliticalBlocs.FirstOrDefault(candidate =>
            candidate.IsActive &&
            ReferenceEquals(candidate.Country, country) &&
            ReferenceEquals(candidate.Leader, character));

        if (bloc is null)
            return 0;

        var structuralSupport = bloc.PowerBases.Sum(powerBase =>
            country.GetPowerBaseStrength(powerBase));

        return Math.Min(
            18,
            structuralSupport / 25.0 +
            bloc.MemberIds.Count * 1.5 +
            bloc.Cohesion / 20.0);
    }

    private static double GetBlocAffinityBonus(
        GameState state,
        Country country,
        Character supporter,
        Character instigator)
    {
        var bloc = state.PoliticalBlocs.FirstOrDefault(candidate =>
            candidate.IsActive &&
            ReferenceEquals(candidate.Country, country) &&
            ReferenceEquals(candidate.Leader, instigator));

        if (bloc is null || !bloc.MemberIds.Contains(supporter.Id))
            return 0;

        return 12 + bloc.Cohesion / 10.0;
    }

    public static int GetInfluenceTarget(Country country, Character character)
    {
        if (!character.IsPoliticallyActive)
        {
            return character.Status == PoliticalStatus.Imprisoned
                ? 5
                : 10;
        }

        if (ReferenceEquals(character, country.Ruler))
            return 100;

        var officeTarget = character.Position switch
        {
            Position.Marshal => 80,
            Position.Chancellor => 75,
            Position.Treasurer => 65,
            _ => 20 + character.Ambition / 5
        };

        var powerBaseInfluence = GetPowerBaseInfluence(country, character);
        var powerBaseModifier =
            (int)Math.Round((powerBaseInfluence - 50) * 0.40);

        return Math.Clamp(officeTarget + powerBaseModifier, 5, 95);
    }
}
