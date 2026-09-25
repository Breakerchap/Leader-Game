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

    private readonly RelayCommand _advanceMonthCommand;

    public MainWindowViewModel()
    {
        _selectedForeignCountry = View.ForeignCountries.FirstOrDefault();

        _advanceMonthCommand = new RelayCommand(
            _ => RunAndRefresh(_session.AdvanceMonth),
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
            OnPropertyChanged(nameof(IsIntelligenceVisible));
            OnPropertyChanged(nameof(IsPlaceholderVisible));
            OnPropertyChanged(nameof(SectionTitle));
            OnPropertyChanged(nameof(SectionDescription));
        }
    }

    public bool IsBriefingVisible =>
        CurrentSection == "Briefing";

    public bool IsIntelligenceVisible =>
        CurrentSection == "Intelligence";

    public bool IsPlaceholderVisible =>
        !IsBriefingVisible && !IsIntelligenceVisible;

    public string SectionTitle => CurrentSection;

    public string SectionDescription => CurrentSection switch
    {
        "Government" =>
            "Policies, institutions, budgets and the machinery through which orders become state action.",
        "Court" =>
            "People, offices, personal relationships, political impressions, factions and patronage.",
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

    private void RunAndRefresh(Action action)
    {
        action();

        if (SelectedForeignCountry is null ||
            !View.ForeignCountries.Any(country =>
                country.Id == SelectedForeignCountry.Id))
        {
            SelectedForeignCountry = View.ForeignCountries.FirstOrDefault();
        }

        OnPropertyChanged(nameof(View));
        OnPropertyChanged(nameof(ForeignCountries));
        OnPropertyChanged(nameof(SelectedForeignCountry));
        _advanceMonthCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
