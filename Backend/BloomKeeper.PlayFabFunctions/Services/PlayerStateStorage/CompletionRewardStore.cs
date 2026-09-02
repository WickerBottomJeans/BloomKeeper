using Azure;
using Azure.Data.Tables;
using BloomKeeper.PlayFabFunctions.Models;
using Newtonsoft.Json;
using System.Net;

namespace BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;

/// <summary>
/// Stores one completion reward saga row per winning level attempt.
/// </summary>
public class CompletionRewardStore
{
    private const string StorageConnectionStringEnvironmentVariable = "AzureWebJobsStorage";
    private const string TableName = "CompletionRewards";
    private const string CompletionRewardDataPropertyName = "CompletionRewardData";

    private readonly TableClient completionRewardTableClient;

    public CompletionRewardStore()
    {
        // Load the Azure Storage connection.
        string storageConnectionString = Environment.GetEnvironmentVariable(StorageConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(storageConnectionString)) throw new InvalidOperationException($"{StorageConnectionStringEnvironmentVariable} is missing.");

        // Connect to the completion reward table.
        completionRewardTableClient = new TableClient(storageConnectionString, TableName);
        completionRewardTableClient.CreateIfNotExists();
    }

    /// <summary>
    /// [Duong] Get completion reward data from player
    /// </summary>
    public async Task<(CompletionRewardData completionRewardData, ETag completionRewardETag)?> LoadCompletionReward(string playerEntityType, string playerEntityId, string levelAttemptId)
    {
        // Build and validate the row identity.
        string playerPartitionKey = CreatePlayerPartitionKey(playerEntityType, playerEntityId);
        ValidateLevelAttemptId(levelAttemptId);

        // Load the saga row with its concurrency token.
        NullableResponse<TableEntity> response = await completionRewardTableClient.GetEntityIfExistsAsync<TableEntity>(playerPartitionKey, levelAttemptId);
        return response.HasValue ? (DeserializeCompletionRewardData(response.Value), response.Value.ETag) : null;
    }

    /// <summary>
    /// [Duong] Inserts the completion reward row into Azure Table Storage, or returns null if it already exists.
    /// </summary>
    public async Task<ETag?> TryInsertCompletionReward(string playerEntityType, string playerEntityId, CompletionRewardData completionRewardData)
    {
        // [Duong] Validate the new saga row.
        string playerPartitionKey = CreatePlayerPartitionKey(playerEntityType, playerEntityId);
        CompletionRewardData.ValidateCompletionRewardData(completionRewardData);

        // [Duong] Create the level attempt row once.
        try
        {
            Response response = await completionRewardTableClient.AddEntityAsync(CreateCompletionRewardTableEntity(playerPartitionKey, completionRewardData));
            return response.Headers.ETag ?? throw new InvalidOperationException("Azure did not return an ETag for the created completion reward row.");
        }
        catch (RequestFailedException exception) when (exception.Status == (int)HttpStatusCode.Conflict)
        {
            return null;
        }
    }

    public async Task<ETag> UpdateCompletionReward(string playerEntityType, string playerEntityId, CompletionRewardData completionRewardData, ETag expectedCompletionRewardETag)
    {
        // Validate the replacement and its expected version.
        string playerPartitionKey = CreatePlayerPartitionKey(playerEntityType, playerEntityId);
        CompletionRewardData.ValidateCompletionRewardData(completionRewardData);
        if (expectedCompletionRewardETag == ETag.All || expectedCompletionRewardETag.Equals(default(ETag))) throw new ArgumentException("A specific completion reward row ETag is required.", nameof(expectedCompletionRewardETag));

        // Replace only the matching saga version.
        TableEntity updatedCompletionRewardEntity = CreateCompletionRewardTableEntity(playerPartitionKey, completionRewardData);
        Response response = await completionRewardTableClient.UpdateEntityAsync(updatedCompletionRewardEntity, expectedCompletionRewardETag, TableUpdateMode.Replace);
        return response.Headers.ETag ?? throw new InvalidOperationException("Azure did not return an ETag for the updated completion reward row.");
    }

    private static string CreatePlayerPartitionKey(string playerEntityType, string playerEntityId)
    {
        if (string.IsNullOrWhiteSpace(playerEntityType)) throw new ArgumentException("Player entity type is missing.", nameof(playerEntityType));
        if (string.IsNullOrWhiteSpace(playerEntityId)) throw new ArgumentException("Player entity ID is missing.", nameof(playerEntityId));
        return $"{playerEntityType}:{playerEntityId}";
    }

    private static TableEntity CreateCompletionRewardTableEntity(string playerPartitionKey, CompletionRewardData completionRewardData)
    {
        // Store the attempt ID as the row key and the saga as JSON.
        return new TableEntity(playerPartitionKey, completionRewardData.levelAttemptId) { [CompletionRewardDataPropertyName] = JsonConvert.SerializeObject(completionRewardData) };
    }

    private static CompletionRewardData DeserializeCompletionRewardData(TableEntity completionRewardEntity)
    {
        // Read the serialized saga payload.
        if (!completionRewardEntity.TryGetValue(CompletionRewardDataPropertyName, out object completionRewardDataValue) || completionRewardDataValue is not string completionRewardDataJson || string.IsNullOrWhiteSpace(completionRewardDataJson)) throw new InvalidOperationException("Azure completion reward row has no completion reward data.");

        // Validate the payload against its row identity.
        CompletionRewardData completionRewardData = JsonConvert.DeserializeObject<CompletionRewardData>(completionRewardDataJson);
        CompletionRewardData.ValidateCompletionRewardData(completionRewardData);
        if (completionRewardData.levelAttemptId != completionRewardEntity.RowKey) throw new InvalidOperationException("Azure completion reward row key does not match its level attempt ID.");
        return completionRewardData;
    }

    private static void ValidateLevelAttemptId(string levelAttemptId)
    {
        // Require the canonical attempt row key.
        if (!Guid.TryParseExact(levelAttemptId, "N", out _)) throw new ArgumentException("Level attempt ID must be a canonical GUID.", nameof(levelAttemptId));
    }
}
