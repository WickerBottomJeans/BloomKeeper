using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

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
            //[Duong] Load player state
            var fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
            Task<(PlayerProgressionData progression, bool fileExists)> progressionTask = progressionStore.Load(fileClient, fileMetadata);
            Task<(LevelAttemptData levelAttempt, bool fileExists)> levelAttemptTask = levelAttemptStore.Load(fileClient, fileMetadata);
            Task<(PlayerLivesData lives, bool fileExists)> livesTask = livesStore.Load(fileClient, fileMetadata, livesConfig.maximumLives);
            await Task.WhenAll(progressionTask, levelAttemptTask, livesTask);
            PlayerProgressionData progression = (await progressionTask).progression;
            LevelAttemptData levelAttempt = (await levelAttemptTask).levelAttempt;
            PlayerLivesData lives = (await livesTask).lives;

            //[Duong] Apply completion changes
            bool livesChanged = livesService.RegenerateLives(lives, livesConfig, operationTimeUtc);
            (CompleteLevelAttemptResponse response, bool progressionChanged, bool levelAttemptChanged) = levelAttemptService.Complete(progression, levelAttempt, attemptRequest, level);

            // Handle ended level attempt
            if (levelAttemptChanged)
            {
                livesService.HandleLevelAttemptEnded(lives, livesConfig, operationTimeUtc, attemptRequest.didWin);
                livesChanged = true;
            }

            // Save changed player state
            if (levelAttemptChanged || livesChanged)
            {
                try
                {
                    var filesToUpload = new Dictionary<string, byte[]>();
                    if (progressionChanged) filesToUpload.Add(progressionStore.FileName, progressionStore.Serialize(progression));
                    if (levelAttemptChanged) filesToUpload.Add(levelAttemptStore.FileName, levelAttemptStore.Serialize(levelAttempt));
                    if (livesChanged) filesToUpload.Add(livesStore.FileName, livesStore.Serialize(lives, livesConfig.maximumLives));
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

            // Return completion response
            response.lives = livesService.CreateLivesSnapshot(lives, livesConfig);
            string json = JsonConvert.SerializeObject(response);
            return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
        }

        throw new InvalidOperationException("CompleteLevelAttempt exhausted its write attempts without returning a result.");
    }
}
