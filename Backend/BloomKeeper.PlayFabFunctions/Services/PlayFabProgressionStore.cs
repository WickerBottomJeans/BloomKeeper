using BloomKeeper.PlayFabFunctions.Models;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.DataModels;
using DataEntityKey = PlayFab.DataModels.EntityKey;

namespace BloomKeeper.PlayFabFunctions.Services;

public class PlayFabProgressionStore
{
    private const string ProgressionFileName = "progression.json";
    private readonly PlayFabEntityFileClient fileClient = new PlayFabEntityFileClient();

    public async Task<PlayerProgressionData> LoadProgression(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity)
    {
        GetFilesResponse fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
        if (fileClient.TryGetFileMetadata(fileMetadata, ProgressionFileName, out GetFileMetadata progressionFile))
            return await DownloadProgression(progressionFile);

        PlayerProgressionData newProgression = new PlayerProgressionData();
        await UploadProgression(dataApi, dataEntity, newProgression, fileMetadata.ProfileVersion);
        return newProgression;
    }

    public async Task<(PlayerProgressionData progression, int profileVersion)> LoadProgressionForUpdate(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity)
    {
        GetFilesResponse fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
        if (fileClient.TryGetFileMetadata(fileMetadata, ProgressionFileName, out GetFileMetadata progressionFile))
            return (await DownloadProgression(progressionFile), fileMetadata.ProfileVersion);

        return (new PlayerProgressionData(), fileMetadata.ProfileVersion);
    }

    public async Task SaveProgression(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity, PlayerProgressionData progression, int profileVersion)
    {
        await UploadProgression(dataApi, dataEntity, progression, profileVersion);
    }

    private async Task<PlayerProgressionData> DownloadProgression(GetFileMetadata progressionFile)
    {
        string json = await fileClient.DownloadText(progressionFile);
        return CreateProgressionFromJson(json);
    }

    private static PlayerProgressionData CreateProgressionFromJson(string json)
    {
        PlayerProgressionData progression = JsonConvert.DeserializeObject<PlayerProgressionData>(json);
        return ValidateProgression(progression);
    }

    private static PlayerProgressionData ValidateProgression(PlayerProgressionData progression)
    {
        if (progression == null) throw new InvalidOperationException("PlayFab progression file has invalid JSON.");
        if (progression.schemaVersion <= 0) throw new InvalidOperationException($"PlayFab progression file has invalid schema version: {progression.schemaVersion}.");
        if (progression.levels == null) throw new InvalidOperationException("PlayFab progression file has no level data map.");
        if (progression.processedLevelAttempts == null) throw new InvalidOperationException("PlayFab progression file has no processed level attempt map.");

        return progression;
    }

    private async Task UploadProgression(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity, PlayerProgressionData progression, int profileVersion)
    {
        string json = JsonConvert.SerializeObject(progression);
        await fileClient.UploadFile(dataApi, dataEntity, ProgressionFileName, System.Text.Encoding.UTF8.GetBytes(json), profileVersion);
    }
}
