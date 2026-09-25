using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Military;

namespace LeaderGame.Simulation.Systems;

internal static class MilitaryAiSystem
{
    public static void ProcessMonth(GameState state)
    {
        foreach (var war in state.Wars.Where(war => war.Status == WarStatus.Active))
        {
            ChooseStance(state, war, war.Attacker);
            ChooseStance(state, war, war.Defender);
        }
    }

    private static void ChooseStance(
        GameState state,
        War war,
        Country country)
    {
        if (ReferenceEquals(country, state.Player.Country))
            return;

        var opponent = war.OpponentOf(country);
        var score = ReferenceEquals(country, war.Attacker)
            ? war.WarScore
            : -war.WarScore;

        var stance =
            country.WarExhaustion >= 60 ||
            country.ArmyReadiness <= 30
                ? WarStance.Defensive
                : score >= 35 &&
                  country.ArmyReadiness >= 50 &&
                  country.WarExhaustion < 45
                    ? WarStance.Aggressive
                    : country.ArmySize >= opponent.ArmySize * 1.30 &&
                      country.ArmyReadiness >= 55 &&
                      country.WarExhaustion < 40
                        ? WarStance.Aggressive
                        : score <= -35 &&
                          (country.ArmyReadiness < 60 ||
                           country.WarExhaustion >= 35)
                            ? WarStance.Defensive
                            : WarStance.Balanced;

        war.SetStance(country, stance);
    }
}
