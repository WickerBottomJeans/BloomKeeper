using Azure;
using Azure.Data.Tables;
using BloomKeeper.PlayFabFunctions.Models;
using Newtonsoft.Json;
using System.Net;

namespace BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;

/// <summary>
/// Stores each player's current shop purchase saga state.
/// </summary>
public class ShopPurchaseStore
{
    private const string StorageConnectionStringEnvironmentVariable = "AzureWebJobsStorage";
    private const string TableName = "ShopPurchases";
    private const string PurchaseRowKey = "current-purchase";
    private const string PurchaseDataPropertyName = "PurchaseData";

    private readonly TableClient shopPurchaseTableClient;

    /// <summary>
    /// [Duong] Connects to the shop purchase table and creates it when missing.
    /// </summary>
    public ShopPurchaseStore()
    {
        string storageConnectionString = Environment.GetEnvironmentVariable(StorageConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(storageConnectionString)) throw new InvalidOperationException($"{StorageConnectionStringEnvironmentVariable} is missing.");

        shopPurchaseTableClient = new TableClient(storageConnectionString, TableName);
        shopPurchaseTableClient.CreateIfNotExists();
    }

    /// <summary>
    /// [Duong] Loads the player's purchase data and ETag, or returns null when the row does not exist.
    /// </summary>
    public async Task<(ShopPurchaseData shopPurchaseData, ETag shopPurchaseETag)?> LoadPurchase(string playerEntityType, string playerEntityId)
    {
        string playerPartitionKey = CreatePlayerPartitionKey(playerEntityType, playerEntityId);
        NullableResponse<TableEntity> response = await shopPurchaseTableClient.GetEntityIfExistsAsync<TableEntity>(playerPartitionKey, PurchaseRowKey);
        return response.HasValue ? (DeserializeShopPurchaseData(response.Value), response.Value.ETag) : null;
    }

    /// <summary>
    /// [Duong] Creates the player's purchase row.
    /// </summary>
    /// <returns>The created row's ETag, or null if the row already exists.</returns>
    public async Task<ETag?> TryCreatePurchase(string playerEntityType, string playerEntityId, ShopPurchaseData shopPurchaseData)
    {
        string playerPartitionKey = CreatePlayerPartitionKey(playerEntityType, playerEntityId);
        ShopPurchaseData.ValidateShopPurchaseData(shopPurchaseData);
        try
        {
            Response response = await shopPurchaseTableClient.AddEntityAsync(CreatePurchaseTableEntity(playerPartitionKey, shopPurchaseData));
            return response.Headers.ETag ?? throw new InvalidOperationException("Azure did not return an ETag for the created purchase row.");
        }
        catch (RequestFailedException exception) when (exception.Status == (int)HttpStatusCode.Conflict)
        {
            //[Duong] Row already exists
            return null;
        }
    }

    /// <summary>
    /// [Duong] Replaces the player's purchase row only if its ETag still matches.
    /// </summary>
    /// <returns>The updated row's ETag.</returns>
    public async Task<ETag> UpdatePurchase(string playerEntityType, string playerEntityId, ShopPurchaseData shopPurchaseData, ETag expectedShopPurchaseETag)
    {
        string playerPartitionKey = CreatePlayerPartitionKey(playerEntityType, playerEntityId);
        ShopPurchaseData.ValidateShopPurchaseData(shopPurchaseData);

        if (expectedShopPurchaseETag == ETag.All || expectedShopPurchaseETag.Equals(default(ETag))) throw new ArgumentException("A specific purchase row ETag is required.", nameof(expectedShopPurchaseETag));

        TableEntity updatedPurchaseEntity = CreatePurchaseTableEntity(playerPartitionKey, shopPurchaseData);
        Response response = await shopPurchaseTableClient.UpdateEntityAsync(updatedPurchaseEntity, expectedShopPurchaseETag, TableUpdateMode.Replace);
        return response.Headers.ETag ?? throw new InvalidOperationException("Azure did not return an ETag for the updated purchase row.");
    }

    /// <summary>
    /// [Duong] Creates one player's table partition key.
    /// </summary>
    private static string CreatePlayerPartitionKey(string playerEntityType, string playerEntityId)
    {
        if (string.IsNullOrWhiteSpace(playerEntityType)) throw new ArgumentException("Player entity type is missing.", nameof(playerEntityType));
        if (string.IsNullOrWhiteSpace(playerEntityId)) throw new ArgumentException("Player entity ID is missing.", nameof(playerEntityId));
        return $"{playerEntityType}:{playerEntityId}";
    }

    /// <summary>
    /// Creates the player's current serialized purchase row.
    /// </summary>
    private static TableEntity CreatePurchaseTableEntity(string playerPartitionKey, ShopPurchaseData shopPurchaseData)
    {
        return new TableEntity(playerPartitionKey, PurchaseRowKey)
        {
            [PurchaseDataPropertyName] = JsonConvert.SerializeObject(shopPurchaseData)
        };
    }

    /// <summary>
    /// [Duong] Deserializes and validates one purchase table row.
    /// </summary>
    private static ShopPurchaseData DeserializeShopPurchaseData(TableEntity purchaseEntity)
    {
        if (!purchaseEntity.TryGetValue(PurchaseDataPropertyName, out object purchaseDataValue) || purchaseDataValue is not string purchaseDataJson || string.IsNullOrWhiteSpace(purchaseDataJson)) throw new InvalidOperationException("Azure shop purchase row has no purchase data.");

        ShopPurchaseData shopPurchaseData = JsonConvert.DeserializeObject<ShopPurchaseData>(purchaseDataJson);
        ShopPurchaseData.ValidateShopPurchaseData(shopPurchaseData);
        return shopPurchaseData;
    }
}
