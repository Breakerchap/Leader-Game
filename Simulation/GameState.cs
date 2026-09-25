using LeaderGame.Simulation.Characters;
using LeaderGame.Simulation.Countries;
using LeaderGame.Simulation.Diplomacy;
using LeaderGame.Simulation.Military;
using LeaderGame.Simulation.Information;
using LeaderGame.Simulation.Orders;
using LeaderGame.Simulation.Player;
using LeaderGame.Simulation.Politics;
using LeaderGame.Simulation.Randomness;
using LeaderGame.Simulation.Reports;

namespace LeaderGame.Simulation;

public class GameState
{
    public GameDate Date { get; set; }

    public List<Country> Countries { get; } = [];

    public List<Order> PendingOrders { get; } = [];

    public List<PoliticalPlot> Plots { get; } = [];

    public List<PowerBaseDemand> PowerBaseDemands { get; } = [];

    public List<PoliticalBloc> PoliticalBlocs { get; } = [];

    public List<CabinetProposal> CabinetProposals { get; } = [];

    public PlayerKnowledge Knowledge { get; } = new();

    public List<AdvisorIntelligenceReport> AdvisorReports { get; } = [];

    public List<InformationRequest> InformationRequests { get; } = [];

    public List<InformationTruthSnapshot> InformationHistory { get; } = [];

    public List<DiplomaticProposal> DiplomaticProposals { get; } = [];

    public List<War> Wars { get; } = [];

    public List<SimulationReport> Reports { get; } = [];

    public RelationshipGraph Relationships { get; } = new();

    public DiplomaticGraph Diplomacy { get; } = new();

    public IRandomSource Random { get; set; } = new SimulationRandom(42);

    /// <summary>
    /// Separate deterministic random stream for uncertainty, misinformation and
    /// reporting behaviour. Information mechanics must not perturb physical
    /// simulation outcomes such as battles, illness or plots.
    /// </summary>
    public IRandomSource InformationRandom { get; set; } =
        new SimulationRandom(0x494E464FUL);

    public required PlayerState Player { get; set; }

    public Country? FindCountry(string id) =>
        Countries.FirstOrDefault(country => country.Id == id);
}
