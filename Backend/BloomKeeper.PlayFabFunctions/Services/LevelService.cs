using System.Net;
using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Services;

public class LevelService
{
    private const string RemoteConfigBaseUrlEnvironmentVariable = "REMOTE_CONFIG_BASE_URL";
    private static readonly HttpClient HttpClient = new HttpClient();
    private readonly Uri remoteConfigBaseUri;

    public LevelService()
    {
        string remoteConfigBaseUrl = Environment.GetEnvironmentVariable(RemoteConfigBaseUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(remoteConfigBaseUrl)) throw new InvalidOperationException($"Azure app setting {RemoteConfigBaseUrlEnvironmentVariable} is missing.");
        if (!Uri.TryCreate(remoteConfigBaseUrl, UriKind.Absolute, out Uri parsedRemoteConfigBaseUri)) throw new InvalidOperationException($"Azure app setting {RemoteConfigBaseUrlEnvironmentVariable} must be an absolute URL.");
        remoteConfigBaseUri = parsedRemoteConfigBaseUri.AbsoluteUri.EndsWith('/') ? parsedRemoteConfigBaseUri : new Uri($"{parsedRemoteConfigBaseUri.AbsoluteUri}/");
    }

    public async Task<LevelData> Load(int levelId)
    {
        Uri levelUri = new Uri(remoteConfigBaseUri, $"levels/level_{levelId}.json");
        using HttpResponseMessage response = await HttpClient.GetAsync(levelUri);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Failed to load level {levelId} from {levelUri}. HTTP status: {(int)response.StatusCode} {response.StatusCode}.");

        string json = await response.Content.ReadAsStringAsync();
        LevelData level;
        try
        {
            level = JsonConvert.DeserializeObject<LevelData>(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Level {levelId} at {levelUri} contains invalid JSON.", exception);
        }

        if (level == null) throw new InvalidOperationException($"Level {levelId} at {levelUri} contains invalid JSON.");
        if (level.levelId != levelId) throw new InvalidOperationException($"Level file for {levelId} contains level ID {level.levelId}.");
        return level;
    }

    public static bool IsLevelAvailable(LevelData level)
    {
        return level != null && level.published;
    }

    public static bool IsLevelUnlocked(PlayerProgressionData progression, LevelData level)
    {
        if (progression == null) throw new ArgumentNullException(nameof(progression));
        if (level == null) throw new ArgumentNullException(nameof(level));
        return level.levelId <= progression.highestUnlockedLevel;
    }

    public static int? GetNextLevelId(LevelData level, int completedLevelId)
    {
        if (level == null) throw new InvalidOperationException($"Published level {completedLevelId} is missing from remote Config.");
        if (level.levelId != completedLevelId) throw new InvalidOperationException($"Loaded level {level.levelId} does not match completed level {completedLevelId}.");
        return level.nextLevelId;
    }
}
