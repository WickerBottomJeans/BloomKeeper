using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class CompleteLevelAttemptFunction
{
    private const int MaxProgressionWriteAttempts = 3;
    private const int InitialConflictRetryDelayMilliseconds = 100;
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabProgressionStore progressionStore = new PlayFabProgressionStore();
    private readonly CompleteLevelAttemptService completeLevelAttemptService = new CompleteLevelAttemptService();

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

        for (int writeAttempt = 1; writeAttempt <= MaxProgressionWriteAttempts; writeAttempt++)
        {
            (PlayerProgressionData progression, int profileVersion) = await progressionStore.LoadProgressionForUpdate(dataApi, dataEntity);
            (CompleteLevelAttemptResponse response, bool progressionChanged) = completeLevelAttemptService.Apply(progression, attemptRequest);

            if (progressionChanged)
            {
                try
                {
                    await progressionStore.SaveProgression(dataApi, dataEntity, progression, profileVersion);
                }
                catch (EntityProfileVersionConflictException) when (writeAttempt < MaxProgressionWriteAttempts)
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

        throw new InvalidOperationException("CompleteLevelAttempt exhausted its progression write attempts without returning a result.");
    }
}
