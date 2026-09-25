using LeaderGame.Simulation.Campaign;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation.Systems;

internal static class CampaignSystem
{
    public static IEnumerable<SimulationReport> ProcessMonth(GameState state)
    {
        var campaign = state.Campaign;

        if (campaign is null ||
            state.Player.HasLost ||
            state.Player.HasWon)
        {
            return [];
        }

        var reports = new List<SimulationReport>();
        var country = state.Player.Country;

        foreach (var objective in campaign.Objectives.Where(objective =>
                     !objective.IsCompleted))
        {
            var satisfied = IsSatisfied(state, country, objective);

            objective.ProgressMonths = satisfied
                ? objective.ProgressMonths + 1
                : 0;

            if (objective.ProgressMonths < Math.Max(1, objective.RequiredMonths))
                continue;

            objective.IsCompleted = true;
            objective.CompletedOn = state.Date;

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.System,
                $"Objective completed: {objective.Title}",
                objective.Description));
        }

        if (campaign.Objectives.Count > 0 &&
            campaign.Objectives.All(objective => objective.IsCompleted))
        {
            state.Player.HasWon = true;
            state.Player.WinReason =
                $"{campaign.Title} has been completed. " +
                "Every campaign objective has been secured.";

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.System,
                "Campaign victory",
                state.Player.WinReason));
        }

        return reports;
    }

    private static bool IsSatisfied(
        GameState state,
        Countries.Country country,
        CampaignObjective objective)
    {
        return objective.Type switch
        {
            CampaignObjectiveType.StableGovernment =>
                country.Government.Stability >= objective.TargetValue &&
                PoliticalCalculations.GetPowerBaseInfluence(
                    country,
                    country.Ruler) >= objective.SecondaryTargetValue,

            CampaignObjectiveType.SolventTreasury =>
                (double)country.Treasury >= objective.TargetValue &&
                (double)country.LastMonthlyBalance >=
                objective.SecondaryTargetValue,

            CampaignObjectiveType.TradeNetwork =>
                state.Diplomacy.ForCountry(country)
                    .Count(relation => relation.HasTradeAgreement) >=
                objective.TargetValue,

            CampaignObjectiveType.DiplomaticStanding =>
                IsDiplomaticStandingSatisfied(
                    state,
                    country,
                    objective),

            CampaignObjectiveType.PeacefulRealm =>
                !state.Wars.Any(war =>
                    war.Status == WarStatus.Active &&
                    war.IsParticipant(country)) &&
                country.PublicUnrest <= objective.TargetValue,

            CampaignObjectiveType.ElectoralMandate =>
                state.Player.ElectionsWon >= objective.TargetValue,

            _ => false
        };
    }

    private static bool IsDiplomaticStandingSatisfied(
        GameState state,
        Countries.Country country,
        CampaignObjective objective)
    {
        if (string.IsNullOrWhiteSpace(objective.RelatedCountryId))
            return false;

        var other = state.FindCountry(objective.RelatedCountryId);

        if (other is null || ReferenceEquals(other, country))
            return false;

        return state.Diplomacy
            .GetOrCreate(country, other)
            .Relations >= objective.TargetValue;
    }
}
