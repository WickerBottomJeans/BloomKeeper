using System.Text;
using BloomKeeper.PlayFabFunctions.Models;
using Newtonsoft.Json;
using PlayFab.DataModels;

namespace BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;

public class LivesFileStore
{
    private const int CurrentSchemaVersion = 1;

    public string FileName => "lives.json";

    public async Task<(PlayerLivesData lives, bool fileExists)> Load(PlayFabEntityFileClient fileClient, GetFilesResponse fileMetadata, int maximumLives)
    {
        if (!fileClient.TryGetFileMetadata(fileMetadata, FileName, out GetFileMetadata livesFile)) return (CreateDefault(maximumLives), false);

        string json = await fileClient.DownloadText(livesFile);
        PlayerLivesData lives = JsonConvert.DeserializeObject<PlayerLivesData>(json);
        return (Validate(lives, maximumLives), true);
    }

    public PlayerLivesData CreateDefault(int maximumLives)
    {
        if (maximumLives <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLives), maximumLives, "Maximum lives must be positive.");
        return new PlayerLivesData { availableLives = maximumLives, regenerationAnchorUtc = null, unlimitedLivesExpiresAtUtc = null };
    }

    public byte[] Serialize(PlayerLivesData lives, int maximumLives)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Validate(lives, maximumLives)));
    }

    private PlayerLivesData Validate(PlayerLivesData lives, int maximumLives)
    {
        if (maximumLives <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLives), maximumLives, "Maximum lives must be positive.");
        if (lives == null) throw new InvalidOperationException("PlayFab lives file has invalid JSON.");
        if (lives.schemaVersion != CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab lives file has unsupported schema version: {lives.schemaVersion}.");
        if (lives.availableLives < 0 || lives.availableLives > maximumLives) throw new InvalidOperationException($"PlayFab lives file has invalid available lives: {lives.availableLives}.");
        if (lives.availableLives < maximumLives && !lives.regenerationAnchorUtc.HasValue) throw new InvalidOperationException("PlayFab lives file is missing its regeneration anchor while lives are below the cap.");
        if (lives.availableLives == maximumLives && lives.regenerationAnchorUtc.HasValue) throw new InvalidOperationException("PlayFab lives file has a regeneration anchor while lives are full.");
        return lives;
    }
}
