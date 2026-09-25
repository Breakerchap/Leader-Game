using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeaderGame.Simulation.Persistence;

public static partial class GameSaveService
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static string Serialize(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return JsonSerializer.Serialize(
            Capture(state),
            JsonOptions);
    }

    public static GameState Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var snapshot = JsonSerializer.Deserialize<GameSaveSnapshot>(
            json,
            JsonOptions) ?? throw new InvalidDataException(
                "The save file did not contain a valid game snapshot.");

        if (snapshot.Version != GameSaveSnapshot.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Unsupported save version {snapshot.Version}. " +
                $"This build supports version {GameSaveSnapshot.CurrentVersion}.");
        }

        return Restore(snapshot);
    }

    public static void SaveToFile(GameState state, string path)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = fullPath + ".tmp";
        File.WriteAllText(temporaryPath, Serialize(state));
        File.Move(temporaryPath, fullPath, overwrite: true);
    }

    public static GameState LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Deserialize(File.ReadAllText(Path.GetFullPath(path)));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
