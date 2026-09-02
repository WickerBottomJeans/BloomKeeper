using Azure;
using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using BloomKeeper.PlayFabFunctions.Services.ShopGrants;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System.Net;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class BuyShopOfferFunction
{
    private const int MaxPurchaseProgressWriteAttempts = 3;
    private const int InitialPurchaseProgressRetryDelayMilliseconds = 100;

    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly ShopConfigService shopConfigService = new ShopConfigService();
    private readonly ShopPurchaseStore shopPurchaseStore;
    private readonly PlayFabCurrencyService playFabCurrencyService = new PlayFabCurrencyService();
    private readonly PlayFabInventoryService playFabInventoryService = new PlayFabInventoryService();
    private readonly PlayFabLivesConfigService playFabLivesConfigService = new PlayFabLivesConfigService();
    private readonly PlayFabEntityFileClient playFabEntityFileClient = new PlayFabEntityFileClient();
    private readonly LivesFileStore livesFileStore = new LivesFileStore();
    private readonly LivesService livesService = new LivesService();
    private readonly ShopGrantDispatcher shopGrantDispatcher;
    
    public BuyShopOfferFunction(ShopGrantDispatcher shopGrantDispatcher, ShopPurchaseStore shopPurchaseStore)
    {
        this.shopGrantDispatcher = shopGrantDispatcher ?? throw new ArgumentNullException(nameof(shopGrantDispatcher));
        this.shopPurchaseStore = shopPurchaseStore ?? throw new ArgumentNullException(nameof(shopPurchaseStore));
    }

    /// <summary>
    /// Validates the purchase request and loads its purchasable offer.
    /// </summary>
    [Function("BuyShopOffer")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        // [Duong] Prepare the purchase inputs
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        BuyShopOfferRequest buyShopOfferRequest = ValidateBuyShopOfferRequest(contextReader.GetFunctionArgument<BuyShopOfferRequest>(context));
        ShopOfferConfig shopOfferConfig = await shopConfigService.LoadPurchasableOffer(buyShopOfferRequest.shopId, buyShopOfferRequest.offerId);
        var callerDataEntity = contextReader.GetCallerEntity(context);

        // [Duong] Load the player's existing purchase.
        (ShopPurchaseData shopPurchaseData, ETag shopPurchaseETag)? storedShopPurchaseData = await shopPurchaseStore.LoadPurchase(callerDataEntity.Type, callerDataEntity.Id);
        ShopPurchaseData existingShopPurchaseData = storedShopPurchaseData?.shopPurchaseData;
        if (existingShopPurchaseData != null)
        {
            // [Duong] Reject requests while a purchase is unfinished.
            if (existingShopPurchaseData.status == ShopPurchaseStatus.Pending)
            {
                return CreateBuyShopOfferJsonResult(new BuyShopOfferResponse { schemaVersion = ShopContract.CurrentSchemaVersion, outcome = BuyShopOfferOutcome.Rejected, rejectionReason = BuyShopOfferRejectionReason.UnfinishedPurchase });
            }

            // [Duong] Return the result if this purchase request was already completed; don't charge or grant again.
            if (existingShopPurchaseData.buyShopOfferIdempotencyKey == buyShopOfferRequest.buyShopOfferIdempotencyKey) return CreateBuyShopOfferJsonResult(await CreateCompletedPurchaseResponse(context, existingShopPurchaseData));
        }

        // [Duong] Create the current purchase state.
        var shopPurchaseData = new ShopPurchaseData(buyShopOfferRequest.buyShopOfferIdempotencyKey, shopOfferConfig.cost, shopOfferConfig.grants);
        ETag shopPurchaseETag;
        if (existingShopPurchaseData == null)
        {
            ETag? createdShopPurchaseETag = await shopPurchaseStore.TryInsertPurchase(callerDataEntity.Type, callerDataEntity.Id, shopPurchaseData);
            if (!createdShopPurchaseETag.HasValue)
            {
                storedShopPurchaseData = await shopPurchaseStore.LoadPurchase(callerDataEntity.Type, callerDataEntity.Id);
                existingShopPurchaseData = storedShopPurchaseData?.shopPurchaseData;
                if (existingShopPurchaseData != null && existingShopPurchaseData.status == ShopPurchaseStatus.Completed && existingShopPurchaseData.buyShopOfferIdempotencyKey == buyShopOfferRequest.buyShopOfferIdempotencyKey) return CreateBuyShopOfferJsonResult(await CreateCompletedPurchaseResponse(context, existingShopPurchaseData));
                if (existingShopPurchaseData != null && existingShopPurchaseData.status == ShopPurchaseStatus.Pending) return CreateBuyShopOfferJsonResult(new BuyShopOfferResponse { schemaVersion = ShopContract.CurrentSchemaVersion, outcome = BuyShopOfferOutcome.Rejected, rejectionReason = BuyShopOfferRejectionReason.UnfinishedPurchase });
                throw new InvalidOperationException("Shop purchase row creation conflicted, but the reloaded row was neither pending nor a completed replay.");
            }

            shopPurchaseETag = createdShopPurchaseETag.Value;
        }
        else
        {
            shopPurchaseETag = await shopPurchaseStore.UpdatePurchase(callerDataEntity.Type, callerDataEntity.Id, shopPurchaseData, storedShopPurchaseData.Value.shopPurchaseETag);
        }

        DateTimeOffset operationTimeUtc = DateTimeOffset.UtcNow;
        try
        {
            // Apply and record the purchase cost.
            ShopCostRecord shopCostRecord = shopPurchaseData.shopCostRecord;
            string shopCostIdempotencyKey = $"{buyShopOfferRequest.buyShopOfferIdempotencyKey}-cost";
            bool shopCostWasApplied = await playFabCurrencyService.TrySubtractCurrency(context, shopCostRecord.shopCostConfig.currencyKind, shopCostRecord.shopCostConfig.amount, shopCostIdempotencyKey);
            
            // [Duong] if fail to charge player
            if (!shopCostWasApplied)
            {
                (shopPurchaseData, shopPurchaseETag) = await UpdateShopPurchaseProgress(callerDataEntity.Type,
                    callerDataEntity.Id, shopPurchaseData, shopPurchaseETag, updatedShopPurchaseData =>
                    {
                        // [Duong] if already grant something before charging then throw
                        if (updatedShopPurchaseData.shopCostRecord.state != ShopCostState.Pending ||
                            updatedShopPurchaseData.shopGrantRecords.Any(shopGrantRecord =>
                                shopGrantRecord.state != ShopGrantState.Pending))
                            throw new InvalidOperationException("Cannot charge after grants have already been applied");
                        return updatedShopPurchaseData.RecordShopPurchaseCompleted(buyShopOfferRequest
                            .buyShopOfferIdempotencyKey);
                    });
                return CreateBuyShopOfferJsonResult(await CreateCompletedPurchaseResponse(context, shopPurchaseData));
            }

            // [Duong] Record the successful charge
            (shopPurchaseData, shopPurchaseETag) = await UpdateShopPurchaseProgress(callerDataEntity.Type, callerDataEntity.Id, shopPurchaseData, shopPurchaseETag, updatedShopPurchaseData =>
            {
                if (updatedShopPurchaseData.shopCostRecord.state != ShopCostState.Applied && (updatedShopPurchaseData.status != ShopPurchaseStatus.Pending || updatedShopPurchaseData.shopGrantRecords.Any(shopGrantRecord => shopGrantRecord.state != ShopGrantState.Pending))) throw new InvalidOperationException("The shop purchase can no longer record an applied cost.");
                return updatedShopPurchaseData.RecordShopCostApplied(buyShopOfferRequest.buyShopOfferIdempotencyKey);
            });

            // [Duong] Apply the recorded grants 
            // TODO: Optimize this like batching compatible inventory grants into one PlayFab request.
            for (int shopGrantIndex = 0; shopGrantIndex < shopPurchaseData.shopGrantRecords.Count; shopGrantIndex++)
            {
                if (shopPurchaseData.shopCostRecord.state != ShopCostState.Applied || shopPurchaseData.shopGrantRecords.Any(recordedShopGrantRecord => recordedShopGrantRecord.state == ShopGrantState.Reverted)) throw new ShopPurchaseProgressUpdateException("The refreshed shop purchase is being compensated; grant application has stopped.");
                ShopGrantRecord shopGrantRecord = shopPurchaseData.shopGrantRecords[shopGrantIndex];
                if (shopGrantRecord.state == ShopGrantState.Applied) continue;

                string grantId = shopGrantRecord.shopGrantConfig.grantId;
                string shopGrantIdempotencyKey = $"{buyShopOfferRequest.buyShopOfferIdempotencyKey}-{shopGrantIndex}";
                await shopGrantDispatcher.ApplyShopGrant(shopGrantRecord.shopGrantConfig, context, shopGrantIdempotencyKey, operationTimeUtc, request.HttpContext.RequestAborted);

                (shopPurchaseData, shopPurchaseETag) = await UpdateShopPurchaseProgress(callerDataEntity.Type, callerDataEntity.Id, shopPurchaseData, shopPurchaseETag, updatedShopPurchaseData =>
                {
                    ShopGrantRecord updatedShopGrantRecord = updatedShopPurchaseData.shopGrantRecords.Single(recordedShopGrantRecord => recordedShopGrantRecord.shopGrantConfig.grantId == grantId);
                    if (updatedShopGrantRecord.state != ShopGrantState.Applied && (updatedShopPurchaseData.status != ShopPurchaseStatus.Pending || updatedShopPurchaseData.shopCostRecord.state != ShopCostState.Applied || updatedShopPurchaseData.shopGrantRecords.Any(recordedShopGrantRecord => recordedShopGrantRecord.state == ShopGrantState.Reverted))) throw new InvalidOperationException($"The shop purchase can no longer record applied grant '{grantId}'.");
                    return updatedShopPurchaseData.RecordShopGrantApplied(buyShopOfferRequest.buyShopOfferIdempotencyKey, grantId);
                });
                if (shopPurchaseData.status == ShopPurchaseStatus.Completed) return CreateBuyShopOfferJsonResult(await CreateCompletedPurchaseResponse(context, shopPurchaseData));
            }
        }
        catch (RequestFailedException exception) when (exception.Status == (int)HttpStatusCode.PreconditionFailed)
        {
            throw;
        }
        catch (Exception exception) when (exception is ShopPurchaseProgressUpdateException || shopPurchaseData.status == ShopPurchaseStatus.Completed)
        {
            throw;
        }
        catch
        {
            // [Duong] Undo applied grants in reverse order.
            DateTimeOffset compensationTimeUtc = DateTimeOffset.UtcNow;
            for (int shopGrantIndex = shopPurchaseData.shopGrantRecords.Count - 1; shopGrantIndex >= 0; shopGrantIndex--)
            {
                ShopGrantRecord shopGrantRecord = shopPurchaseData.shopGrantRecords[shopGrantIndex];
                if (shopGrantRecord.state != ShopGrantState.Applied) continue;

                string grantId = shopGrantRecord.shopGrantConfig.grantId;
                string shopGrantIdempotencyKey = $"{buyShopOfferRequest.buyShopOfferIdempotencyKey}-{shopGrantIndex}";
                await shopGrantDispatcher.RevertShopGrant(shopGrantRecord.shopGrantConfig, context, shopGrantIdempotencyKey, compensationTimeUtc, CancellationToken.None);

                // [Duong] Record the reverted grant.
                (shopPurchaseData, shopPurchaseETag) = await UpdateShopPurchaseProgress(callerDataEntity.Type, callerDataEntity.Id, shopPurchaseData, shopPurchaseETag, updatedShopPurchaseData =>
                {
                    ShopGrantRecord updatedShopGrantRecord = updatedShopPurchaseData.shopGrantRecords.Single(recordedShopGrantRecord => recordedShopGrantRecord.shopGrantConfig.grantId == grantId);
                    if (updatedShopGrantRecord.state != ShopGrantState.Reverted && (updatedShopPurchaseData.status != ShopPurchaseStatus.Pending || updatedShopPurchaseData.shopCostRecord.state != ShopCostState.Applied)) throw new InvalidOperationException($"The shop purchase can no longer record reverted grant '{grantId}'.");
                    return updatedShopPurchaseData.RecordShopGrantReverted(buyShopOfferRequest.buyShopOfferIdempotencyKey, grantId);
                });
                if (shopPurchaseData.status == ShopPurchaseStatus.Completed) return CreateBuyShopOfferJsonResult(await CreateCompletedPurchaseResponse(context, shopPurchaseData));
            }

            // [Duong] Refund the applied charge.
            if (shopPurchaseData.shopCostRecord.state == ShopCostState.Applied)
            {
                string shopCostRevertIdempotencyKey = $"{buyShopOfferRequest.buyShopOfferIdempotencyKey}-cost-revert";
                await playFabCurrencyService.AddCurrency(context, shopPurchaseData.shopCostRecord.shopCostConfig.currencyKind, shopPurchaseData.shopCostRecord.shopCostConfig.amount, shopCostRevertIdempotencyKey);

                // [Duong] Record the refunded charge.
                (shopPurchaseData, shopPurchaseETag) = await UpdateShopPurchaseProgress(callerDataEntity.Type, callerDataEntity.Id, shopPurchaseData, shopPurchaseETag, updatedShopPurchaseData =>
                {
                    if (updatedShopPurchaseData.shopCostRecord.state != ShopCostState.Reverted && (updatedShopPurchaseData.status != ShopPurchaseStatus.Pending || updatedShopPurchaseData.shopGrantRecords.Any(shopGrantRecord => shopGrantRecord.state == ShopGrantState.Applied))) throw new InvalidOperationException("The shop purchase cannot record a reverted cost while grants remain applied or the purchase is completed.");
                    return updatedShopPurchaseData.RecordShopCostReverted(buyShopOfferRequest.buyShopOfferIdempotencyKey);
                });
                if (shopPurchaseData.status == ShopPurchaseStatus.Completed) return CreateBuyShopOfferJsonResult(await CreateCompletedPurchaseResponse(context, shopPurchaseData));
            }

            // [Duong] Mark the refunded purchase as completed.
            (shopPurchaseData, shopPurchaseETag) = await UpdateShopPurchaseProgress(callerDataEntity.Type, callerDataEntity.Id, shopPurchaseData, shopPurchaseETag, updatedShopPurchaseData =>
            {
                if (updatedShopPurchaseData.shopCostRecord.state != ShopCostState.Reverted || updatedShopPurchaseData.shopGrantRecords.Any(shopGrantRecord => shopGrantRecord.state == ShopGrantState.Applied)) throw new InvalidOperationException("The shop purchase cannot finish compensation until its cost is reverted and no grants remain applied.");
                return updatedShopPurchaseData.RecordShopPurchaseCompleted(buyShopOfferRequest.buyShopOfferIdempotencyKey);
            });
            return CreateBuyShopOfferJsonResult(await CreateCompletedPurchaseResponse(context, shopPurchaseData));
        }

        (shopPurchaseData, shopPurchaseETag) = await UpdateShopPurchaseProgress(callerDataEntity.Type, callerDataEntity.Id, shopPurchaseData, shopPurchaseETag, updatedShopPurchaseData =>
        {
            if (updatedShopPurchaseData.shopCostRecord.state != ShopCostState.Applied || updatedShopPurchaseData.shopGrantRecords.Any(shopGrantRecord => shopGrantRecord.state != ShopGrantState.Applied)) throw new InvalidOperationException("The shop purchase cannot succeed until its cost and all grants are applied.");
            return updatedShopPurchaseData.RecordShopPurchaseCompleted(buyShopOfferRequest.buyShopOfferIdempotencyKey);
        });
        return CreateBuyShopOfferJsonResult(await CreateCompletedPurchaseResponse(context, shopPurchaseData));
    }

    /// <summary>
    /// [Duong] Records a progress change, reapplying it to fresh purchase data after an ETag conflict.
    /// </summary>
    private async Task<(ShopPurchaseData shopPurchaseData, ETag shopPurchaseETag)> UpdateShopPurchaseProgress(string playerEntityType, string playerEntityId, ShopPurchaseData shopPurchaseData, ETag shopPurchaseETag, Func<ShopPurchaseData, bool> applyShopPurchaseProgress)
    {
        try
        {
            for (int purchaseProgressWriteAttempt = 0; ; purchaseProgressWriteAttempt++)
            {
                if (!applyShopPurchaseProgress(shopPurchaseData)) return (shopPurchaseData, shopPurchaseETag);

                try
                {
                    shopPurchaseETag = await shopPurchaseStore.UpdatePurchase(playerEntityType, playerEntityId, shopPurchaseData, shopPurchaseETag);
                    return (shopPurchaseData, shopPurchaseETag);
                }
                catch (RequestFailedException exception) when (exception.Status == (int)HttpStatusCode.PreconditionFailed && purchaseProgressWriteAttempt + 1 < MaxPurchaseProgressWriteAttempts)
                {
                    await Task.Delay(InitialPurchaseProgressRetryDelayMilliseconds * (1 << purchaseProgressWriteAttempt));
                    (shopPurchaseData, shopPurchaseETag) = await shopPurchaseStore.LoadPurchase(playerEntityType, playerEntityId) ?? throw new InvalidOperationException("The shop purchase row disappeared while recording progress.");
                }
            }
        }
        catch (Exception exception)
        {
            throw new ShopPurchaseProgressUpdateException("Could not record shop purchase progress.", exception);
        }
    }

    /// <summary>
    /// [Duong] Creates the response for a completed purchase from its recorded outcome and current affected state.
    /// </summary>
    private async Task<BuyShopOfferResponse> CreateCompletedPurchaseResponse(PlayFabFunctionExecutionContext context, ShopPurchaseData shopPurchaseData)
    {
        if (shopPurchaseData.status != ShopPurchaseStatus.Completed) throw new InvalidOperationException("Cannot create a response for a pending shop purchase.");

        if (shopPurchaseData.shopCostRecord.state == ShopCostState.Pending)
        {
            return new BuyShopOfferResponse
            {
                schemaVersion = ShopContract.CurrentSchemaVersion,
                outcome = BuyShopOfferOutcome.Rejected,
                rejectionReason = BuyShopOfferRejectionReason.InsufficientCurrencyAmount
            };
        }

        if (shopPurchaseData.shopCostRecord.state == ShopCostState.Reverted)
        {
            return new BuyShopOfferResponse
            {
                schemaVersion = ShopContract.CurrentSchemaVersion,
                outcome = BuyShopOfferOutcome.Rejected,
                rejectionReason = BuyShopOfferRejectionReason.GrantApplicationFailed
            };
        }

        foreach (ShopGrantRecord shopGrantRecord in shopPurchaseData.shopGrantRecords)
        {
            if (shopGrantRecord.state != ShopGrantState.Applied) throw new InvalidOperationException("A completed purchased shop offer contains a grant that was not applied.");
        }

        var playFabEconomyApi = contextReader.CreateEconomyApi(context);
        var callerEconomyEntity = contextReader.GetCallerEconomyEntity(context);
        var playerInventoryItems = await playFabInventoryService.LoadPlayerInventoryItems(playFabEconomyApi, callerEconomyEntity);
        PlayerLivesSnapshot playerLivesSnapshot = null;

        if (shopPurchaseData.shopGrantRecords.Any(shopGrantRecord => shopGrantRecord.shopGrantConfig.kind == ShopGrantKind.UnlimitedLives))
        {
            var playFabDataApi = contextReader.CreateDataApi(context);
            var callerDataEntity = contextReader.GetCallerEntity(context);
            PlayerLivesConfig playerLivesConfig = await playFabLivesConfigService.Load(context.TitleAuthenticationContext.Id);
            var entityFilesResponse = await playFabEntityFileClient.LoadEntityFileMetadata(playFabDataApi, callerDataEntity);
            (PlayerLivesData playerLivesData, _) = await livesFileStore.Load(playFabEntityFileClient, entityFilesResponse, playerLivesConfig.maximumLives);
            livesService.UpdateLivesToCurrentTime(playerLivesData, playerLivesConfig, DateTimeOffset.UtcNow);
            playerLivesSnapshot = livesService.CreateLivesSnapshot(playerLivesData, playerLivesConfig);
        }

        return new BuyShopOfferResponse
        {
            schemaVersion = ShopContract.CurrentSchemaVersion,
            outcome = BuyShopOfferOutcome.Purchased,
            playerInventorySnapshot = playFabInventoryService.CreatePlayerInventorySnapshot(playerInventoryItems),
            lives = playerLivesSnapshot
        };
    }

    /// <summary>
    /// Creates the JSON result for one shop purchase response.
    /// </summary>
    private static ContentResult CreateBuyShopOfferJsonResult(BuyShopOfferResponse buyShopOfferResponse)
    {
        return new ContentResult { Content = JsonConvert.SerializeObject(buyShopOfferResponse), ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }

    /// <summary>
    /// [Duong] Returns the purchase request after validating its required identifiers.
    /// </summary>
    private static BuyShopOfferRequest ValidateBuyShopOfferRequest(BuyShopOfferRequest buyShopOfferRequest)
    {
        if (string.IsNullOrWhiteSpace(buyShopOfferRequest.shopId)) throw new InvalidOperationException("BuyShopOffer shop ID is missing.");
        if (string.IsNullOrWhiteSpace(buyShopOfferRequest.offerId)) throw new InvalidOperationException("BuyShopOffer offer ID is missing.");
        if (!Guid.TryParseExact(buyShopOfferRequest.buyShopOfferIdempotencyKey, "N", out _)) throw new InvalidOperationException("BuyShopOffer idempotency key is invalid.");
        return buyShopOfferRequest;
    }
}
