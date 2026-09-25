using LeaderGame.Simulation.Campaign;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Randomness;

namespace LeaderGame.Simulation.Persistence;

public static partial class GameSaveService
{
    private static GameSaveSnapshot Capture(GameState state)
    {
        if (state.Random is not SimulationRandom random)
        {
            throw new InvalidOperationException(
                "Saving requires the main simulation RNG to be SimulationRandom.");
        }

        if (state.InformationRandom is not SimulationRandom informationRandom)
        {
            throw new InvalidOperationException(
                "Saving requires the information RNG to be SimulationRandom.");
        }

        var characters = state.Countries
            .SelectMany(country => country.PoliticalFigures)
            .Concat(state.Player.Lineage.Members)
            .Append(state.Player.CurrentCharacter)
            .DistinctBy(character => character.Id)
            .OrderBy(character => character.Id)
            .ToList();

        return new GameSaveSnapshot
        {
            Date = Date(state.Date),
            RandomState = random.State,
            InformationRandomState = informationRandom.State,
            Player = new PlayerSnapshot
            {
                CurrentCharacterId = state.Player.CurrentCharacter.Id,
                CountryId = state.Player.Country.Id,
                LineageId = state.Player.Lineage.Id,
                LineageName = state.Player.Lineage.Name,
                LineageType = state.Player.Lineage.Type,
                LineageMemberIds = state.Player.Lineage.Members
                    .Select(character => character.Id)
                    .ToList(),
                HasLost = state.Player.HasLost,
                LossReason = state.Player.LossReason,
                HasWon = state.Player.HasWon,
                WinReason = state.Player.WinReason
            },
            Campaign = state.Campaign is null
                ? null
                : new CampaignSnapshot
                {
                    ScenarioId = state.Campaign.ScenarioId,
                    Title = state.Campaign.Title,
                    Summary = state.Campaign.Summary,
                    StartedOn = Date(state.Campaign.StartedOn),
                    Objectives = state.Campaign.Objectives
                        .Select(objective => new CampaignObjectiveSnapshot
                        {
                            Id = objective.Id,
                            Title = objective.Title,
                            Description = objective.Description,
                            Type = objective.Type,
                            TargetValue = objective.TargetValue,
                            SecondaryTargetValue = objective.SecondaryTargetValue,
                            RelatedCountryId = objective.RelatedCountryId,
                            RequiredMonths = objective.RequiredMonths,
                            ProgressMonths = objective.ProgressMonths,
                            IsCompleted = objective.IsCompleted,
                            CompletedOn = objective.CompletedOn is { } completed
                                ? Date(completed)
                                : null
                        })
                        .ToList()
                },
            Characters = characters.Select(CaptureCharacter).ToList(),
            Countries = state.Countries.Select(CaptureCountry).ToList(),
            Relationships = state.Relationships.Entries
                .Select(entry => new RelationshipSnapshot(
                    entry.FromId,
                    entry.ToId,
                    entry.Relationship.Opinion,
                    entry.Relationship.Trust,
                    entry.Relationship.Fear))
                .ToList(),
            Diplomacy = state.Diplomacy.All
                .Select(CaptureDiplomaticRelation)
                .ToList(),
            Wars = state.Wars.Select(CaptureWar).ToList(),
            Plots = state.Plots.Select(CapturePlot).ToList(),
            PowerBaseDemands = state.PowerBaseDemands
                .Select(demand => new PowerBaseDemandSnapshot
                {
                    CountryId = demand.Country.Id,
                    PowerBase = demand.PowerBase,
                    Type = demand.Type,
                    TargetValue = demand.TargetValue,
                    MonthsOpen = demand.MonthsOpen,
                    EscalationLevel = demand.EscalationLevel,
                    IsResolved = demand.IsResolved
                })
                .ToList(),
            PoliticalBlocs = state.PoliticalBlocs
                .Select(bloc => new PoliticalBlocSnapshot
                {
                    Id = bloc.Id,
                    CountryId = bloc.Country.Id,
                    LeaderId = bloc.Leader.Id,
                    PowerBases = bloc.PowerBases.ToList(),
                    MemberIds = bloc.MemberIds.ToList(),
                    MonthsActive = bloc.MonthsActive,
                    Cohesion = bloc.Cohesion,
                    IsActive = bloc.IsActive
                })
                .ToList(),
            CabinetProposals = state.CabinetProposals
                .Select(proposal => new CabinetProposalSnapshot
                {
                    Id = proposal.Id,
                    CountryId = proposal.Country.Id,
                    AdvisorId = proposal.Advisor.Id,
                    Type = proposal.Type,
                    TargetCountryId = proposal.TargetCountry?.Id,
                    WarId = proposal.War?.Id,
                    TargetValue = proposal.TargetValue,
                    CreatedOn = Date(proposal.CreatedOn),
                    MonthsOpen = proposal.MonthsOpen,
                    Status = proposal.Status
                })
                .ToList(),
            DiplomaticProposals = state.DiplomaticProposals
                .Select(CaptureProposal)
                .ToList(),
            Reports = state.Reports
                .Select(report => new SimulationReportSnapshot(
                    Date(report.Date),
                    report.Category,
                    report.Title,
                    report.Details))
                .ToList(),
            Knowledge = state.Knowledge.Latest
                .Select(CaptureKnownInformation)
                .ToList(),
            AdvisorReports = state.AdvisorReports
                .Select(CaptureAdvisorReport)
                .ToList(),
            InformationRequests = state.InformationRequests
                .Select(request => new InformationRequestSnapshot
                {
                    Id = request.Id,
                    Topic = request.Topic,
                    AdvisorId = request.Advisor.Id,
                    SubjectCountryId = request.SubjectCountry.Id,
                    RelatedCountryId = request.RelatedCountry?.Id,
                    RequestedOn = Date(request.RequestedOn),
                    RemainingMonths = request.RemainingMonths,
                    WillingnessAtRequest = request.WillingnessAtRequest,
                    Status = request.Status
                })
                .ToList(),
            InformationHistory = state.InformationHistory
                .Select(history => new TruthHistorySnapshot
                {
                    Date = Date(history.Date),
                    Values = history.Values.Select(entry => new TruthValueSnapshot
                    {
                        Metric = entry.Key.Metric,
                        SubjectCountryId = entry.Key.SubjectCountryId,
                        RelatedCountryId = entry.Key.RelatedCountryId,
                        Value = entry.Value
                    }).ToList()
                })
                .ToList(),
            PendingOrders = state.PendingOrders.Select(CaptureOrder).ToList()
        };
    }

