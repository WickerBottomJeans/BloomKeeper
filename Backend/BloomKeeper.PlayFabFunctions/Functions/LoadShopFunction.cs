using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class LoadShopFunction
{
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly ShopConfigService shopConfigService = new ShopConfigService();

    /// <summary>
    /// [Duong] Loads shop config and returns its shop data
    /// </summary>
    [Function("LoadShop")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        LoadShopRequest loadShopRequest = contextReader.GetFunctionArgument<LoadShopRequest>(context);
        ShopConfig shopConfig = await shopConfigService.LoadShop(loadShopRequest.shopId);
        LoadShopResponse response = CreateLoadShopResponse(shopConfig);
        return CreateJsonResult(response);
    }
    
    private static LoadShopResponse CreateLoadShopResponse(ShopConfig shopConfig)
    {
        var offersById = shopConfig.offerCatalog.offers.ToDictionary(offer => offer.offerId);
        List<ShopOfferViewData> offers = CreateEnabledOffers(shopConfig.shopfront.offerIds, offersById);

        return new LoadShopResponse
        {
            schemaVersion = ShopContract.CurrentSchemaVersion,
            shopId = shopConfig.shopId,
            offerCatalogRevision = shopConfig.offerCatalog.revision,
            shopfrontRevision = shopConfig.shopfront.revision,
            offers = offers
        };
    }

    private static List<ShopOfferViewData> CreateEnabledOffers(IReadOnlyList<string> offerIds, IReadOnlyDictionary<string, ShopOfferConfig> offersById)
    {
        var enabledOffers = new List<ShopOfferViewData>();
        foreach (string offerId in offerIds)
        {
            ShopOfferConfig offer = offersById[offerId];
            if (!offer.enabled) continue;

            enabledOffers.Add(new ShopOfferViewData
            {
                offerId = offer.offerId,
                displayName = offer.displayName,
                cost = new ShopCostViewData { presentationKey = offer.cost.presentationKey, quantity = offer.cost.amount },
                grants = offer.grants.Select(CreateShopGrantViewData).ToList()
            });
        }

        return enabledOffers;
    }
    
    private static ShopGrantViewData CreateShopGrantViewData(ShopGrantConfig grant)
    {
        return new ShopGrantViewData { grantId = grant.grantId, presentationKey = grant.presentationKey, displayQuantity = GetGrantDisplayQuantity(grant) };
    }
    
    private static int? GetGrantDisplayQuantity(ShopGrantConfig grant)
    {
        return grant.kind switch
        {
            ShopGrantKind.InventoryItem => grant.inventoryItem.quantity,
            ShopGrantKind.UnlimitedLives => grant.unlimitedLives.durationSeconds,
            _ => throw new ArgumentOutOfRangeException(nameof(grant.kind), grant.kind, "Unsupported shop grant kind.")
        };
    }
    
    private static ContentResult CreateJsonResult(LoadShopResponse response)
    {
        return new ContentResult { Content = JsonConvert.SerializeObject(response), ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
