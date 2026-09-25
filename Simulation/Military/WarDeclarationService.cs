using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;

namespace LeaderGame.Simulation.Military;

public static class WarDeclarationService
{
    public static War Declare(
        GameState state,
        Country source,
        Country target)
    {
        if (ReferenceEquals(source, target))
            throw new ArgumentException("A country cannot declare war on itself.");

        if (state.Wars.Any(war =>
                war.Status == WarStatus.Active &&
                war.IsParticipant(source) &&
                war.IsParticipant(target)))
        {
            throw new InvalidOperationException(
                $"{source.Name} and {target.Name} are already at war.");
        }

        var relation = state.Diplomacy.GetOrCreate(source, target);
        var preWarRelations = relation.Relations;

        relation.HasTradeAgreement = false;
        relation.TradeAgreementStartedOn = null;
        relation.Relations = Math.Min(-70, relation.Relations - 30);
        relation.Trust = Math.Max(0, relation.Trust - 40);
        relation.Tension = 100;

        foreach (var proposal in state.DiplomaticProposals.Where(proposal =>
                     proposal.Status == DiplomaticProposalStatus.Pending &&
                     ((ReferenceEquals(proposal.SourceCountry, source) &&
                       ReferenceEquals(proposal.TargetCountry, target)) ||
                      (ReferenceEquals(proposal.SourceCountry, target) &&
                       ReferenceEquals(proposal.TargetCountry, source)))))
        {
            proposal.Status = DiplomaticProposalStatus.Withdrawn;
        }

        ApplyDomesticPoliticalCost(source, preWarRelations);

        var targetToSource = state.Relationships.GetOrCreate(
            target.Ruler,
            source.Ruler);
        targetToSource.ChangeOpinion(-35);
        targetToSource.Trust = 0;
        targetToSource.ChangeFear(10);

        var sourceToTarget = state.Relationships.GetOrCreate(
            source.Ruler,
            target.Ruler);
        sourceToTarget.ChangeOpinion(-20);
        sourceToTarget.ChangeTrust(-20);

        var war = new War
        {
            Attacker = source,
            Defender = target,
            StartedOn = state.Date
        };

        state.Wars.Add(war);
        return war;
    }

    private static void ApplyDomesticPoliticalCost(
        Country country,
        int previousRelations)
    {
        if (previousRelations >= 25)
        {
            country.Ruler.Legitimacy -= 5;
            country.Government.Stability -= 4;
            country.PublicUnrest += 4;
        }
        else if (previousRelations >= 0)
        {
            country.Ruler.Legitimacy -= 3;
            country.Government.Stability -= 2;
            country.PublicUnrest += 2;
        }
        else
        {
            country.Government.Stability -= 1;
            country.PublicUnrest += 1;
        }
    }
}
