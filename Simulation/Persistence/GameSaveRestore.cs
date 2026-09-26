using LeaderGame.Simulation.Campaign;
using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Reports;
using LeaderGame.Simulation.Scenarios;

namespace LeaderGame.Simulation.Persistence;

public static partial class GameSaveService
{
    private static GameState Restore(GameSaveSnapshot snapshot)
    {
        var characters = snapshot.Characters.ToDictionary(
            character => character.Id,
            RestoreCharacter);

        var countries = new Dictionary<string, Country>(
            StringComparer.Ordinal);

        foreach (var saved in snapshot.Countries)
        {
            var country = new Country
            {
                Id = saved.Id,
                Name = saved.Name,
                Population = saved.Population,
                Gdp = saved.Gdp,
                Treasury = saved.Treasury,
                Debt = saved.Debt,
                ArmySize = saved.ArmySize,
                ArmyReadiness = saved.ArmyReadiness,
                TaxRate = saved.TaxRate,
                AdministrativeEfficiency = saved.AdministrativeEfficiency,
                ArmyFunding = saved.ArmyFunding,
                AdministrationFunding = saved.AdministrationFunding,
                CourtFunding = saved.CourtFunding,
                WarExhaustion = saved.WarExhaustion,
                PublicUnrest = saved.PublicUnrest,
                AdministrativeDevelopment =
                    saved.AdministrativeDevelopment !=
                        AdministrativeDevelopment.PatrimonialCourt
                        ? saved.AdministrativeDevelopment
                        : saved.Id == "valeria"
                            ? AdministrativeDevelopment.CollegiateCivic
                            : AdministrativeDevelopment.PatrimonialCourt,
                Government = new Government
                {
                    Type = saved.GovernmentType,
                    Stability = saved.GovernmentStability,
                    ElectionIntervalMonths =
                        saved.ElectionIntervalMonths > 0
                            ? saved.ElectionIntervalMonths
                            : saved.GovernmentType == GovernmentType.Republic
                                ? 48
                                : 0,
                    MonthsUntilElection =
                        saved.MonthsUntilElection > 0
                            ? saved.MonthsUntilElection
                            : saved.GovernmentType == GovernmentType.Republic
                                ? 12
                                : 0,
                    ElectionCampaignMonths =
                        saved.ElectionCampaignMonths > 0
                            ? saved.ElectionCampaignMonths
                            : 6,
                    ElectionMethod =
                        saved.ElectionMethod != ElectionMethod.None
                            ? saved.ElectionMethod
                            : saved.Id == "valeria"
                                ? ElectionMethod.CouncilElection
                                : saved.GovernmentType == GovernmentType.Republic
                                    ? ElectionMethod.PopularElection
                                    : ElectionMethod.None,
                    LegislativeBody =
                        saved.LegislativeBody != LegislativeBodyType.None
                            ? saved.LegislativeBody
                            : saved.Id switch
                            {
                                "valeria" => LegislativeBodyType.GreatCouncil,
                                "falkenreich" => LegislativeBodyType.EstatesAssembly,
                                _ => saved.GovernmentType switch
                                {
                                    GovernmentType.Republic => LegislativeBodyType.Assembly,
                                    GovernmentType.FeudalMonarchy => LegislativeBodyType.RoyalCouncil,
                                    _ => LegislativeBodyType.None
                                }
                            },
                    LegislativeIndependence =
                        saved.LegislativeIndependence > 0
                            ? saved.LegislativeIndependence
                            : saved.Id switch
                            {
                                "valeria" => 75,
                                "falkenreich" => 55,
                                _ => saved.GovernmentType switch
                                {
                                    GovernmentType.Republic => 70,
                                    GovernmentType.FeudalMonarchy => 40,
                                    _ => 0
                                }
                            }
                },
                Ruler = RequireCharacter(characters, saved.RulerId)
            };

            country.LastMonthlyTaxRevenue = saved.LastMonthlyTaxRevenue;
            country.LastMonthlyTradeIncome = saved.LastMonthlyTradeIncome;
            country.LastMonthlyExpenses = saved.LastMonthlyExpenses;
            country.LastMonthlyDebtInterest = saved.LastMonthlyDebtInterest;
            country.LastMonthlyBalance = saved.LastMonthlyBalance;

            country.NeighborIds.UnionWith(saved.NeighborIds);

            foreach (var id in saved.SuccessionOrderIds)
                country.SuccessionOrder.Add(RequireCharacter(characters, id));

            foreach (var id in saved.PoliticalFigureIds)
                country.PoliticalFigures.Add(RequireCharacter(characters, id));

            foreach (var powerBase in saved.PowerBaseStrengths)
                country.SetPowerBaseStrength(powerBase.Type, powerBase.Value);

            foreach (var office in saved.AdministrativeOffices)
            {
                country.AdministrativeOffices.Add(new AdministrativeOffice
                {
                    Function = office.Function,
                    Name = office.Name,
                    ResponsiblePosition = office.ResponsiblePosition,
                    Capacity = office.Capacity,
                    Reach = office.Reach,
                    Integrity = office.Integrity,
                    Workload = office.Workload,
                    PatronageDependence = office.PatronageDependence
                });
            }

            foreach (var region in saved.Regions)
            {
                country.Regions.Add(new Region
                {
                    Id = region.Id,
                    Name = region.Name,
                    EconomicShare = region.EconomicShare,
                    PopulationShare = region.PopulationShare,
                    CrownControl = region.CrownControl,
                    LocalElitePower = region.LocalElitePower,
                    Unrest = region.Unrest,
                    Privileges = region.Privileges,
                    Prosperity = region.Prosperity
                });
            }

            if (country.Regions.Count == 0)
                AddLegacyRegions(country);

            countries.Add(country.Id, country);
        }

        var lineage = new PoliticalLineage
        {
            Id = snapshot.Player.LineageId,
            Name = snapshot.Player.LineageName,
            Type = snapshot.Player.LineageType
        };

        foreach (var memberId in snapshot.Player.LineageMemberIds)
            lineage.Members.Add(RequireCharacter(characters, memberId));

        var player = new PlayerState
        {
            CurrentCharacter = RequireCharacter(
                characters,
                snapshot.Player.CurrentCharacterId),
            Country = RequireCountry(
                countries,
                snapshot.Player.CountryId),
            Lineage = lineage,
            HasLost = snapshot.Player.HasLost,
            LossReason = snapshot.Player.LossReason,
            HasWon = snapshot.Player.HasWon,
            WinReason = snapshot.Player.WinReason,
            ElectionsWon = snapshot.Player.ElectionsWon,
            MonthsOutOfPower = snapshot.Player.MonthsOutOfPower,
            ConsecutiveLowViabilityMonths =
                snapshot.Player.ConsecutiveLowViabilityMonths,
            PoliticalViability = snapshot.Player.PoliticalViability
        };

        var state = new GameState
        {
            Date = GameDate(snapshot.Date),
            Player = player,
            Random = new SimulationRandom(1)
            {
                State = snapshot.RandomState
            },
            InformationRandom = new SimulationRandom(1)
            {
                State = snapshot.InformationRandomState
            }
        };

        state.Campaign = RestoreCampaign(
            snapshot.Campaign,
            player.Country.Id,
            state.Date);

        foreach (var country in snapshot.Countries)
            state.Countries.Add(RequireCountry(countries, country.Id));

        foreach (var relationship in snapshot.Relationships)
        {
            state.Relationships.Set(
                RequireCharacter(characters, relationship.FromId),
                RequireCharacter(characters, relationship.ToId),
                relationship.Opinion,
                relationship.Trust,
                relationship.Fear);
        }

        foreach (var relation in snapshot.Diplomacy)
        {
            state.Diplomacy.Set(
                RequireCountry(countries, relation.CountryAId),
                RequireCountry(countries, relation.CountryBId),
                relation.Relations,
                relation.Trust,
                relation.Tension,
                relation.HasTradeAgreement,
                relation.TradeAgreementStartedOn is { } started
                    ? GameDate(started)
                    : null);
        }

        var wars = new Dictionary<Guid, War>();

        foreach (var saved in snapshot.Wars)
        {
            var war = new War
            {
                Id = saved.Id,
                Attacker = RequireCountry(countries, saved.AttackerId),
                Defender = RequireCountry(countries, saved.DefenderId),
                StartedOn = GameDate(saved.StartedOn),
                AttackerAim = saved.AttackerAim,
                WarScore = saved.WarScore,
                AttackerStance = saved.AttackerStance,
                DefenderStance = saved.DefenderStance
            };

            war.MonthsActive = saved.MonthsActive;
            war.Status = saved.Status;

            state.Wars.Add(war);
            wars.Add(war.Id, war);
        }

        foreach (var saved in snapshot.Plots)
        {
            var plot = new PoliticalPlot
            {
                Id = saved.Id,
                Type = saved.Type,
                Country = RequireCountry(countries, saved.CountryId),
                Instigator = RequireCharacter(characters, saved.InstigatorId),
                Progress = saved.Progress
            };

            plot.SupporterIds.UnionWith(saved.SupporterIds);
            plot.DiscoveryStage = saved.DiscoveryStage;
            plot.IsResolved = saved.IsResolved;
            plot.Succeeded = saved.Succeeded;
            state.Plots.Add(plot);
        }

        foreach (var saved in snapshot.PowerBaseDemands)
        {
            state.PowerBaseDemands.Add(new PowerBaseDemand
            {
                Id = saved.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : saved.Id,
                Country = RequireCountry(countries, saved.CountryId),
                PowerBase = saved.PowerBase,
                Type = saved.Type,
                Spokesperson = saved.SpokespersonId is { } spokespersonId
                    ? RequireCharacter(characters, spokespersonId)
                    : null,
                TargetValue = saved.TargetValue,
                MonthsOpen = saved.MonthsOpen,
                EscalationLevel = saved.EscalationLevel,
                AcknowledgedByRuler = saved.AcknowledgedByRuler,
                IsRejected = saved.IsRejected,
                ResolvedOn = saved.ResolvedOn is { } resolvedOn
                    ? GameDate(resolvedOn)
                    : null,
                IsResolved = saved.IsResolved
            });
        }

        foreach (var saved in snapshot.PoliticalBlocs)
        {
            var bloc = new PoliticalBloc
            {
                Id = saved.Id,
                Country = RequireCountry(countries, saved.CountryId),
                Leader = RequireCharacter(characters, saved.LeaderId),
                MonthsActive = saved.MonthsActive,
                Cohesion = saved.Cohesion,
                IsActive = saved.IsActive
            };

            bloc.PowerBases.UnionWith(saved.PowerBases);
            bloc.MemberIds.UnionWith(saved.MemberIds);
            state.PoliticalBlocs.Add(bloc);
        }

        foreach (var saved in snapshot.CabinetProposals)
        {
            state.CabinetProposals.Add(new CabinetProposal
            {
                Id = saved.Id,
                Country = RequireCountry(countries, saved.CountryId),
                Advisor = RequireCharacter(characters, saved.AdvisorId),
                Type = saved.Type,
                TargetCountry = saved.TargetCountryId is { } targetId
                    ? RequireCountry(countries, targetId)
                    : null,
                War = saved.WarId is { } warId
                    ? RequireWar(wars, warId)
                    : null,
                TargetValue = saved.TargetValue,
                Rationale = saved.Rationale,
                CreatedOn = GameDate(saved.CreatedOn),
                MonthsOpen = saved.MonthsOpen,
                Status = saved.Status
            });
        }

        foreach (var saved in snapshot.PoliticalCrises)
        {
            var country = RequireCountry(
                countries,
                saved.CountryId);

            state.PoliticalCrises.Add(new PoliticalCrisis
            {
                Id = saved.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : saved.Id,
                Country = country,
                Type = saved.Type,
                Region = saved.RegionId is { } regionId
                    ? RequireRegion(country, regionId)
                    : null,
                RelatedBlocId = saved.RelatedBlocId,
                RelatedWarId = saved.RelatedWarId,
                RelatedCharacterId = saved.RelatedCharacterId,
                StartedOn = GameDate(saved.StartedOn),
                Stage = saved.Stage <= 0
                    ? 1
                    : saved.Stage,
                MonthsActive = saved.MonthsActive,
                MonthsAtCurrentStage =
                    saved.MonthsAtCurrentStage,
                AwaitingDecision =
                    saved.AwaitingDecision,
                LastResponse = saved.LastResponse,
                Status = saved.Status,
                ResolvedOn = saved.ResolvedOn is { } resolved
                    ? GameDate(resolved)
                    : null
            });
        }

        foreach (var saved in snapshot.ElectionPromises)
        {
            state.ElectionPromises.Add(new ElectionPromise
            {
                Id = saved.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : saved.Id,
                Country = RequireCountry(countries, saved.CountryId),
                Candidate = RequireCharacter(characters, saved.CandidateId),
                Type = saved.Type,
                TargetValue = saved.TargetValue,
                MadeOn = GameDate(saved.MadeOn),
                Status = saved.Status,
                MonthsSinceElection = saved.MonthsSinceElection
            });
        }

        var proposals = new Dictionary<Guid, DiplomaticProposal>();

        foreach (var saved in snapshot.DiplomaticProposals)
        {
            var proposal = new DiplomaticProposal
            {
                Id = saved.Id,
                Type = saved.Type,
                SourceCountry = RequireCountry(
                    countries,
                    saved.SourceCountryId),
                TargetCountry = RequireCountry(
                    countries,
                    saved.TargetCountryId),
                CreatedOn = GameDate(saved.CreatedOn),
                DemandedPayment = saved.DemandedPayment
            };

            proposal.MonthsOpen = saved.MonthsOpen;
            proposal.Status = saved.Status;

            state.DiplomaticProposals.Add(proposal);
            proposals.Add(proposal.Id, proposal);
        }

        foreach (var saved in snapshot.Reports)
        {
            state.Reports.Add(new SimulationReport(
                GameDate(saved.Date),
                saved.Category,
                saved.Title,
                saved.Details));
        }

        foreach (var saved in snapshot.Knowledge)
            state.Knowledge.Update(RestoreKnownInformation(saved));

        foreach (var saved in snapshot.AdvisorReports)
        {
            var report = new AdvisorIntelligenceReport
            {
                Id = saved.Id,
                Topic = saved.Topic,
                Advisor = RequireCharacter(characters, saved.AdvisorId),
                SubjectCountry = RequireCountry(
                    countries,
                    saved.SubjectCountryId),
                RelatedCountry = saved.RelatedCountryId is { } relatedId
                    ? RequireCountry(countries, relatedId)
                    : null,
                ProducedOn = GameDate(saved.ProducedOn),
                DataAsOf = GameDate(saved.DataAsOf),
                WasRequested = saved.WasRequested,
                Title = saved.Title,
                Summary = saved.Summary
            };

            report.Facts.AddRange(
                saved.Facts.Select(RestoreKnownInformation));
            report.Caveats.AddRange(saved.Caveats);
            state.AdvisorReports.Add(report);
        }

        foreach (var saved in snapshot.InformationRequests)
        {
            state.InformationRequests.Add(new InformationRequest
            {
                Id = saved.Id,
                Topic = saved.Topic,
                Advisor = RequireCharacter(characters, saved.AdvisorId),
                SubjectCountry = RequireCountry(
                    countries,
                    saved.SubjectCountryId),
                RelatedCountry = saved.RelatedCountryId is { } relatedId
                    ? RequireCountry(countries, relatedId)
                    : null,
                RequestedOn = GameDate(saved.RequestedOn),
                RemainingMonths = saved.RemainingMonths,
                WillingnessAtRequest = saved.WillingnessAtRequest,
                Status = saved.Status
            });
        }

        foreach (var saved in snapshot.InformationHistory)
        {
            var history = new InformationTruthSnapshot
            {
                Date = GameDate(saved.Date)
            };

            foreach (var value in saved.Values)
            {
                history.Values[new InformationKey(
                    value.Metric,
                    value.SubjectCountryId,
                    value.RelatedCountryId)] = value.Value;
            }

            state.InformationHistory.Add(history);
        }

        foreach (var saved in snapshot.LegislativeProposals)
        {
            state.LegislativeProposals.Add(new LegislativeProposal
            {
                Id = saved.Id,
                Country = RequireCountry(countries, saved.CountryId),
                Sponsor = RequireCharacter(characters, saved.SponsorId),
                Drafter = RequireCharacter(characters, saved.DrafterId),
                Type = saved.Type,
                CreatedOn = GameDate(saved.CreatedOn),
                MonthsOpen = saved.MonthsOpen,
                Status = saved.Status,
                TargetTaxRate = saved.TargetTaxRate,
                TargetArmyFunding = saved.TargetArmyFunding,
                TargetAdministrationFunding = saved.TargetAdministrationFunding,
                TargetCourtFunding = saved.TargetCourtFunding
            });
        }

        foreach (var saved in snapshot.PendingOrders)
        {
            state.PendingOrders.Add(
                RestoreOrder(
                    saved,
                    characters,
                    countries,
                    wars,
                    proposals));
        }

        return state;
    }

