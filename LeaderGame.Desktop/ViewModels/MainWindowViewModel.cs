using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LeaderGame.Presentation;

namespace LeaderGame.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly GameSession _session = new();
    private string _currentSection = "Briefing";
    private ForeignCountryOptionView? _selectedForeignCountry;
    private CourtFigureView? _selectedCourtFigure;
    private WarCampaignView? _selectedCampaign;
    private string _selectedOffice = "Chancellor";
    private string _selectedWarStance = "Balanced";
    private string _selectedPeaceTerms = "WhitePeace";
    private string _archiveSearchText = string.Empty;
    private string _selectedArchiveCategory = "All";
    private double _targetTaxPercent;
    private double _targetArmyFundingPercent;
    private double _targetAdministrationFundingPercent;
    private double _targetCourtFundingPercent;

    private readonly RelayCommand _advanceMonthCommand;
    private readonly RelayCommand _loadCommand;

    public MainWindowViewModel()
    {
        _selectedForeignCountry = View.ForeignCountries.FirstOrDefault();
        _selectedCourtFigure = View.Court.Figures.FirstOrDefault(figure =>
            figure.IsAvailableForOffice);
        _selectedCampaign = View.Military.Campaigns.FirstOrDefault();

        SyncPolicyTargets();

        _advanceMonthCommand = new RelayCommand(
            _ =>
            {
                _session.AdvanceMonth();
                CurrentSection = "Briefing";
                SyncPolicyTargets();
                RefreshBindings();
            },
            _ => !View.HasLost);

        SaveCommand = new RelayCommand(_ =>
        {
            _session.SaveDefault();
            RefreshBindings();
            _loadCommand.RaiseCanExecuteChanged();
        });

        _loadCommand = new RelayCommand(
            _ =>
            {
                _session.LoadDefault();
                CurrentSection = "Briefing";
                SyncPolicyTargets();
                RefreshBindings();
                _advanceMonthCommand.RaiseCanExecuteChanged();
            },
            _ => _session.DefaultSaveExists);

        NavigateCommand = new RelayCommand(parameter =>
        {
            CurrentSection = parameter?.ToString() ?? "Briefing";
        });

        RequestEconomyReportCommand = new RelayCommand(
            _ => RunAndRefresh(_session.RequestEconomyReport));

        RequestOwnMilitaryReportCommand = new RelayCommand(
            _ => RunAndRefresh(_session.RequestOwnMilitaryReport));

        RequestDomesticReportCommand = new RelayCommand(
            _ => RunAndRefresh(_session.RequestDomesticPoliticsReport));

        RequestForeignMilitaryReportCommand = new RelayCommand(_ =>
        {
            if (SelectedForeignCountry is null)
            {
                RunAndRefresh(() =>
                    _session.Refresh("Choose a foreign country before requesting military intelligence."));
                return;
            }

            RunAndRefresh(() =>
                _session.RequestForeignMilitaryReport(SelectedForeignCountry.Id));
        });

        RequestForeignAffairsReportCommand = new RelayCommand(_ =>
        {
            if (SelectedForeignCountry is null)
            {
                RunAndRefresh(() =>
                    _session.Refresh("Choose a foreign country before requesting a diplomatic assessment."));
                return;
            }

            RunAndRefresh(() =>
                _session.RequestForeignAffairsReport(SelectedForeignCountry.Id));
        });

        ApplyTaxCommand = new RelayCommand(_ =>
            RunAndRefresh(() =>
                _session.SetTaxRate(TargetTaxPercent)));

        ApplyBudgetCommand = new RelayCommand(_ =>
            RunAndRefresh(() =>
                _session.SetBudget(
                    TargetArmyFundingPercent,
                    TargetAdministrationFundingPercent,
                    TargetCourtFundingPercent)));

        AppointAdvisorCommand = new RelayCommand(_ =>
        {
            if (SelectedCourtFigure is null)
            {
                RunAndRefresh(() =>
                    _session.Refresh("Choose a political figure to appoint."));
                return;
            }

            RunAndRefresh(() =>
                _session.AppointAdvisor(
                    SelectedCourtFigure.Id,
                    SelectedOffice));
        });

        DismissAdvisorCommand = new RelayCommand(_ =>
            RunAndRefresh(() =>
                _session.DismissAdvisor(SelectedOffice)));

        ImproveRelationsCommand = new RelayCommand(_ =>
        {
            if (SelectedForeignCountry is null)
                return;

            RunAndRefresh(() =>
                _session.ImproveRelations(SelectedForeignCountry.Id));
        });

        ToggleTradeCommand = new RelayCommand(_ =>
        {
            if (SelectedForeignCountry is null)
                return;

            RunAndRefresh(() =>
                _session.ToggleTradeAgreement(SelectedForeignCountry.Id));
        });

        DeclareWarCommand = new RelayCommand(_ =>
        {
            if (SelectedForeignCountry is null)
                return;

            RunAndRefresh(() =>
                _session.DeclareWar(SelectedForeignCountry.Id));
        });

        AcceptProposalCommand = new RelayCommand(parameter =>
        {
            if (parameter is DiplomaticProposalView proposal)
            {
                RunAndRefresh(() =>
                    _session.RespondToDiplomaticProposal(proposal.Id, accept: true));
            }
        });

        RejectProposalCommand = new RelayCommand(parameter =>
        {
            if (parameter is DiplomaticProposalView proposal)
            {
                RunAndRefresh(() =>
                    _session.RespondToDiplomaticProposal(proposal.Id, accept: false));
            }
        });

        ApplyWarStanceCommand = new RelayCommand(_ =>
        {
            if (SelectedCampaign is null)
                return;

            RunAndRefresh(() =>
                _session.SetWarStance(
                    SelectedCampaign.Id,
                    SelectedWarStance));
        });

        OfferPeaceCommand = new RelayCommand(_ =>
        {
            if (SelectedCampaign is null)
                return;

            RunAndRefresh(() =>
                _session.OfferPeace(
                    SelectedCampaign.Id,
                    SelectedPeaceTerms));
        });

        RequestCampaignIntelligenceCommand = new RelayCommand(_ =>
        {
            if (SelectedCampaign is null)
                return;

            RunAndRefresh(() =>
                _session.RequestForeignMilitaryReport(
                    SelectedCampaign.OpponentId));
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PlayerViewState View => _session.View;

    public ICommand AdvanceMonthCommand => _advanceMonthCommand;

    public ICommand SaveCommand { get; }

    public ICommand LoadCommand => _loadCommand;

    public string SaveLocation => GameSession.DefaultSavePath;

    public ICommand NavigateCommand { get; }

    public ICommand RequestEconomyReportCommand { get; }

    public ICommand RequestOwnMilitaryReportCommand { get; }

    public ICommand RequestDomesticReportCommand { get; }

    public ICommand RequestForeignMilitaryReportCommand { get; }

    public ICommand RequestForeignAffairsReportCommand { get; }

    public ICommand ApplyTaxCommand { get; }

    public ICommand ApplyBudgetCommand { get; }

    public ICommand AppointAdvisorCommand { get; }

    public ICommand DismissAdvisorCommand { get; }

    public ICommand ImproveRelationsCommand { get; }

    public ICommand ToggleTradeCommand { get; }

    public ICommand DeclareWarCommand { get; }

    public ICommand AcceptProposalCommand { get; }

    public ICommand RejectProposalCommand { get; }

    public ICommand ApplyWarStanceCommand { get; }

    public ICommand OfferPeaceCommand { get; }

    public ICommand RequestCampaignIntelligenceCommand { get; }

    public string CurrentSection
    {
        get => _currentSection;
        private set
        {
            if (_currentSection == value)
                return;

            _currentSection = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBriefingVisible));
            OnPropertyChanged(nameof(IsGovernmentVisible));
            OnPropertyChanged(nameof(IsCourtVisible));
            OnPropertyChanged(nameof(IsEconomyVisible));
            OnPropertyChanged(nameof(IsForeignAffairsVisible));
            OnPropertyChanged(nameof(IsMilitaryVisible));
            OnPropertyChanged(nameof(IsIntelligenceVisible));
            OnPropertyChanged(nameof(IsArchiveVisible));
            OnPropertyChanged(nameof(IsPlaceholderVisible));
            OnPropertyChanged(nameof(IsBriefingSelected));
            OnPropertyChanged(nameof(IsGovernmentSelected));
            OnPropertyChanged(nameof(IsCourtSelected));
            OnPropertyChanged(nameof(IsEconomySelected));
            OnPropertyChanged(nameof(IsForeignAffairsSelected));
            OnPropertyChanged(nameof(IsMilitarySelected));
            OnPropertyChanged(nameof(IsIntelligenceSelected));
            OnPropertyChanged(nameof(IsArchiveSelected));
            OnPropertyChanged(nameof(SectionTitle));
            OnPropertyChanged(nameof(SectionDescription));
        }
    }

    public bool IsBriefingVisible => CurrentSection == "Briefing";

    public bool IsGovernmentVisible => CurrentSection == "Government";

    public bool IsCourtVisible => CurrentSection == "Court";

    public bool IsEconomyVisible => CurrentSection == "Economy";

    public bool IsForeignAffairsVisible => CurrentSection == "Foreign Affairs";

    public bool IsMilitaryVisible => CurrentSection == "Military";

    public bool IsIntelligenceVisible => CurrentSection == "Intelligence";

    public bool IsArchiveVisible => CurrentSection == "Archive";

    public bool IsPlaceholderVisible =>
        !IsBriefingVisible &&
        !IsGovernmentVisible &&
        !IsCourtVisible &&
        !IsEconomyVisible &&
        !IsForeignAffairsVisible &&
        !IsMilitaryVisible &&
        !IsIntelligenceVisible &&
        !IsArchiveVisible;

    public bool IsBriefingSelected => CurrentSection == "Briefing";
    public bool IsGovernmentSelected => CurrentSection == "Government";
    public bool IsCourtSelected => CurrentSection == "Court";
    public bool IsEconomySelected => CurrentSection == "Economy";
    public bool IsForeignAffairsSelected => CurrentSection == "Foreign Affairs";
    public bool IsMilitarySelected => CurrentSection == "Military";
    public bool IsIntelligenceSelected => CurrentSection == "Intelligence";
    public bool IsArchiveSelected => CurrentSection == "Archive";

    public bool HasPendingReports => View.PendingReports > 0;

    public bool HasActiveCampaigns => View.Military.Campaigns.Count > 0;

    public string SectionTitle => CurrentSection;

    public string SectionDescription => CurrentSection switch
    {
        "Economy" =>
            "Reported economic history and fiscal estimates, including disagreement between successive reports.",
        "Foreign Affairs" =>
            "Treaties, diplomatic messages, country assessments and relationships as reported by the Chancellor.",
        "Military" =>
            "Campaign directives, commanders and uncertain field intelligence rather than perfect battlefield knowledge.",
        "Archive" =>
            "Historical reports, orders, treaties, crises and the changing record of what the government believed at the time.",
        _ =>
            "This desktop surface has not been wired yet."
    };

    public IReadOnlyList<ForeignCountryOptionView> ForeignCountries =>
        View.ForeignCountries;

    public ForeignStateView? SelectedForeignState =>
        SelectedForeignCountry is null
            ? null
            : View.ForeignAffairs.Countries.FirstOrDefault(country =>
                country.Id == SelectedForeignCountry.Id);

    public string TradeActionLabel =>
        SelectedForeignState?.HasTradeAgreement == true
            ? "End trade agreement"
            : "Propose trade agreement";

    public IReadOnlyList<CourtFigureView> AppointmentCandidates =>
        View.Court.Figures
            .Where(figure => figure.IsAvailableForOffice)
            .ToList();

    public IReadOnlyList<string> OfficeOptions { get; } =
        ["Chancellor", "Treasurer", "Marshal"];

    public ForeignCountryOptionView? SelectedForeignCountry
    {
        get => _selectedForeignCountry;
        set
        {
            if (Equals(_selectedForeignCountry, value))
                return;

            _selectedForeignCountry = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedForeignState));
            OnPropertyChanged(nameof(TradeActionLabel));
        }
    }

    public CourtFigureView? SelectedCourtFigure
    {
        get => _selectedCourtFigure;
        set
        {
            if (Equals(_selectedCourtFigure, value))
                return;

            _selectedCourtFigure = value;
            OnPropertyChanged();
        }
    }

    public string SelectedOffice
    {
        get => _selectedOffice;
        set
        {
            if (_selectedOffice == value)
                return;

            _selectedOffice = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> WarStanceOptions { get; } =
        ["Defensive", "Balanced", "Aggressive"];

    public IReadOnlyList<string> PeaceTermOptions { get; } =
        ["WhitePeace", "DemandReparations", "OfferReparations"];

    public WarCampaignView? SelectedCampaign
    {
        get => _selectedCampaign;
        set
        {
            if (Equals(_selectedCampaign, value))
                return;

            _selectedCampaign = value;

            if (value is not null)
                SelectedWarStance = value.Stance;

            OnPropertyChanged();
        }
    }

    public string SelectedWarStance
    {
        get => _selectedWarStance;
        set
        {
            if (_selectedWarStance == value)
                return;

            _selectedWarStance = value;
            OnPropertyChanged();
        }
    }

    public string SelectedPeaceTerms
    {
        get => _selectedPeaceTerms;
        set
        {
            if (_selectedPeaceTerms == value)
                return;

            _selectedPeaceTerms = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> ArchiveCategoryOptions { get; } =
    [
        "All",
        "ADVISER REPORT",
        "ECONOMY",
        "MILITARY",
        "POLITICS",
        "DIPLOMACY",
        "ORDER",
        "PERSONAL",
        "SYSTEM"
    ];

    public IReadOnlyList<ArchiveEntryView> ArchiveEntries
    {
        get
        {
            IEnumerable<ArchiveEntryView> entries = View.Archive.Entries;

            if (!string.Equals(
                    SelectedArchiveCategory,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                entries = entries.Where(entry =>
                    string.Equals(
                        entry.Category,
                        SelectedArchiveCategory,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(ArchiveSearchText))
            {
                var search = ArchiveSearchText.Trim();

                entries = entries.Where(entry =>
                    entry.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.Details.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.Source.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    entry.Date.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return entries.ToList();
        }
    }

    public string ArchiveSearchText
    {
        get => _archiveSearchText;
        set
        {
            if (_archiveSearchText == value)
                return;

            _archiveSearchText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ArchiveEntries));
        }
    }

    public string SelectedArchiveCategory
    {
        get => _selectedArchiveCategory;
        set
        {
            if (_selectedArchiveCategory == value)
                return;

            _selectedArchiveCategory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ArchiveEntries));
        }
    }

    public double TargetTaxPercent
    {
        get => _targetTaxPercent;
        set
        {
            if (Math.Abs(_targetTaxPercent - value) < 0.01)
                return;

            _targetTaxPercent = value;
            OnPropertyChanged();
        }
    }

    public double TargetArmyFundingPercent
    {
        get => _targetArmyFundingPercent;
        set
        {
            if (Math.Abs(_targetArmyFundingPercent - value) < 0.01)
                return;

            _targetArmyFundingPercent = value;
            OnPropertyChanged();
        }
    }

    public double TargetAdministrationFundingPercent
    {
        get => _targetAdministrationFundingPercent;
        set
        {
            if (Math.Abs(_targetAdministrationFundingPercent - value) < 0.01)
                return;

            _targetAdministrationFundingPercent = value;
            OnPropertyChanged();
        }
    }

    public double TargetCourtFundingPercent
    {
        get => _targetCourtFundingPercent;
        set
        {
            if (Math.Abs(_targetCourtFundingPercent - value) < 0.01)
                return;

            _targetCourtFundingPercent = value;
            OnPropertyChanged();
        }
    }

    private void RunAndRefresh(Action action)
    {
        action();
        RefreshBindings();
    }

    private void RefreshBindings()
    {
        if (SelectedForeignCountry is null ||
            !View.ForeignCountries.Any(country =>
                country.Id == SelectedForeignCountry.Id))
        {
            SelectedForeignCountry = View.ForeignCountries.FirstOrDefault();
        }

        if (SelectedCourtFigure is null ||
            !View.Court.Figures.Any(figure =>
                figure.Id == SelectedCourtFigure.Id &&
                figure.IsAvailableForOffice))
        {
            SelectedCourtFigure = View.Court.Figures.FirstOrDefault(figure =>
                figure.IsAvailableForOffice);
        }
        else
        {
            SelectedCourtFigure = View.Court.Figures.First(figure =>
                figure.Id == SelectedCourtFigure.Id);
        }

        if (SelectedCampaign is null ||
            !View.Military.Campaigns.Any(campaign =>
                campaign.Id == SelectedCampaign.Id))
        {
            SelectedCampaign = View.Military.Campaigns.FirstOrDefault();
        }
        else
        {
            SelectedCampaign = View.Military.Campaigns.First(campaign =>
                campaign.Id == SelectedCampaign.Id);
        }

        OnPropertyChanged(nameof(View));
        OnPropertyChanged(nameof(ForeignCountries));
        OnPropertyChanged(nameof(SelectedForeignState));
        OnPropertyChanged(nameof(TradeActionLabel));
        OnPropertyChanged(nameof(AppointmentCandidates));
        OnPropertyChanged(nameof(SelectedCampaign));
        OnPropertyChanged(nameof(SelectedForeignCountry));
        OnPropertyChanged(nameof(SelectedCourtFigure));
        OnPropertyChanged(nameof(HasPendingReports));
        OnPropertyChanged(nameof(HasActiveCampaigns));
        OnPropertyChanged(nameof(ArchiveEntries));
        OnPropertyChanged(nameof(SaveLocation));
        _advanceMonthCommand.RaiseCanExecuteChanged();
        _loadCommand.RaiseCanExecuteChanged();
    }

    private void SyncPolicyTargets()
    {
        TargetTaxPercent = View.Government.TaxPercent;
        TargetArmyFundingPercent = View.Government.ArmyFundingPercent;
        TargetAdministrationFundingPercent =
            View.Government.AdministrationFundingPercent;
        TargetCourtFundingPercent = View.Government.CourtFundingPercent;
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
