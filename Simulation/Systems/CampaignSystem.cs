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

        EnsureDynamicObjectives(state, campaign, reports);

        foreach (var objective in campaign.Objectives.Where(objective =>
                     !objective.IsCompleted).ToList())
        {
            if (!state.Player.IsInPower &&
                objective.Type != CampaignObjectiveType.RestorePoliticalControl)
            {
                objective.ProgressMonths = 0;
                continue;
            }

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

        AddPostRestorationObjectives(campaign, reports, state);

        if (state.Player.IsInPower &&
            campaign.Objectives.Count > 0 &&
            campaign.Objectives.All(objective => objective.IsCompleted))
        {
            state.Player.HasWon = true;
            state.Player.WinReason =
                $"{campaign.Title} has been completed. " +
                "Every current campaign objective has been secured while the lineage holds power.";

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.System,
                "Campaign victory",
                state.Player.WinReason));
        }

        return reports;
    }

    private static void EnsureDynamicObjectives(
        GameState state,
        CampaignState campaign,
        List<SimulationReport> reports)
    {
        if (state.Player.IsInPower)
            return;

        if (campaign.Objectives.Any(objective =>
                objective.Type ==
                    CampaignObjectiveType.RestorePoliticalControl &&
                !objective.IsCompleted))
        {
            return;
        }

        var objective = new CampaignObjective
        {
            Id =
                $"restore-control-{state.Date.Year:D4}-{state.Date.Month:D2}",
            Title = "Return to Power",
            Description =
                $"Restore {state.Player.Lineage.Name} to control of {state.Player.Country.Name}. " +
                "Losing office does not end the campaign, but permanent political irrelevance does.",
            Type = CampaignObjectiveType.RestorePoliticalControl,
            RequiredMonths = 1
        };

        campaign.Objectives.Add(objective);

        reports.Add(new SimulationReport(
            state.Date,
            ReportCategory.System,
            "Campaign objective changed: Return to Power",
            objective.Description));
    }

    private static void AddPostRestorationObjectives(
        CampaignState campaign,
        List<SimulationReport> reports,
        GameState state)
    {
        var completedRestorations = campaign.Objectives
            .Where(objective =>
                objective.Type ==
                    CampaignObjectiveType.RestorePoliticalControl &&
                objective.IsCompleted)
            .ToList();

        foreach (var restoration in completedRestorations)
        {
            var id = $"consolidate-{restoration.Id}";

            if (campaign.Objectives.Any(objective =>
                    objective.Id == id))
            {
                continue;
            }

            var objective = new CampaignObjective
            {
                Id = id,
                Title = "Consolidate the Restoration",
                Description =
                    "After returning to government, keep stability at or above 55 and " +
                    "overall lineage backing at or above 45 for three consecutive months.",
                Type = CampaignObjectiveType.ConsolidateRestoration,
                TargetValue = 55,
                SecondaryTargetValue = 45,
                RequiredMonths = 3
            };

            campaign.Objectives.Add(objective);

            reports.Add(new SimulationReport(
                state.Date,
                ReportCategory.System,
                "Campaign objective changed: Consolidate the Restoration",
                objective.Description));
        }
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

            CampaignObjectiveType.RegionalAuthority =>
                IsRegionalAuthoritySatisfied(
                    country,
                    objective),

            CampaignObjectiveType.MilitaryPreparedness =>
                country.ArmyReadiness >= objective.TargetValue &&
                (double)country.ArmyFunding >=
                    objective.SecondaryTargetValue,

            CampaignObjectiveType.RestorePoliticalControl =>
                state.Player.IsInPower,

            CampaignObjectiveType.ConsolidateRestoration =>
                state.Player.IsInPower &&
                country.Government.Stability >= objective.TargetValue &&
                PoliticalCalculations.GetPowerBaseInfluence(
                    country,
                    country.Ruler) >= objective.SecondaryTargetValue,

            _ => false
        };
    }

    private static bool IsRegionalAuthoritySatisfied(
        Countries.Country country,
        CampaignObjective objective)
    {
        if (country.Regions.Count == 0)
            return false;

        var totalWeight =
            country.Regions.Sum(region =>
                Math.Max(
                    0.01m,
                    region.PopulationShare));

        var weightedControl =
            country.Regions.Sum(region =>
                region.CrownControl *
                (double)Math.Max(
                    0.01m,
                    region.PopulationShare)) /
            (double)totalWeight;

        return
            weightedControl >= objective.TargetValue &&
            country.Regions.All(region =>
                region.Unrest <
                objective.SecondaryTargetValue);
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