    private static CharacterSnapshot CaptureCharacter(Character character)
    {
        return new CharacterSnapshot
        {
            Id = character.Id,
            FirstName = character.FirstName,
            LastName = character.LastName,
            Age = character.Age,
            Health = character.Health,
            Competence = character.Competence,
            Ambition = character.Ambition,
            Legitimacy = character.Legitimacy,
            Influence = character.Influence,
            Position = character.Position,
            Status = character.Status,
            IsAlive = character.IsAlive,
            Allegiances = character.Allegiances.ToDictionary(
                entry => entry.Key,
                entry => entry.Value,
                StringComparer.Ordinal),
            PowerBaseStanding = character.PowerBaseStanding
                .Select(entry => new PowerBaseValueSnapshot(
                    entry.Key,
                    entry.Value))
                .ToList()
        };
    }

    private static CountrySnapshot CaptureCountry(Country country)
    {
        return new CountrySnapshot
        {
            Id = country.Id,
            Name = country.Name,
            Population = country.Population,
            Gdp = country.Gdp,
            Treasury = country.Treasury,
            Debt = country.Debt,
            ArmySize = country.ArmySize,
            ArmyReadiness = country.ArmyReadiness,
            TaxRate = country.TaxRate,
            AdministrativeEfficiency = country.AdministrativeEfficiency,
            ArmyFunding = country.ArmyFunding,
            AdministrationFunding = country.AdministrationFunding,
            CourtFunding = country.CourtFunding,
            WarExhaustion = country.WarExhaustion,
            PublicUnrest = country.PublicUnrest,
            LastMonthlyTaxRevenue = country.LastMonthlyTaxRevenue,
            LastMonthlyTradeIncome = country.LastMonthlyTradeIncome,
            LastMonthlyExpenses = country.LastMonthlyExpenses,
            LastMonthlyDebtInterest = country.LastMonthlyDebtInterest,
            LastMonthlyBalance = country.LastMonthlyBalance,
            GovernmentType = country.Government.Type,
            GovernmentStability = country.Government.Stability,
            RulerId = country.Ruler.Id,
            NeighborIds = country.NeighborIds.ToList(),
            SuccessionOrderIds = country.SuccessionOrder
                .Select(character => character.Id)
                .ToList(),
            PoliticalFigureIds = country.PoliticalFigures
                .Select(character => character.Id)
                .ToList(),
            PowerBaseStrengths = country.PowerBaseStrengths
                .Select(entry => new PowerBaseValueSnapshot(
                    entry.Key,
                    entry.Value))
                .ToList()
        };
    }

