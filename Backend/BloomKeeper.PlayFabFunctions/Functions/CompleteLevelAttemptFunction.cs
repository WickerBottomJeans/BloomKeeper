using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
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
    private readonly PlayFabPlayerStateStore playerStateStore = new PlayFabPlayerStateStore();
    private readonly LevelAttemptService levelAttemptService = new LevelAttemptService();

    [Function("CompleteLevelAttempt")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        CompleteLevelAttemptRequest attemptRequest =
            contextReader.GetFunctionArgument<CompleteLevelAttemptRequest>(context);
        if (attemptRequest == null)
        {
            throw new InvalidOperationException("CompleteLevelAttempt function argument is null.");
        }

        var dataApi = contextReader.CreateDataApi(context);
        var dataEntity = contextReader.GetCallerEntity(context);

        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            (PlayerProgressionData progression, LevelAttemptData levelAttempt, int profileVersion) = await playerStateStore.LoadPlayerStateForUpdate(dataApi, dataEntity);
            (CompleteLevelAttemptResponse response, bool progressionChanged, bool levelAttemptChanged) = levelAttemptService.Complete(progression, levelAttempt, attemptRequest);

            if (levelAttemptChanged)
            {
                try
                {
                    if (progressionChanged)
                        await playerStateStore.SaveProgressionAndLevelAttempt(dataApi, dataEntity, progression, levelAttempt, profileVersion);
                    else
                        await playerStateStore.SaveLevelAttempt(dataApi, dataEntity, levelAttempt, profileVersion);
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

            string json = JsonConvert.SerializeObject(response);
            return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
        }

        throw new InvalidOperationException("CompleteLevelAttempt exhausted its write attempts without returning a result.");
    }
}
