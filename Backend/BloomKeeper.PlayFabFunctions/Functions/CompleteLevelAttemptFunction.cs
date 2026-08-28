using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using PlayFab.EconomyModels;

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
    private readonly PendingRewardsFileStore pendingRewardsStore = new PendingRewardsFileStore();
    private readonly LevelService levelService = new LevelService();
    private readonly LevelAttemptService levelAttemptService = new LevelAttemptService();
    private readonly LivesService livesService = new LivesService();
    private readonly RewardConfigService rewardConfigService = new RewardConfigService();
    private readonly RewardService rewardService = new RewardService();
    private readonly PlayFabInventoryService inventoryService = new PlayFabInventoryService();
    private readonly RewardFulfillmentService rewardFulfillmentService;

    public CompleteLevelAttemptFunction()
    {
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
        {
            throw new InvalidOperationException("CompleteLevelAttempt function argument is null.");
        }

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
            Task<(PlayerProgressionData progression, bool fileExists)> progressionTask = progressionStore.Load(fileClient, fileMetadata);
            Task<(LevelAttemptData levelAttempt, bool fileExists)> levelAttemptTask = levelAttemptStore.Load(fileClient, fileMetadata);
            Task<(PlayerLivesData lives, bool fileExists)> livesTask = livesStore.Load(fileClient, fileMetadata, livesConfig.maximumLives);
            Task<(PendingRewardsData pendingRewards, bool fileExists)> pendingRewardsTask = pendingRewardsStore.Load(fileClient, fileMetadata);
            await Task.WhenAll(progressionTask, levelAttemptTask, livesTask, pendingRewardsTask);
            PlayerProgressionData progression = (await progressionTask).progression;
            LevelAttemptData levelAttempt = (await levelAttemptTask).levelAttempt;
            PlayerLivesData lives = (await livesTask).lives;
            PendingRewardsData pendingRewards = (await pendingRewardsTask).pendingRewards;

            LevelAttemptCompletionResult completionResult = levelAttemptService.CompleteLevelAttempt(progression, levelAttempt, attemptRequest, level);
            CompleteLevelAttemptResponse response = completionResult.Response;
            PlayerProgressionData? updatedProgression = completionResult.UpdatedProgression;
            LevelAttemptData? updatedLevelAttempt = completionResult.UpdatedLevelAttempt;

            PendingRewardBatch? pendingRewardBatch = null;
            bool pendingRewardsChanged = false;
            if (response.outcome == CompleteLevelAttemptOutcome.Saved && attemptRequest.didWin)
            {
                string completionRewardBatchId = $"{levelAttempt.attemptId}-completion-rewards";
                pendingRewardBatch = pendingRewards.batches.SingleOrDefault(batch => batch.rewardBatchId == completionRewardBatchId);
                if (updatedLevelAttempt != null)
                {
                    if (pendingRewardBatch != null) throw new InvalidOperationException($"Pending reward batch {completionRewardBatchId} already exists for a newly completed attempt.");

                    RewardTableConfig rewardConfig = await rewardConfigService.LoadCompletionRewardTable();
                    IReadOnlyList<RewardRollResult> rewardRolls = rewardService.RollRewards(rewardConfig, attemptRequest.stars);
                    pendingRewardBatch = new PendingRewardBatch { rewardBatchId = completionRewardBatchId };
                    foreach (RewardRollResult rewardRoll in rewardRolls) pendingRewardBatch.rewardRolls.Add(rewardRoll.Reward);
                    pendingRewards.batches.Add(pendingRewardBatch);
                    pendingRewardsChanged = true;
                }
            }

            // [Duong] Apply lives rules for the ended level attempt
            bool livesChanged = livesService.UpdateLivesToCurrentTime(lives, livesConfig, operationTimeUtc);
            if (updatedLevelAttempt != null)
            {
                livesService.HandleLevelAttemptEnded(lives, livesConfig, operationTimeUtc, attemptRequest.didWin, levelAttempt.didSpendLife);
                livesChanged = true;
            }

            // [Duong] Save changed player state
            if (updatedLevelAttempt != null || livesChanged || pendingRewardsChanged)
            {
                try
                {
                    var filesToUpload = new Dictionary<string, byte[]>();
                    if (updatedProgression != null) filesToUpload.Add(progressionStore.FileName, progressionStore.Serialize(updatedProgression));
                    if (updatedLevelAttempt != null) filesToUpload.Add(levelAttemptStore.FileName, levelAttemptStore.Serialize(updatedLevelAttempt));
                    if (livesChanged) filesToUpload.Add(livesStore.FileName, livesStore.Serialize(lives, livesConfig.maximumLives));
                    if (pendingRewardsChanged) filesToUpload.Add(pendingRewardsStore.FileName, pendingRewardsStore.Serialize(pendingRewards));
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

            if (pendingRewardBatch != null)
            {
                await rewardFulfillmentService.FulfillPendingRewardBatch(pendingRewardBatch, context);
                foreach (RewardGrant? completionRewardGrant in pendingRewardBatch.rewardRolls)
                    if (completionRewardGrant != null) response.completionRewardPresentationKeys.Add(completionRewardGrant.presentationKey);
                bool pendingRewardBatchRemoved = await TryRemovePendingRewardBatch(dataApi, dataEntity, pendingRewardBatch.rewardBatchId, request.HttpContext.RequestAborted);
                if (!pendingRewardBatchRemoved) return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            if (response.outcome == CompleteLevelAttemptOutcome.Saved && attemptRequest.didWin)
            {
                IReadOnlyList<InventoryItem> inventoryItems = await inventoryService.LoadPlayerInventoryItems(contextReader.CreateEconomyApi(context), contextReader.GetCallerEconomyEntity(context));
                response.playerInventorySnapshot = inventoryService.CreatePlayerInventorySnapshot(inventoryItems);
            }

            // Return completion response
            response.lives = livesService.CreateLivesSnapshot(lives, livesConfig);
            string json = JsonConvert.SerializeObject(response);
            return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
        }

        throw new InvalidOperationException("CompleteLevelAttempt exhausted its write attempts without returning a result.");
    }

    private async Task<bool> TryRemovePendingRewardBatch(PlayFab.PlayFabDataInstanceAPI dataApi, PlayFab.DataModels.EntityKey dataEntity, string rewardBatchId, CancellationToken cancellationToken)
    {
        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            var fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
            (PendingRewardsData pendingRewards, _) = await pendingRewardsStore.Load(fileClient, fileMetadata);
            PendingRewardBatch? pendingRewardBatch = pendingRewards.batches.SingleOrDefault(batch => batch.rewardBatchId == rewardBatchId);
            if (pendingRewardBatch == null) return true;

            pendingRewards.batches.Remove(pendingRewardBatch);
            try
            {
                await fileClient.UploadFile(dataApi, dataEntity, pendingRewardsStore.FileName, pendingRewardsStore.Serialize(pendingRewards), fileMetadata.ProfileVersion);
                return true;
            }
            catch (EntityProfileVersionConflictException) when (writeAttempt < MaxWriteAttempts)
            {
                int delayMilliseconds = InitialConflictRetryDelayMilliseconds * (1 << (writeAttempt - 1));
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
            catch (EntityProfileVersionConflictException)
            {
                return false;
            }
        }

        return false;
    }
}
