namespace LeaderGame.Simulation.Systems;

internal static class DiplomacySystem
{
    public static void ProcessMonth(GameState state)
    {
        foreach (var relation in state.Diplomacy.All)
        {
            var first = state.FindCountry(relation.CountryAId);
            var second = state.FindCountry(relation.CountryBId);

            if (first is null || second is null)
                continue;

            if (relation.HasTradeAgreement)
            {
                if (state.Date.Month % 3 == 0)
                    relation.ChangeTension(-1);

                if (state.Date.Month % 6 == 0)
                {
                    relation.ChangeRelations(1);
                    relation.ChangeTrust(1);
                }
            }
            else if (state.Date.Month % 3 == 0 &&
                     relation.Tension >= 70 &&
                     relation.Relations < 0)
            {
                relation.ChangeRelations(-1);
                relation.ChangeTrust(-1);
            }
            else if (state.Date.Month % 6 == 0 &&
                     relation.Tension > 20)
            {
                relation.ChangeTension(-1);
            }

            if (state.Date.Month % 6 != 0)
                continue;

            var firstToSecond =
                state.Relationships.GetOrCreate(first.Ruler, second.Ruler);
            var secondToFirst =
                state.Relationships.GetOrCreate(second.Ruler, first.Ruler);

            var averagePersonalOpinion =
                (firstToSecond.Opinion + secondToFirst.Opinion) / 2.0;
            var averagePersonalTrust =
                (firstToSecond.Trust + secondToFirst.Trust) / 2.0;

            if (averagePersonalOpinion >= 50 && averagePersonalTrust >= 60)
                relation.ChangeRelations(1);
            else if (averagePersonalOpinion <= -50 || averagePersonalTrust <= 20)
                relation.ChangeRelations(-1);
        }
    }
}
