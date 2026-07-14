using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class CompleteLevelAttemptFunction
{
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
        (PlayerProgressionData progression, int profileVersion) = await progressionStore.LoadProgressionForUpdate(dataApi, dataEntity);
        CompleteLevelAttemptResponse response = completeLevelAttemptService.Apply(progression, attemptRequest);
        await progressionStore.SaveProgression(dataApi, dataEntity, progression, profileVersion);

        string json = JsonConvert.SerializeObject(response);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
