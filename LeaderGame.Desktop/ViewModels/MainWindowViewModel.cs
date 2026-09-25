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
    private string _selectedOffice = "Chancellor";
    private double _targetTaxPercent;
    private double _targetArmyFundingPercent;
    private double _targetAdministrationFundingPercent;
    private double _targetCourtFundingPercent;

    private readonly RelayCommand _advanceMonthCommand;

    public MainWindowViewModel()
    {
        _selectedForeignCountry = View.ForeignCountries.FirstOrDefault();
        _selectedCourtFigure = View.Court.Figures.FirstOrDefault(figure =>
            figure.IsAvailableForOffice);

        SyncPolicyTargets();

        _advanceMonthCommand = new RelayCommand(
            _ =>
            {
                _session.AdvanceMonth();
                SyncPolicyTargets();
                RefreshBindings();
            },
            _ => !View.HasLost);

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
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PlayerViewState View => _session.View;

    public ICommand AdvanceMonthCommand => _advanceMonthCommand;

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
            OnPropertyChanged(nameof(IsIntelligenceVisible));
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

    public bool IsIntelligenceVisible => CurrentSection == "Intelligence";

    public bool IsPlaceholderVisible =>
        !IsBriefingVisible &&
        !IsGovernmentVisible &&
        !IsCourtVisible &&
        !IsIntelligenceVisible;

    public bool IsBriefingSelected => CurrentSection == "Briefing";
    public bool IsGovernmentSelected => CurrentSection == "Government";
    public bool IsCourtSelected => CurrentSection == "Court";
    public bool IsEconomySelected => CurrentSection == "Economy";
    public bool IsForeignAffairsSelected => CurrentSection == "Foreign Affairs";
    public bool IsMilitarySelected => CurrentSection == "Military";
    public bool IsIntelligenceSelected => CurrentSection == "Intelligence";
    public bool IsArchiveSelected => CurrentSection == "Archive";

    public bool HasPendingReports => View.PendingReports > 0;

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

        OnPropertyChanged(nameof(View));
        OnPropertyChanged(nameof(ForeignCountries));
        OnPropertyChanged(nameof(AppointmentCandidates));
        OnPropertyChanged(nameof(SelectedForeignCountry));
        OnPropertyChanged(nameof(SelectedCourtFigure));
        OnPropertyChanged(nameof(HasPendingReports));
        _advanceMonthCommand.RaiseCanExecuteChanged();
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
