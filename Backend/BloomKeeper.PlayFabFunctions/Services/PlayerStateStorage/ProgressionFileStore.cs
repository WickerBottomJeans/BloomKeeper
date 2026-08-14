using System.Text;
using DefaultNamespace;
using Newtonsoft.Json;
using PlayFab.DataModels;

namespace BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;

public class ProgressionFileStore
{
    public string FileName => "progression.json";

    public async Task<(PlayerProgressionData progression, bool fileExists)> Load(PlayFabEntityFileClient fileClient, GetFilesResponse fileMetadata)
    {
        if (!fileClient.TryGetFileMetadata(fileMetadata, FileName, out GetFileMetadata progressionFile)) return (CreateDefault(), false);

        string json = await fileClient.DownloadText(progressionFile);
        PlayerProgressionData progression = JsonConvert.DeserializeObject<PlayerProgressionData>(json);
        return (Validate(progression), true);
    }

    public PlayerProgressionData CreateDefault()
    {
        return new PlayerProgressionData();
    }

    public byte[] Serialize(PlayerProgressionData progression)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Validate(progression)));
    }

    private PlayerProgressionData Validate(PlayerProgressionData progression)
    {
        if (progression == null) throw new InvalidOperationException("PlayFab progression file has invalid JSON.");
        if (progression.schemaVersion <= 0) throw new InvalidOperationException($"PlayFab progression file has invalid schema version: {progression.schemaVersion}.");
        if (progression.levels == null) throw new InvalidOperationException("PlayFab progression file has no level data dictionary.");
        return progression;
    }
}
