using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class LoadProgressionFunction
{
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabPlayerStateStore playerStateStore = new PlayFabPlayerStateStore();

    [Function("LoadProgression")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        PlayerProgressionData progression = await playerStateStore.LoadProgression(contextReader.CreateDataApi(context), contextReader.GetCallerEntity(context));
        var response = new LoadProgressionResponse { schemaVersion = progression.schemaVersion, highestUnlockedLevel = progression.highestUnlockedLevel, levels = progression.levels };
        string json = JsonConvert.SerializeObject(response);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
