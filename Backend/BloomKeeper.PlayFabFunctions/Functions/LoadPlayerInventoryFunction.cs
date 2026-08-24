using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using PlayFab.EconomyModels;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class LoadPlayerInventoryFunction
{
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabInventoryService inventoryService = new PlayFabInventoryService();

    [Function("LoadPlayerInventory")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        IReadOnlyList<InventoryItem> inventoryItems = await inventoryService.LoadPlayerInventoryItems(contextReader.CreateEconomyApi(context), contextReader.GetCallerEconomyEntity(context));
        var response = new LoadPlayerInventoryResponse { playerInventorySnapshot = inventoryService.CreatePlayerInventorySnapshot(inventoryItems) };
        string json = JsonConvert.SerializeObject(response);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
