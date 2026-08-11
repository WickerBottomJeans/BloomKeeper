using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class LoadBoosterInventoryFunction
{
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabBoosterInventoryStore inventoryStore = new PlayFabBoosterInventoryStore();

    [Function("LoadBoosterInventory")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        Dictionary<string, int> quantities = await inventoryStore.LoadInventory(contextReader.CreateEconomyApi(context), contextReader.GetCallerEconomyEntity(context));
        var response = new LoadBoosterInventoryResponse { quantitiesByFriendlyId = quantities };
        string json = JsonConvert.SerializeObject(response);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
