namespace LeaderGame.Simulation.Systems;

internal static class DiplomacySystem
{
    public static void ProcessMonth(GameState state)
    {
        foreach (var relation in state.Diplomacy.All)
        {
            if (relation.HasTradeAgreement)
            {
                if (state.Date.Month % 3 == 0)
                    relation.ChangeTension(-1);

                if (state.Date.Month % 6 == 0)
                {
                    relation.ChangeRelations(1);
                    relation.ChangeTrust(1);
                }

                continue;
            }

            if (state.Date.Month % 3 == 0 &&
                relation.Tension >= 70 &&
                relation.Relations < 0)
            {
                relation.ChangeRelations(-1);
                relation.ChangeTrust(-1);
                continue;
            }

            if (state.Date.Month % 6 == 0 &&
                relation.Tension > 20)
            {
                relation.ChangeTension(-1);
            }
        }
    }
}