    private static DiplomaticRelationSnapshot CaptureDiplomaticRelation(
        DiplomaticRelation relation)
    {
        return new DiplomaticRelationSnapshot
        {
            CountryAId = relation.CountryAId,
            CountryBId = relation.CountryBId,
            Relations = relation.Relations,
            Trust = relation.Trust,
            Tension = relation.Tension,
            HasTradeAgreement = relation.HasTradeAgreement,
            TradeAgreementStartedOn = relation.TradeAgreementStartedOn is { } started
                ? Date(started)
                : null
        };
    }

    private static WarSnapshot CaptureWar(War war)
    {
        return new WarSnapshot
        {
            Id = war.Id,
            AttackerId = war.Attacker.Id,
            DefenderId = war.Defender.Id,
            StartedOn = Date(war.StartedOn),
            WarScore = war.WarScore,
            MonthsActive = war.MonthsActive,
            AttackerStance = war.AttackerStance,
            DefenderStance = war.DefenderStance,
            Status = war.Status
        };
    }

    private static PoliticalPlotSnapshot CapturePlot(
        PoliticalPlot plot)
    {
        return new PoliticalPlotSnapshot
        {
            Id = plot.Id,
            Type = plot.Type,
            CountryId = plot.Country.Id,
            InstigatorId = plot.Instigator.Id,
            Progress = plot.Progress,
            SupporterIds = plot.SupporterIds.ToList(),
            DiscoveryStage = plot.DiscoveryStage,
            IsResolved = plot.IsResolved,
            Succeeded = plot.Succeeded
        };
    }

    private static DiplomaticProposalSnapshot CaptureProposal(
        DiplomaticProposal proposal)
    {
        return new DiplomaticProposalSnapshot
        {
            Id = proposal.Id,
            Type = proposal.Type,
            SourceCountryId = proposal.SourceCountry.Id,
            TargetCountryId = proposal.TargetCountry.Id,
            CreatedOn = Date(proposal.CreatedOn),
            DemandedPayment = proposal.DemandedPayment,
            MonthsOpen = proposal.MonthsOpen,
            Status = proposal.Status
        };
    }

    private static KnownInformationSnapshot CaptureKnownInformation(
        KnownInformation information)
    {
        return new KnownInformationSnapshot
        {
            Metric = information.Key.Metric,
            SubjectCountryId = information.Key.SubjectCountryId,
            RelatedCountryId = information.Key.RelatedCountryId,
            Estimate = information.Estimate,
            Margin = information.Margin,
            ReportedConfidence = information.ReportedConfidence,
            AsOf = Date(information.AsOf),
            ReceivedOn = Date(information.ReceivedOn),
            SourceAdvisorId = information.SourceAdvisorId,
            SourceAdvisorName = information.SourceAdvisorName,
            WasRequested = information.WasRequested
        };
    }

    private static AdvisorReportSnapshot CaptureAdvisorReport(
        AdvisorIntelligenceReport report)
    {
        return new AdvisorReportSnapshot
        {
            Id = report.Id,
            Topic = report.Topic,
            AdvisorId = report.Advisor.Id,
            SubjectCountryId = report.SubjectCountry.Id,
            RelatedCountryId = report.RelatedCountry?.Id,
            ProducedOn = Date(report.ProducedOn),
            DataAsOf = Date(report.DataAsOf),
            WasRequested = report.WasRequested,
            Title = report.Title,
            Summary = report.Summary,
            Facts = report.Facts
                .Select(CaptureKnownInformation)
                .ToList(),
            Caveats = report.Caveats.ToList()
        };
    }

