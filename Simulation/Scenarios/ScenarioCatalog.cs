using LeaderGame.Simulation.Campaign;
using LeaderGame.Simulation.Player;

namespace LeaderGame.Simulation.Scenarios;

public sealed record ScenarioDefinition(
    string Id,
    string Name,
    string CountryName,
    string LeaderName,
    string LineageName,
    PoliticalLineageType LineageType,
    string Summary,
    string StrategicProblem);

public static class ScenarioCatalog
{
    public const string FalkenreichId = "falkenreich";
    public const string NordmarkId = "nordmark";
    public const string ValeriaId = "valeria";

    public static IReadOnlyList<ScenarioDefinition> All { get; } =
    [
        new(
            FalkenreichId,
            "The Falken Crown",
            "Falkenreich",
            "Friedrich von Falken",
            "House von Falken",
            PoliticalLineageType.Dynasty,
            "A wealthy but politically brittle feudal monarchy. The crown still matters, " +
            "but the army, aristocracy and regional elites each have enough weight to make disobedience dangerous.",
            "Secure the dynasty without bankrupting the realm or allowing court opposition to become an alternative government."),

        new(
            NordmarkId,
            "The Northern Crown",
            "Nordmark",
            "Ingrid af Skeld",
            "House af Skeld",
            PoliticalLineageType.Dynasty,
            "A comparatively orderly northern monarchy with a respectable crown, a capable administrative core and powerful local interests beyond the royal coast.",
            "Turn a stable inheritance into durable state power: prepare for war without living under permanent mobilisation, extend authority into the outer regions and keep the crown politically credible."),

        new(
            ValeriaId,
            "The Valerian Republic",
            "Valeria",
            "Marco Vieri",
            "Vieri Coalition",
            PoliticalLineageType.Party,
            "An oligarchic merchant republic with a strong chancery, powerful commercial families and a chief magistrate chosen within the governing council.",
            "Turn commercial strength into durable influence inside the Great Council while keeping rival patrician networks, neighbours and your own coalition from unravelling.")
    ];

    public static ScenarioDefinition Get(string id) =>
        All.FirstOrDefault(scenario =>
            string.Equals(
                scenario.Id,
                id,
                StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException(
            $"Unknown scenario '{id}'.",
            nameof(id));

    public static CampaignState CreateCampaign(
        string scenarioId,
        GameDate startDate)
    {
        var definition = Get(scenarioId);

        var campaign = new CampaignState
        {
            ScenarioId = definition.Id,
            Title = definition.Name,
            Summary = definition.Summary,
            StartedOn = startDate
        };

        switch (definition.Id)
        {
            case FalkenreichId:
                campaign.Objectives.AddRange(
                [
                    new CampaignObjective
                    {
                        Id = "secure-crown",
                        Title = "Secure the Crown",
                        Description =
                            "Keep government stability at or above 70 and overall ruler backing at or above 60 for six consecutive months.",
                        Type = CampaignObjectiveType.StableGovernment,
                        TargetValue = 70,
                        SecondaryTargetValue = 60,
                        RequiredMonths = 6
                    },
                    new CampaignObjective
                    {
                        Id = "sound-finances",
                        Title = "Sound Finances",
                        Description =
                            "Build the treasury to at least 1,000,000 while maintaining a non-negative monthly balance for three consecutive months.",
                        Type = CampaignObjectiveType.SolventTreasury,
                        TargetValue = 1_000_000,
                        SecondaryTargetValue = 0,
                        RequiredMonths = 3
                    },
                    new CampaignObjective
                    {
                        Id = "realm-at-peace",
                        Title = "Peace of the Realm",
                        Description =
                            "Remain out of active war with public unrest at or below 30 for six consecutive months.",
                        Type = CampaignObjectiveType.PeacefulRealm,
                        TargetValue = 30,
                        RequiredMonths = 6
                    }
                ]);
                break;

            case NordmarkId:
                campaign.Objectives.AddRange(
                [
                    new CampaignObjective
                    {
                        Id = "northern-readiness",
                        Title = "A Crown Ready for War",
                        Description =
                            "Keep army readiness at or above 85 while funding the army at 110% or more for three consecutive months.",
                        Type = CampaignObjectiveType.MilitaryPreparedness,
                        TargetValue = 85,
                        SecondaryTargetValue = 1.10,
                        RequiredMonths = 3
                    },
                    new CampaignObjective
                    {
                        Id = "bind-the-realm",
                        Title = "Bind the Realm",
                        Description =
                            "Raise population-weighted central control across Nordmark to at least 60 while keeping every region below 45 unrest for four consecutive months.",
                        Type = CampaignObjectiveType.RegionalAuthority,
                        TargetValue = 60,
                        SecondaryTargetValue = 45,
                        RequiredMonths = 4
                    },
                    new CampaignObjective
                    {
                        Id = "trusted-crown",
                        Title = "A Trusted Crown",
                        Description =
                            "Keep government stability at or above 75 and overall ruler backing at or above 60 for four consecutive months.",
                        Type = CampaignObjectiveType.StableGovernment,
                        TargetValue = 75,
                        SecondaryTargetValue = 60,
                        RequiredMonths = 4
                    },
                    new CampaignObjective
                    {
                        Id = "northern-commerce",
                        Title = "Open the Northern Markets",
                        Description =
                            "Maintain at least one active trade agreement with a neighbouring state.",
                        Type = CampaignObjectiveType.TradeNetwork,
                        TargetValue = 1,
                        RequiredMonths = 1
                    }
                ]);
                break;

            case ValeriaId:
                campaign.Objectives.AddRange(
                [
                    new CampaignObjective
                    {
                        Id = "merchant-network",
                        Title = "Merchant Network",
                        Description =
                            "Maintain trade agreements with both neighbouring states.",
                        Type = CampaignObjectiveType.TradeNetwork,
                        TargetValue = 2
                    },
                    new CampaignObjective
                    {
                        Id = "commercial-reserves",
                        Title = "Commercial Reserves",
                        Description =
                            "Build the treasury to at least 2,000,000 while maintaining a non-negative monthly balance for three consecutive months.",
                        Type = CampaignObjectiveType.SolventTreasury,
                        TargetValue = 2_000_000,
                        SecondaryTargetValue = 0,
                        RequiredMonths = 3
                    },
                    new CampaignObjective
                    {
                        Id = "normalise-falkenreich",
                        Title = "Normalise Falkenreich",
                        Description =
                            "Keep formal relations with Falkenreich at +10 or better for three consecutive months.",
                        Type = CampaignObjectiveType.DiplomaticStanding,
                        TargetValue = 10,
                        RelatedCountryId = FalkenreichId,
                        RequiredMonths = 3
                    },
                    new CampaignObjective
                    {
                        Id = "renew-mandate",
                        Title = "Renew the Mandate",
                        Description =
                            "Win a scheduled Great Council election while keeping the Vieri Coalition in power.",
                        Type = CampaignObjectiveType.ElectoralMandate,
                        TargetValue = 1
                    }
                ]);
                break;
        }

        return campaign;
    }
}
