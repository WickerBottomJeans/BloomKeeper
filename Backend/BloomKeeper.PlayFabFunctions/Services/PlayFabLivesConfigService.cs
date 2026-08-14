using BloomKeeper.PlayFabFunctions.Models;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;

namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// [Duong] Load lives config from playfab
/// </summary>
public class PlayFabLivesConfigService
{
    private const string LivesConfigKey = "LivesConfig";
    private const string DeveloperSecretKeyEnvironmentVariable = "PLAYFAB_DEVELOPER_SECRET_KEY";

    /// <summary>
    /// [Duong] Load lives config from PlayFab
    /// </summary>
    public async Task<PlayerLivesConfig> Load(string titleId)
    {
        // [Duong] Safety checks
        if (string.IsNullOrWhiteSpace(titleId)) throw new InvalidOperationException("PlayFab title ID is missing.");
        string developerSecretKey = Environment.GetEnvironmentVariable(DeveloperSecretKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(developerSecretKey)) throw new InvalidOperationException($"Azure app setting {DeveloperSecretKeyEnvironmentVariable} is missing.");

        // [Duong] Request the live configs data from playfab
        var apiSettings = new PlayFabApiSettings { TitleId = titleId, DeveloperSecretKey = developerSecretKey };
        var serverApi = new PlayFabServerInstanceAPI(apiSettings);
        PlayFabResult<GetTitleDataResult> result = await serverApi.GetTitleInternalDataAsync(new GetTitleDataRequest { Keys = new List<string> { LivesConfigKey } });
        if (result.Error != null) throw new InvalidOperationException($"PlayFab failed to load {LivesConfigKey}: {result.Error.GenerateErrorReport()}");
        if (result.Result?.Data == null || !result.Result.Data.TryGetValue(LivesConfigKey, out string json) || string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException($"PlayFab Internal Title Data key {LivesConfigKey} is missing or empty.");

        // [Duong] Make a usable object from playfab's data
        PlayerLivesConfig config;
        try
        {
            config = JsonConvert.DeserializeObject<PlayerLivesConfig>(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"PlayFab Internal Title Data key {LivesConfigKey} contains invalid JSON.", exception);
        }
        Validate(config);
        return config;
    }

    private static void Validate(PlayerLivesConfig config)
    {
        if (config == null) throw new InvalidOperationException($"PlayFab Internal Title Data key {LivesConfigKey} contains invalid JSON.");
        if (config.schemaVersion != PlayerLivesConfig.CurrentSchemaVersion) throw new InvalidOperationException($"Lives config schema version {config.schemaVersion} is unsupported. Expected {PlayerLivesConfig.CurrentSchemaVersion}.");
        if (config.maximumLives <= 0) throw new InvalidOperationException("Lives config maximumLives must be greater than zero.");
        if (config.regenerationIntervalSeconds <= 0) throw new InvalidOperationException("Lives config regenerationIntervalSeconds must be greater than zero.");
    }
}
