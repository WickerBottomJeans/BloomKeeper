using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class ConsumeBoosterFunction
{
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabBoosterInventoryStore inventoryStore = new PlayFabBoosterInventoryStore();

    [Function("ConsumeBooster")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        ConsumeBoosterRequest consumeRequest = contextReader.GetFunctionArgument<ConsumeBoosterRequest>(context);
        if (!Guid.TryParseExact(consumeRequest.operationId, "N", out Guid operationId)) throw new InvalidOperationException("ConsumeBooster operation ID is invalid.");
        if (string.IsNullOrWhiteSpace(consumeRequest.boosterFriendlyId)) throw new InvalidOperationException("ConsumeBooster Friendly ID is missing.");

        var economyApi = contextReader.CreateEconomyApi(context);
        var callerEntity = contextReader.GetCallerEconomyEntity(context);
        (ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, Dictionary<string, int> quantities) = await inventoryStore.ConsumeOne(economyApi, callerEntity, consumeRequest.boosterFriendlyId, operationId.ToString("N"));
        var response = new ConsumeBoosterResponse { outcome = outcome, rejectionReason = rejectionReason, quantitiesByFriendlyId = quantities };
        string json = JsonConvert.SerializeObject(response);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
