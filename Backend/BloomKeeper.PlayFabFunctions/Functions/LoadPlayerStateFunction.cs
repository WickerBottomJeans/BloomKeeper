using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class LoadPlayerStateFunction
{
    private const int MaxWriteAttempts = 3;
    private const int InitialConflictRetryDelayMilliseconds = 100;
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabLivesConfigService livesConfigService = new PlayFabLivesConfigService();
    private readonly PlayFabEntityFileClient fileClient = new PlayFabEntityFileClient();
    private readonly ProgressionFileStore progressionStore = new ProgressionFileStore();
    private readonly LivesFileStore livesStore = new LivesFileStore();
    private readonly LivesService livesService = new LivesService();

    [Function("LoadPlayerState")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        var dataApi = contextReader.CreateDataApi(context);
        var dataEntity = contextReader.GetCallerEntity(context);
        PlayerLivesConfig livesConfig = await livesConfigService.Load(context.TitleAuthenticationContext.Id);
        DateTimeOffset operationTimeUtc = DateTimeOffset.UtcNow;

        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            var fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
            Task<(PlayerProgressionData progression, bool fileExists)> progressionTask = progressionStore.Load(fileClient, fileMetadata);
            Task<(PlayerLivesData lives, bool fileExists)> livesTask = livesStore.Load(fileClient, fileMetadata, livesConfig.maximumLives);
            await Task.WhenAll(progressionTask, livesTask);
            (PlayerProgressionData progression, bool progressionFileExists) = await progressionTask;
            (PlayerLivesData lives, bool livesFileExists) = await livesTask;
            bool livesChanged = livesService.RegenerateLives(lives, livesConfig, operationTimeUtc);

            if (!progressionFileExists || !livesFileExists || livesChanged)
            {
                try
                {
                    var filesToUpload = new Dictionary<string, byte[]>();
                    if (!progressionFileExists) filesToUpload.Add(progressionStore.FileName, progressionStore.Serialize(progression));
                    if (!livesFileExists || livesChanged) filesToUpload.Add(livesStore.FileName, livesStore.Serialize(lives, livesConfig.maximumLives));
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

            PlayerLivesSnapshot livesSnapshot = livesService.CreateLivesSnapshot(lives, livesConfig);
            var response = new LoadPlayerStateResponse { progression = progression, lives = livesSnapshot };
            return new ContentResult { Content = JsonConvert.SerializeObject(response), ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
        }

        throw new InvalidOperationException("LoadPlayerState exhausted its write attempts without returning a result.");
    }
}
