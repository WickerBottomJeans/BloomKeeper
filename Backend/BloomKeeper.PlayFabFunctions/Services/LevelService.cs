using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    public async Task<string> Load(int levelId)
    {
        Uri levelUri = new Uri(remoteConfigBaseUri, $"levels/level_{levelId}.json");
        using HttpResponseMessage response = await HttpClient.GetAsync(levelUri);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Failed to load level {levelId} from {levelUri}. HTTP status: {(int)response.StatusCode} {response.StatusCode}.");

        return await response.Content.ReadAsStringAsync();
    }

    public static (bool published, int? nextLevelId) ReadLevelFields(int levelId, string json)
    {
        JObject level;
        try
        {
            level = JObject.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Level {levelId} contains invalid JSON.", exception);
        }

        int loadedLevelId = level.Value<int>("levelId");
        if (loadedLevelId != levelId) throw new InvalidOperationException($"Level file for {levelId} contains level ID {loadedLevelId}.");
        return (level.Value<bool>("published"), level.Value<int?>("nextLevelId"));
    }
}
