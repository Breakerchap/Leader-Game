namespace LeaderGame.Simulation.Reports;

public enum ReportCategory
{
    Order,
    Economy,
    Politics,
    Personal,
    System
}

public sealed record SimulationReport(
    GameDate Date,
    ReportCategory Category,
    string Title,
    string Details);