    private static OrderSnapshot CaptureOrder(Order order)
    {
        var snapshot = new OrderSnapshot
        {
            Id = order.Id,
            Kind = GetOrderKind(order),
            IssuerId = order.Issuer.Id,
            RecipientId = order.Recipient.Id,
            IssuedOn = Date(order.IssuedOn),
            Status = order.Status
        };

        return order switch
        {
            ChangeTaxOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                TargetTaxRate = typed.TargetTaxRate
            },
            SetBudgetOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                TargetArmyFunding = typed.TargetArmyFunding,
                TargetAdministrationFunding = typed.TargetAdministrationFunding,
                TargetCourtFunding = typed.TargetCourtFunding
            },
            AppointAdvisorOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                Position = typed.Position
            },
            DismissAdvisorOrder typed => snapshot with
            {
                CountryId = typed.Country.Id
            },
            InvestigateCharacterOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                SubjectCharacterId = typed.Subject.Id
            },
            ArrestCharacterOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                SubjectCharacterId = typed.Subject.Id
            },
            ReleasePrisonerOrder typed => snapshot with
            {
                CountryId = typed.Country.Id
            },
            ImproveRelationsOrder typed => snapshot with
            {
                SourceCountryId = typed.SourceCountry.Id,
                TargetCountryId = typed.TargetCountry.Id
            },
            NegotiateTradeAgreementOrder typed => snapshot with
            {
                SourceCountryId = typed.SourceCountry.Id,
                TargetCountryId = typed.TargetCountry.Id
            },
            EndTradeAgreementOrder typed => snapshot with
            {
                SourceCountryId = typed.SourceCountry.Id,
                TargetCountryId = typed.TargetCountry.Id
            },
            DeclareWarOrder typed => snapshot with
            {
                SourceCountryId = typed.SourceCountry.Id,
                TargetCountryId = typed.TargetCountry.Id
            },
            SetWarStanceOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                WarId = typed.War.Id,
                RequestedStance = typed.RequestedStance
            },
            OfferPeaceOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                WarId = typed.War.Id,
                PeaceTerms = typed.Terms
            },
            RespondToDiplomaticProposalOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                DiplomaticProposalId = typed.Proposal.Id,
                Accept = typed.Accept
            },
            RequestReportOrder typed => snapshot with
            {
                CountryId = typed.Country.Id,
                Topic = typed.Topic,
                SubjectCountryId = typed.SubjectCountry.Id,
                RelatedCountryId = typed.RelatedCountry?.Id
            },
            _ => throw new NotSupportedException(
                $"Saving order type {order.GetType().Name} is not supported.")
        };
    }

    private static SaveOrderKind GetOrderKind(Order order)
    {
        return order switch
        {
            ChangeTaxOrder => SaveOrderKind.ChangeTax,
            SetBudgetOrder => SaveOrderKind.SetBudget,
            AppointAdvisorOrder => SaveOrderKind.AppointAdvisor,
            DismissAdvisorOrder => SaveOrderKind.DismissAdvisor,
            InvestigateCharacterOrder => SaveOrderKind.InvestigateCharacter,
            ArrestCharacterOrder => SaveOrderKind.ArrestCharacter,
            ReleasePrisonerOrder => SaveOrderKind.ReleasePrisoner,
            ImproveRelationsOrder => SaveOrderKind.ImproveRelations,
            NegotiateTradeAgreementOrder => SaveOrderKind.NegotiateTradeAgreement,
            EndTradeAgreementOrder => SaveOrderKind.EndTradeAgreement,
            DeclareWarOrder => SaveOrderKind.DeclareWar,
            SetWarStanceOrder => SaveOrderKind.SetWarStance,
            OfferPeaceOrder => SaveOrderKind.OfferPeace,
            RespondToDiplomaticProposalOrder => SaveOrderKind.RespondToDiplomaticProposal,
            RequestReportOrder => SaveOrderKind.RequestReport,
            _ => throw new NotSupportedException(
                $"Saving order type {order.GetType().Name} is not supported.")
        };
    }

    private static DateSnapshot Date(GameDate date) =>
        new(date.Year, date.Month);
}
