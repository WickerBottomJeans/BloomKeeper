using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class StartLevelAttemptFunction
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

    [Function("StartLevelAttempt")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        //[Duong] Load request context and PlayFab data access
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        StartLevelAttemptRequest startRequest = contextReader.GetFunctionArgument<StartLevelAttemptRequest>(context);
        var dataApi = contextReader.CreateDataApi(context);
        var dataEntity = contextReader.GetCallerEntity(context);
        
        //[Duong] Load level and lives config
        PlayerLivesConfig livesConfig = await livesConfigService.Load(context.TitleAuthenticationContext.Id);
        LevelData level = await levelService.Load(startRequest.levelId);
        DateTimeOffset operationTimeUtc = DateTimeOffset.UtcNow;

        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            //[Duong] Load player state files
            var fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
            
            var progressionTask = progressionStore.Load(fileClient, fileMetadata);
            var levelAttemptTask = levelAttemptStore.Load(fileClient, fileMetadata);
            var livesTask = livesStore.Load(fileClient, fileMetadata, livesConfig.maximumLives);
            await Task.WhenAll(progressionTask, levelAttemptTask, livesTask);
            var (progression, _) = await progressionTask;
            var (levelAttempt, _) = await levelAttemptTask;
            var (lives, _) = await livesTask;
            
            bool livesChanged = livesService.UpdateLivesToCurrentTime(lives, livesConfig, operationTimeUtc);
            (StartLevelAttemptResponse response, LevelAttemptData updatedLevelAttempt, bool levelAttemptChanged) = levelAttemptService.TryStartLevelAttempt(progression, levelAttempt, startRequest, level);

            //[Duong] If another attempt is made, mean player play a new level => gonna try charging them lives
            if (levelAttemptChanged)
            {
                if (!livesService.TryHandleNewLevelAttempt(lives, livesConfig, operationTimeUtc, out bool didSpendLife))
                {
                    response = levelAttemptService.CreateStartRejectedResponse(StartLevelAttemptRejectionReason.InsufficientLives);
                    levelAttemptChanged = false;
                }
                else
                {
                    updatedLevelAttempt.didSpendLife = didSpendLife;
                    livesChanged |= didSpendLife;
                }
            }

            //[Duong] Save changed player state files
            if (levelAttemptChanged || livesChanged)
            {
                try
                {
                    var filesToUpload = new Dictionary<string, byte[]>();
                    if (levelAttemptChanged) filesToUpload.Add(levelAttemptStore.FileName, levelAttemptStore.Serialize(updatedLevelAttempt));
                    if (livesChanged) filesToUpload.Add(livesStore.FileName, livesStore.Serialize(lives, livesConfig.maximumLives));
                    await fileClient.UploadFiles(dataApi, dataEntity, filesToUpload, fileMetadata.ProfileVersion);
                }
                catch (EntityProfileVersionConflictException) when (writeAttempt < MaxWriteAttempts)
                {
                    await DelayAfterConflict(writeAttempt, request.HttpContext.RequestAborted);
                    continue;
                }
                catch (EntityProfileVersionConflictException)
                {
                    return new StatusCodeResult(StatusCodes.Status409Conflict);
                }
            }

            //[Duong] Reponse to client
            response.lives = livesService.CreateLivesSnapshot(lives, livesConfig);
            return CreateJsonResult(response);
        }

        throw new InvalidOperationException("StartLevelAttempt exhausted its write attempts without returning a result.");
    }

    private  async Task DelayAfterConflict(int writeAttempt, CancellationToken cancellationToken)
    {
        int delayMilliseconds = InitialConflictRetryDelayMilliseconds * (1 << (writeAttempt - 1));
        await Task.Delay(delayMilliseconds, cancellationToken);
    }

    private  ContentResult CreateJsonResult(StartLevelAttemptResponse response)
    {
        return new ContentResult { Content = JsonConvert.SerializeObject(response), ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
