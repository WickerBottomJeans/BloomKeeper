using System.Text;
using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;
using Newtonsoft.Json;
using PlayFab.DataModels;

namespace BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;

public class LevelAttemptFileStore
{
    public string FileName => "level-attempt.json";

    public async Task<(LevelAttemptData levelAttempt, bool fileExists)> Load(PlayFabEntityFileClient fileClient, GetFilesResponse fileMetadata)
    {
        if (!fileClient.TryGetFileMetadata(fileMetadata, FileName, out GetFileMetadata levelAttemptFile)) return (CreateDefault(), false);

        string json = await fileClient.DownloadText(levelAttemptFile);
        LevelAttemptData levelAttempt = JsonConvert.DeserializeObject<LevelAttemptData>(json);
        return (Validate(levelAttempt), true);
    }

    public LevelAttemptData CreateDefault()
    {
        return null;
    }

    public byte[] Serialize(LevelAttemptData levelAttempt)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Validate(levelAttempt)));
    }

    private LevelAttemptData Validate(LevelAttemptData levelAttempt)
    {
        if (levelAttempt == null) throw new InvalidOperationException("PlayFab level-attempt file has invalid JSON.");
        if (levelAttempt.schemaVersion != LevelAttemptContract.CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab level-attempt file has unsupported schema version: {levelAttempt.schemaVersion}.");
        if (!Guid.TryParseExact(levelAttempt.attemptId, "N", out _)) throw new InvalidOperationException("PlayFab level-attempt file has an invalid attempt ID.");
        if (!Guid.TryParseExact(levelAttempt.startLevelRequestIdempotencyKey, "N", out _)) throw new InvalidOperationException("PlayFab level-attempt file has an invalid start level request idempotency key.");
        if (levelAttempt.status != LevelAttemptStatus.Active && levelAttempt.status != LevelAttemptStatus.Completed && levelAttempt.status != LevelAttemptStatus.Abandoned) throw new InvalidOperationException($"PlayFab level-attempt file has unsupported status {levelAttempt.status}.");
        if (levelAttempt.status == LevelAttemptStatus.Completed && (!levelAttempt.didWin.HasValue || !levelAttempt.score.HasValue || !levelAttempt.stars.HasValue)) throw new InvalidOperationException("PlayFab completed level attempt has no saved result.");
        if (levelAttempt.status != LevelAttemptStatus.Completed && (levelAttempt.didWin.HasValue || levelAttempt.score.HasValue || levelAttempt.stars.HasValue)) throw new InvalidOperationException("PlayFab non-completed level attempt contains a saved result.");
        return levelAttempt;
    }
}
