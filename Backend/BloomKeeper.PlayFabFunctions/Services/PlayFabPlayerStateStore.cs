using System.Text;
using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.DataModels;
using DataEntityKey = PlayFab.DataModels.EntityKey;

namespace BloomKeeper.PlayFabFunctions.Services;

public class PlayFabPlayerStateStore
{
    private const string ProgressionFileName = "progression.json";
    private const string LevelAttemptFileName = "level-attempt.json";
    private readonly PlayFabEntityFileClient fileClient = new PlayFabEntityFileClient();

    public async Task<PlayerProgressionData> LoadProgression(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity)
    {
        GetFilesResponse fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
        if (fileClient.TryGetFileMetadata(fileMetadata, ProgressionFileName, out GetFileMetadata progressionFile))
            return await DownloadProgression(progressionFile);

        var progression = new PlayerProgressionData();
        await SaveProgression(dataApi, dataEntity, progression, fileMetadata.ProfileVersion);
        return progression;
    }

    /// <returns>Tuple&lt;Progression, Level attempt, Profile version&gt;.</returns>
    public async Task<(PlayerProgressionData progression, LevelAttemptData levelAttempt, int profileVersion)> LoadPlayerStateForUpdate(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity)
    {
        GetFilesResponse fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
        Task<PlayerProgressionData> progressionTask = fileClient.TryGetFileMetadata(fileMetadata, ProgressionFileName, out GetFileMetadata progressionFile) ? DownloadProgression(progressionFile) : Task.FromResult(new PlayerProgressionData());
        Task<LevelAttemptData> levelAttemptTask = fileClient.TryGetFileMetadata(fileMetadata, LevelAttemptFileName, out GetFileMetadata levelAttemptFile) ? DownloadLevelAttempt(levelAttemptFile) : Task.FromResult<LevelAttemptData>(null);
        await Task.WhenAll(progressionTask, levelAttemptTask);
        return (await progressionTask, await levelAttemptTask, fileMetadata.ProfileVersion);
    }

    public async Task SaveLevelAttempt(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity, LevelAttemptData levelAttempt, int profileVersion)
    {
        ValidateLevelAttempt(levelAttempt);
        await fileClient.UploadFile(dataApi, dataEntity, LevelAttemptFileName, Serialize(levelAttempt), profileVersion);
    }

    public async Task SaveProgressionAndLevelAttempt(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity, PlayerProgressionData progression, LevelAttemptData levelAttempt, int profileVersion)
    {
        ValidateProgression(progression);
        ValidateLevelAttempt(levelAttempt);
        var files = new Dictionary<string, byte[]>
        {
            { ProgressionFileName, Serialize(progression) },
            { LevelAttemptFileName, Serialize(levelAttempt) }
        };
        await fileClient.UploadFiles(dataApi, dataEntity, files, profileVersion);
    }

    private async Task SaveProgression(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity, PlayerProgressionData progression, int profileVersion)
    {
        ValidateProgression(progression);
        await fileClient.UploadFile(dataApi, dataEntity, ProgressionFileName, Serialize(progression), profileVersion);
    }

    private async Task<PlayerProgressionData> DownloadProgression(GetFileMetadata progressionFile)
    {
        string json = await fileClient.DownloadText(progressionFile);
        PlayerProgressionData progression = JsonConvert.DeserializeObject<PlayerProgressionData>(json);
        return ValidateProgression(progression);
    }

    private async Task<LevelAttemptData> DownloadLevelAttempt(GetFileMetadata levelAttemptFile)
    {
        string json = await fileClient.DownloadText(levelAttemptFile);
        LevelAttemptData levelAttempt = JsonConvert.DeserializeObject<LevelAttemptData>(json);
        return ValidateLevelAttempt(levelAttempt);
    }

    private  PlayerProgressionData ValidateProgression(PlayerProgressionData progression)
    {
        if (progression == null) throw new InvalidOperationException("PlayFab progression file has invalid JSON.");
        if (progression.schemaVersion <= 0) throw new InvalidOperationException($"PlayFab progression file has invalid schema version: {progression.schemaVersion}.");
        if (progression.levels == null) throw new InvalidOperationException("PlayFab progression file has no level data dictionary.");
        return progression;
    }

    private  LevelAttemptData ValidateLevelAttempt(LevelAttemptData levelAttempt)
    {
        if (levelAttempt == null) throw new InvalidOperationException("PlayFab level-attempt file has invalid JSON.");
        if (levelAttempt.schemaVersion != LevelAttemptContract.CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab level-attempt file has unsupported schema version: {levelAttempt.schemaVersion}.");
        if (!Guid.TryParseExact(levelAttempt.attemptId, "N", out _)) throw new InvalidOperationException("PlayFab level-attempt file has an invalid attempt ID.");
        if (!Guid.TryParseExact(levelAttempt.startOperationId, "N", out _)) throw new InvalidOperationException("PlayFab level-attempt file has an invalid start operation ID.");
        if (levelAttempt.status != LevelAttemptStatus.Active && levelAttempt.status != LevelAttemptStatus.Completed && levelAttempt.status != LevelAttemptStatus.Abandoned) throw new InvalidOperationException($"PlayFab level-attempt file has unsupported status {levelAttempt.status}.");
        if (levelAttempt.status == LevelAttemptStatus.Completed && (!levelAttempt.didWin.HasValue || !levelAttempt.score.HasValue || !levelAttempt.stars.HasValue)) throw new InvalidOperationException("PlayFab completed level attempt has no saved result.");
        if (levelAttempt.status != LevelAttemptStatus.Completed && (levelAttempt.didWin.HasValue || levelAttempt.score.HasValue || levelAttempt.stars.HasValue)) throw new InvalidOperationException("PlayFab non-completed level attempt contains a saved result.");
        return levelAttempt;
    }

    private  byte[] Serialize(object value)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
    }
}
