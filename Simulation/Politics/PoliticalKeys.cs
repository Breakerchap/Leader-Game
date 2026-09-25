namespace LeaderGame.Simulation.Politics;

public static class PoliticalKeys
{
    public static string Country(string countryId) => $"country:{countryId}";

    public static string Lineage(string lineageId) => $"lineage:{lineageId}";
}
