using System.Text;
using BloomKeeper.PlayFabFunctions.Models;
using Newtonsoft.Json;
using PlayFab.DataModels;

namespace BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;

public class PendingRewardsFileStore
{
    public string FileName => "pending-rewards.json";

    public async Task<(PendingRewardsData pendingRewards, bool fileExists)> Load(PlayFabEntityFileClient fileClient, GetFilesResponse fileMetadata)
    {
        if (!fileClient.TryGetFileMetadata(fileMetadata, FileName, out GetFileMetadata pendingRewardsFile)) return (CreateDefault(), false);

        string json = await fileClient.DownloadText(pendingRewardsFile);
        PendingRewardsData pendingRewards = JsonConvert.DeserializeObject<PendingRewardsData>(json);
        return (Validate(pendingRewards), true);
    }

    public PendingRewardsData CreateDefault()
    {
        return new PendingRewardsData();
    }

    public byte[] Serialize(PendingRewardsData pendingRewards)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Validate(pendingRewards)));
    }

    private PendingRewardsData Validate(PendingRewardsData pendingRewards)
    {
        if (pendingRewards == null) throw new InvalidOperationException("PlayFab pending-rewards file has invalid JSON.");
        if (pendingRewards.schemaVersion != PendingRewardsData.CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab pending-rewards file has unsupported schema version: {pendingRewards.schemaVersion}.");
        if (pendingRewards.batches == null) throw new InvalidOperationException("PlayFab pending-rewards file has no batches collection.");

        var observedRewardBatchIds = new HashSet<string>();
        foreach (PendingRewardBatch batch in pendingRewards.batches)
        {
            if (batch == null) throw new InvalidOperationException("PlayFab pending-rewards file contains a null batch.");
            if (string.IsNullOrWhiteSpace(batch.rewardBatchId)) throw new InvalidOperationException("PlayFab pending-rewards file contains a batch without an ID.");
            if (!observedRewardBatchIds.Add(batch.rewardBatchId)) throw new InvalidOperationException($"PlayFab pending-rewards file contains duplicate batch ID {batch.rewardBatchId}.");
            if (batch.rewardRolls == null) throw new InvalidOperationException($"PlayFab pending-rewards batch {batch.rewardBatchId} has no reward rolls collection.");
        }

        return pendingRewards;
    }
}
