using Azure;
using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using PlayFab.EconomyModels;
using System.Net;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class CompleteLevelAttemptFunction
{
    private const int MaxWriteAttempts = 3;
    private const int InitialConflictRetryDelayMilliseconds = 100;
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabLivesConfigService livesConfigService = new PlayFabLivesConfigService();
    private readonly PlayFabEntityFileClient fileClient = new PlayFabEntityFileClient();
    private readonly ProgressionFileStore progressionStore = new ProgressionFileStore();
    private readonly LevelAttemptFileStore levelAttemptStore = new LevelAttemptFileStore();
    private readonly LivesFileStore livesStore = new LivesFileStore();
    private readonly LevelService levelService = new LevelService();
    private readonly LevelAttemptService levelAttemptService = new LevelAttemptService();
    private readonly LivesService livesService = new LivesService();
    private readonly RewardConfigService rewardConfigService = new RewardConfigService();
    private readonly RewardService rewardService = new RewardService();
    private readonly PlayFabInventoryService inventoryService = new PlayFabInventoryService();
    private readonly RewardFulfillmentService rewardFulfillmentService;
    private readonly CompletionRewardStore completionRewardStore;

    public CompleteLevelAttemptFunction(CompletionRewardStore completionRewardStore)
    {
        this.completionRewardStore = completionRewardStore ?? throw new ArgumentNullException(nameof(completionRewardStore));
        rewardFulfillmentService = new RewardFulfillmentService(contextReader, inventoryService);
    }

    [Function("CompleteLevelAttempt")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        //[Duong] Load completion request
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        CompleteLevelAttemptRequest attemptRequest =
            contextReader.GetFunctionArgument<CompleteLevelAttemptRequest>(context);
        if (attemptRequest == null)
            throw new InvalidOperationException("CompleteLevelAttempt function argument is null.");

        //[Duong] Load completion dependencies
        var dataApi = contextReader.CreateDataApi(context);
        var dataEntity = contextReader.GetCallerEntity(context);
        PlayerLivesConfig livesConfig = await livesConfigService.Load(context.TitleAuthenticationContext.Id);
        LevelData level = attemptRequest.didWin ? await levelService.Load(attemptRequest.levelId) : null;
        DateTimeOffset operationTimeUtc = DateTimeOffset.UtcNow;

        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            //[Duong] Load player state entity files
            var fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
            Task<(PlayerProgressionData progression, bool fileExists)> progressionTask =
                progressionStore.Load(fileClient, fileMetadata);
            Task<(LevelAttemptData levelAttempt, bool fileExists)> levelAttemptTask =
                levelAttemptStore.Load(fileClient, fileMetadata);
            Task<(PlayerLivesData lives, bool fileExists)> livesTask =
                livesStore.Load(fileClient, fileMetadata, livesConfig.maximumLives);
            await Task.WhenAll(progressionTask, levelAttemptTask, livesTask);
            PlayerProgressionData progression = (await progressionTask).progression;
            LevelAttemptData levelAttempt = (await levelAttemptTask).levelAttempt;
            PlayerLivesData lives = (await livesTask).lives;

            LevelAttemptCompletionResult completionResult =
                levelAttemptService.CompleteLevelAttempt(progression, levelAttempt, attemptRequest, level);
            CompleteLevelAttemptResponse response = completionResult.Response;
            PlayerProgressionData? updatedProgression = completionResult.UpdatedProgression;
            LevelAttemptData? updatedLevelAttempt = completionResult.UpdatedLevelAttempt;

            CompletionRewardData? completionRewardData = null;
            ETag completionRewardETag = default;
            if (response.outcome == CompleteLevelAttemptOutcome.Saved && attemptRequest.didWin)
            {
                // [Duong] Load the winning attempt's existing saga.
                var storedCompletionRewardData =
                    await completionRewardStore.LoadCompletionReward(dataEntity.Type, dataEntity.Id,
                        attemptRequest.attemptId);

                // [Duong] If this winning level attempt already has a reward saga row.
                if (storedCompletionRewardData.HasValue)
                {
                    completionRewardData = storedCompletionRewardData.Value.completionRewardData;
                    completionRewardETag = storedCompletionRewardData.Value.completionRewardETag;
                }
                // [Duong] If this is the first completion of the level attempt
                else if (updatedLevelAttempt != null)
                {
                    // [Duong] Roll and snapshot rewards before committing the level completion.
                    RewardTableConfig rewardConfig = await rewardConfigService.LoadCompletionRewardTable();
                    IReadOnlyList<RewardRollResult> rewardRollResults =
                        rewardService.RollRewards(rewardConfig, attemptRequest.stars);
                    completionRewardData = new CompletionRewardData(attemptRequest, rewardRollResults);
                    ETag? createdCompletionRewardETag =
                        await completionRewardStore.TryInsertCompletionReward(dataEntity.Type, dataEntity.Id,
                            completionRewardData);
                    // [Duong] If the new saga row was inserted
                    if (createdCompletionRewardETag.HasValue)
                    {
                        // [Duong] Continue with the newly created saga version.
                        completionRewardETag = createdCompletionRewardETag.Value;
                    }
                    //[Duong] If another request already inserted the saga row.
                    else
                    {
                        // [Duong] Resume the saga created by the competing request.
                        storedCompletionRewardData =
                            await completionRewardStore.LoadCompletionReward(dataEntity.Type, dataEntity.Id,
                                attemptRequest.attemptId) ?? throw new InvalidOperationException(
                                "The completion reward row creation conflicted, but the row could not be loaded.");
                        completionRewardData = storedCompletionRewardData.Value.completionRewardData;
                        completionRewardETag = storedCompletionRewardData.Value.completionRewardETag;
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Completed winning level attempt {attemptRequest.attemptId} has no completion reward saga row.");
                }

                // [Duong] Ensure the stored reward saga belongs to this exact attempt and result.
                completionRewardData.ValidateCompletionRequest(attemptRequest);
            }

            // [Duong] Apply lives rules for the ended level attempt
            bool livesChanged = livesService.UpdateLivesToCurrentTime(lives, livesConfig, operationTimeUtc);
            if (updatedLevelAttempt != null)
            {
                livesService.HandleLevelAttemptEnded(lives, livesConfig, operationTimeUtc, attemptRequest.didWin,
                    levelAttempt.didSpendLife);
                livesChanged = true;
            }

            // [Duong] Save changed player state
            if (updatedLevelAttempt != null || livesChanged)
            {
                try
                {
                    var filesToUpload = new Dictionary<string, byte[]>();
                    if (updatedProgression != null)
                        filesToUpload.Add(progressionStore.FileName, progressionStore.Serialize(updatedProgression));
                    if (updatedLevelAttempt != null)
                        filesToUpload.Add(levelAttemptStore.FileName, levelAttemptStore.Serialize(updatedLevelAttempt));
                    if (livesChanged)
                        filesToUpload.Add(livesStore.FileName, livesStore.Serialize(lives, livesConfig.maximumLives));
                    await fileClient.UploadFiles(dataApi, dataEntity, filesToUpload, fileMetadata.ProfileVersion);
                }
                catch (EntityProfileVersionConflictException) when (writeAttempt < MaxWriteAttempts)
                {
                    int delayMilliseconds = InitialConflictRetryDelayMilliseconds * (1 << (writeAttempt - 1));
                    await Task.Delay(delayMilliseconds, request.HttpContext.RequestAborted);
                    continue;
                }
                catch (EntityProfileVersionConflictException)
                {
                    return new StatusCodeResult(StatusCodes.Status409Conflict);
                }
            }

            if (completionRewardData != null)
            {
                // Record the confirmed level completion.
                (completionRewardData, completionRewardETag) = await UpdateCompletionRewardProgress(dataEntity.Type,
                    dataEntity.Id, attemptRequest, completionRewardData, completionRewardETag,
                    updatedCompletionRewardData => updatedCompletionRewardData.RecordLevelCompletionCommitted(),
                    request.HttpContext.RequestAborted);

                // Resume every pending reward grant.
                for (int rewardIndex = 0; rewardIndex < completionRewardData.rewardRecords.Count; rewardIndex++)
                {
                    CompletionRewardRecord completionRewardRecord = completionRewardData.rewardRecords[rewardIndex];
                    if (completionRewardRecord.state != CompletionRewardState.Pending) continue;

                    // Grant the reward with its stable idempotency key.
                    string rewardGrantIdempotencyKey = $"{attemptRequest.attemptId}-completion-rewards-{rewardIndex}";
                    await rewardFulfillmentService.FulfillReward(completionRewardRecord.reward!, context,
                        rewardGrantIdempotencyKey);

                    // Record the applied reward.
                    int appliedRewardIndex = rewardIndex;
                    (completionRewardData, completionRewardETag) = await UpdateCompletionRewardProgress(dataEntity.Type,
                        dataEntity.Id, attemptRequest, completionRewardData, completionRewardETag,
                        updatedCompletionRewardData =>
                            updatedCompletionRewardData.RecordRewardApplied(appliedRewardIndex),
                        request.HttpContext.RequestAborted);
                }

                // Complete the saga and rebuild its presentation result.
                (completionRewardData, completionRewardETag) = await UpdateCompletionRewardProgress(dataEntity.Type,
                    dataEntity.Id, attemptRequest, completionRewardData, completionRewardETag,
                    updatedCompletionRewardData => updatedCompletionRewardData.RecordCompleted(DateTimeOffset.UtcNow),
                    request.HttpContext.RequestAborted);
                foreach (CompletionRewardRecord completionRewardRecord in completionRewardData.rewardRecords)
                    if (completionRewardRecord.reward != null)
                        response.completionRewardPresentationKeys.Add(completionRewardRecord.reward.presentationKey);
            }

            if (response.outcome == CompleteLevelAttemptOutcome.Saved && attemptRequest.didWin)
            {
                IReadOnlyList<InventoryItem> inventoryItems =
                    await inventoryService.LoadPlayerInventoryItems(contextReader.CreateEconomyApi(context),
                        contextReader.GetCallerEconomyEntity(context));
                response.playerInventorySnapshot = inventoryService.CreatePlayerInventorySnapshot(inventoryItems);
            }

            // Return completion response
            response.lives = livesService.CreateLivesSnapshot(lives, livesConfig);
            string json = JsonConvert.SerializeObject(response);
            return new ContentResult
                { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
        }

        throw new InvalidOperationException(
            "CompleteLevelAttempt exhausted its write attempts without returning a result.");
    }

    private async Task<(CompletionRewardData completionRewardData, ETag completionRewardETag)> UpdateCompletionRewardProgress(string playerEntityType, string playerEntityId, CompleteLevelAttemptRequest attemptRequest, CompletionRewardData completionRewardData, ETag completionRewardETag, Func<CompletionRewardData, bool> updateCompletionRewardData, CancellationToken cancellationToken)
    {
        // Apply progress against the current saga version.
        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            try
            {
                if (!updateCompletionRewardData(completionRewardData)) return (completionRewardData, completionRewardETag);
                completionRewardETag = await completionRewardStore.UpdateCompletionReward(playerEntityType, playerEntityId, completionRewardData, completionRewardETag);
                return (completionRewardData, completionRewardETag);
            }
            catch (RequestFailedException exception) when (exception.Status == (int)HttpStatusCode.PreconditionFailed && writeAttempt < MaxWriteAttempts)
            {
                // Reload progress after a competing writer wins.
                int delayMilliseconds = InitialConflictRetryDelayMilliseconds * (1 << (writeAttempt - 1));
                await Task.Delay(delayMilliseconds, cancellationToken);
                var storedCompletionRewardData = await completionRewardStore.LoadCompletionReward(playerEntityType, playerEntityId, attemptRequest.attemptId) ?? throw new InvalidOperationException("The completion reward row disappeared while recording progress.");
                completionRewardData = storedCompletionRewardData.completionRewardData;
                completionRewardETag = storedCompletionRewardData.completionRewardETag;
                completionRewardData.ValidateCompletionRequest(attemptRequest);
            }
        }

        throw new InvalidOperationException("CompleteLevelAttempt exhausted its completion reward progress write attempts.");
    }
}