    private static CampaignState RestoreCampaign(
        CampaignSnapshot? saved,
        string playerCountryId,
        GameDate currentDate)
    {
        if (saved is null)
        {
            var scenarioId = playerCountryId switch
            {
                ScenarioCatalog.ValeriaId => ScenarioCatalog.ValeriaId,
                _ => ScenarioCatalog.FalkenreichId
            };

            return ScenarioCatalog.CreateCampaign(
                scenarioId,
                currentDate);
        }

        var campaign = new CampaignState
        {
            ScenarioId = saved.ScenarioId,
            Title = saved.Title,
            Summary = saved.Summary,
            StartedOn = GameDate(saved.StartedOn)
        };

        foreach (var objective in saved.Objectives)
        {
            campaign.Objectives.Add(new CampaignObjective
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
                    ? GameDate(completed)
                    : null
            });
        }

        return campaign;
    }

    private static Character RestoreCharacter(CharacterSnapshot saved)
    {
        var character = new Character
        {
            Id = saved.Id,
            FirstName = saved.FirstName,
            LastName = saved.LastName,
            Age = saved.Age,
            Health = saved.Health,
            Competence = saved.Competence,
            Ambition = saved.Ambition,
            Legitimacy = saved.Legitimacy,
            Influence = saved.Influence,
            Position = saved.Position,
            Status = saved.Status,
            IsAlive = saved.IsAlive
        };

        foreach (var allegiance in saved.Allegiances)
            character.SetAllegiance(allegiance.Key, allegiance.Value);

        foreach (var powerBase in saved.PowerBaseStanding)
            character.SetPowerBaseStanding(powerBase.Type, powerBase.Value);

        return character;
    }

    private static KnownInformation RestoreKnownInformation(
        KnownInformationSnapshot saved)
    {
        return new KnownInformation
        {
            Key = new InformationKey(
                saved.Metric,
                saved.SubjectCountryId,
                saved.RelatedCountryId),
            Estimate = saved.Estimate,
            Margin = saved.Margin,
            ReportedConfidence = saved.ReportedConfidence,
            AsOf = GameDate(saved.AsOf),
            ReceivedOn = GameDate(saved.ReceivedOn),
            SourceAdvisorId = saved.SourceAdvisorId,
            SourceAdvisorName = saved.SourceAdvisorName,
            WasRequested = saved.WasRequested
        };
    }

    private static Order RestoreOrder(
        OrderSnapshot saved,
        IReadOnlyDictionary<int, Character> characters,
        IReadOnlyDictionary<string, Country> countries,
        IReadOnlyDictionary<Guid, War> wars,
        IReadOnlyDictionary<Guid, DiplomaticProposal> proposals)
    {
        var issuer = RequireCharacter(characters, saved.IssuerId);
        var recipient = RequireCharacter(characters, saved.RecipientId);
        var issuedOn = GameDate(saved.IssuedOn);

        Order order = saved.Kind switch
        {
            SaveOrderKind.ChangeTax => new ChangeTaxOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                TargetTaxRate = Require(
                    saved.TargetTaxRate,
                    nameof(saved.TargetTaxRate))
            },

            SaveOrderKind.SetBudget => new SetBudgetOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                TargetArmyFunding = Require(
                    saved.TargetArmyFunding,
                    nameof(saved.TargetArmyFunding)),
                TargetAdministrationFunding = Require(
                    saved.TargetAdministrationFunding,
                    nameof(saved.TargetAdministrationFunding)),
                TargetCourtFunding = Require(
                    saved.TargetCourtFunding,
                    nameof(saved.TargetCourtFunding))
            },

            SaveOrderKind.AppointAdvisor => new AppointAdvisorOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                Position = Require(saved.Position, nameof(saved.Position))
            },

            SaveOrderKind.DismissAdvisor => new DismissAdvisorOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId)
            },

            SaveOrderKind.InvestigateCharacter => new InvestigateCharacterOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                Subject = RequireCharacter(
                    characters,
                    Require(
                        saved.SubjectCharacterId,
                        nameof(saved.SubjectCharacterId)))
            },

            SaveOrderKind.ArrestCharacter => new ArrestCharacterOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                Subject = RequireCharacter(
                    characters,
                    Require(
                        saved.SubjectCharacterId,
                        nameof(saved.SubjectCharacterId)))
            },

            SaveOrderKind.ReleasePrisoner => new ReleasePrisonerOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId)
            },

            SaveOrderKind.ImproveRelations => new ImproveRelationsOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                SourceCountry = RequireCountry(
                    countries,
                    saved.SourceCountryId),
                TargetCountry = RequireCountry(
                    countries,
                    saved.TargetCountryId)
            },

            SaveOrderKind.NegotiateTradeAgreement =>
                new NegotiateTradeAgreementOrder
                {
                    Id = saved.Id,
                    Issuer = issuer,
                    Recipient = recipient,
                    IssuedOn = issuedOn,
                    SourceCountry = RequireCountry(
                        countries,
                        saved.SourceCountryId),
                    TargetCountry = RequireCountry(
                        countries,
                        saved.TargetCountryId)
                },

            SaveOrderKind.EndTradeAgreement => new EndTradeAgreementOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                SourceCountry = RequireCountry(
                    countries,
                    saved.SourceCountryId),
                TargetCountry = RequireCountry(
                    countries,
                    saved.TargetCountryId)
            },

            SaveOrderKind.DeclareWar => new DeclareWarOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                SourceCountry = RequireCountry(
                    countries,
                    saved.SourceCountryId),
                TargetCountry = RequireCountry(
                    countries,
                    saved.TargetCountryId),
                Aim = saved.WarAim ??
                    WarAim.Reparations
            },

            SaveOrderKind.SetWarStance => new SetWarStanceOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                War = RequireWar(wars, saved.WarId),
                RequestedStance = Require(
                    saved.RequestedStance,
                    nameof(saved.RequestedStance))
            },

            SaveOrderKind.OfferPeace => new OfferPeaceOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                War = RequireWar(wars, saved.WarId),
                Terms = Require(
                    saved.PeaceTerms,
                    nameof(saved.PeaceTerms))
            },

            SaveOrderKind.RespondToDiplomaticProposal =>
                new RespondToDiplomaticProposalOrder
                {
                    Id = saved.Id,
                    Issuer = issuer,
                    Recipient = recipient,
                    IssuedOn = issuedOn,
                    Country = RequireCountry(countries, saved.CountryId),
                    Proposal = RequireProposal(
                        proposals,
                        saved.DiplomaticProposalId),
                    Accept = Require(
                        saved.Accept,
                        nameof(saved.Accept))
                },

            SaveOrderKind.RequestReport => new RequestReportOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                Topic = Require(saved.Topic, nameof(saved.Topic)),
                SubjectCountry = RequireCountry(
                    countries,
                    saved.SubjectCountryId),
                RelatedCountry = saved.RelatedCountryId is { } relatedId
                    ? RequireCountry(countries, relatedId)
                    : null
            },

            SaveOrderKind.OppositionAction => new OppositionActionOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                ActionType = Require(
                    saved.OppositionAction,
                    nameof(saved.OppositionAction)),
                TargetPowerBase = Require(
                    saved.TargetPowerBase,
                    nameof(saved.TargetPowerBase))
            },

            SaveOrderKind.AdministrativeReform => new AdministrativeReformOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                Function = Require(
                    saved.AdministrativeFunction,
                    nameof(saved.AdministrativeFunction)),
                ReformType = Require(
                    saved.AdministrativeReform,
                    nameof(saved.AdministrativeReform))
            },

            SaveOrderKind.RegionalAction => new RegionalActionOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                Region = RequireRegion(
                    RequireCountry(countries, saved.CountryId),
                    saved.RegionId),
                ActionType = Require(
                    saved.RegionalAction,
                    nameof(saved.RegionalAction))
            },

            SaveOrderKind.CourtAction => new CourtActionOrder
            {
                Id = saved.Id,
                Issuer = issuer,
                Recipient = recipient,
                IssuedOn = issuedOn,
                Country = RequireCountry(countries, saved.CountryId),
                Subject = RequireCharacter(
                    characters,
                    Require(
                        saved.SubjectCharacterId,
                        nameof(saved.SubjectCharacterId))),
                ActionType = Require(
                    saved.CourtAction,
                    nameof(saved.CourtAction))
            },

            _ => throw new InvalidDataException(
                $"Unsupported pending order kind {saved.Kind}.")
        };

        order.Status = saved.Status;
        return order;
    }

    private static Character RequireCharacter(
        IReadOnlyDictionary<int, Character> characters,
        int id)
    {
        return characters.TryGetValue(id, out var character)
            ? character
            : throw new InvalidDataException(
                $"Save references missing character {id}.");
    }

    private static Country RequireCountry(
        IReadOnlyDictionary<string, Country> countries,
        string? id)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            !countries.TryGetValue(id, out var country))
        {
            throw new InvalidDataException(
                $"Save references missing country '{id ?? "<null>"}'.");
        }

        return country;
    }

    private static Region RequireRegion(
        Country country,
        string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidDataException("Save references a regional order without a region.");

        return country.FindRegion(id) ??
               throw new InvalidDataException(
                   $"Save references missing region '{id}' in {country.Name}.");
    }

    private static void AddLegacyRegions(Country country)
    {
        var baselineControl =
            Math.Clamp(
                (double)country.AdministrativeEfficiency * 100,
                35,
                80);

        country.Regions.AddRange(
        [
            new Region
            {
                Id = $"{country.Id}-core",
                Name = $"{country.Name} Heartland",
                EconomicShare = 0.40m,
                PopulationShare = 0.35m,
                CrownControl = baselineControl + 10,
                LocalElitePower = 45,
                Unrest = Math.Max(8, country.PublicUnrest - 5),
                Privileges = 40,
                Prosperity = 65
            },
            new Region
            {
                Id = $"{country.Id}-provinces",
                Name = "Inner Provinces",
                EconomicShare = 0.35m,
                PopulationShare = 0.40m,
                CrownControl = baselineControl,
                LocalElitePower = 62,
                Unrest = country.PublicUnrest,
                Privileges = 58,
                Prosperity = 55
            },
            new Region
            {
                Id = $"{country.Id}-outer",
                Name = "Outer Provinces",
                EconomicShare = 0.25m,
                PopulationShare = 0.25m,
                CrownControl = baselineControl - 15,
                LocalElitePower = 75,
                Unrest = Math.Min(100, country.PublicUnrest + 8),
                Privileges = 70,
                Prosperity = 48
            }
        ]);
    }

    private static War RequireWar(
        IReadOnlyDictionary<Guid, War> wars,
        Guid? id)
    {
        if (!id.HasValue ||
            !wars.TryGetValue(id.Value, out var war))
        {
            throw new InvalidDataException(
                $"Save references missing war '{id?.ToString() ?? "<null>"}'.");
        }

        return war;
    }

    private static DiplomaticProposal RequireProposal(
        IReadOnlyDictionary<Guid, DiplomaticProposal> proposals,
        Guid? id)
    {
        if (!id.HasValue ||
            !proposals.TryGetValue(id.Value, out var proposal))
        {
            throw new InvalidDataException(
                $"Save references missing diplomatic proposal " +
                $"'{id?.ToString() ?? "<null>"}'.");
        }

        return proposal;
    }

    private static T Require<T>(T? value, string fieldName)
        where T : struct
    {
        return value ?? throw new InvalidDataException(
            $"Save is missing required field {fieldName}.");
    }

    private static GameDate GameDate(DateSnapshot saved) =>
        new(saved.Year, saved.Month);
}
