using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class AbandonLevelAttemptFunction
{
    private const int MaxWriteAttempts = 3;
    private const int InitialConflictRetryDelayMilliseconds = 100;
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabPlayerStateStore playerStateStore = new PlayFabPlayerStateStore();
    private readonly LevelAttemptService levelAttemptService = new LevelAttemptService();

    [Function("AbandonLevelAttempt")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        AbandonLevelAttemptRequest abandonRequest = contextReader.GetFunctionArgument<AbandonLevelAttemptRequest>(context);
        var dataApi = contextReader.CreateDataApi(context);
        var dataEntity = contextReader.GetCallerEntity(context);

        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            (PlayerProgressionData _, LevelAttemptData levelAttempt, int profileVersion) = await playerStateStore.LoadPlayerStateForUpdate(dataApi, dataEntity);
            (AbandonLevelAttemptResponse response, LevelAttemptData updatedLevelAttempt, bool levelAttemptChanged) = levelAttemptService.Abandon(levelAttempt, abandonRequest);
            if (levelAttemptChanged)
            {
                try
                {
                    await playerStateStore.SaveLevelAttempt(dataApi, dataEntity, updatedLevelAttempt, profileVersion);
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

            return CreateJsonResult(response);
        }

        throw new InvalidOperationException("AbandonLevelAttempt exhausted its write attempts without returning a result.");
    }

    private  async Task DelayAfterConflict(int writeAttempt, CancellationToken cancellationToken)
    {
        int delayMilliseconds = InitialConflictRetryDelayMilliseconds * (1 << (writeAttempt - 1));
        await Task.Delay(delayMilliseconds, cancellationToken);
    }

    private  ContentResult CreateJsonResult(AbandonLevelAttemptResponse response)
    {
        return new ContentResult { Content = JsonConvert.SerializeObject(response), ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
