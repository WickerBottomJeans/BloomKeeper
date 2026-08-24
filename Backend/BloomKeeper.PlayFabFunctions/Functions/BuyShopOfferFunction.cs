using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

/// <summary>
/// Runs the BuyShopOffer Azure function.
/// </summary>
public class BuyShopOfferFunction
{
    private readonly PlayFabFunctionContextReader contextReader = new PlayFabFunctionContextReader();
    private readonly ShopConfigService shopConfigService = new ShopConfigService();
    private readonly ShopPurchaseService shopPurchaseService = new ShopPurchaseService();

    /// <summary>
    /// [Duong] Loads the offer, buys it, and returns a BuyShopOffer response.
    /// </summary>
    [Function("BuyShopOffer")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext context = await contextReader.ReadContext(request);
        BuyShopOfferRequest buyShopOfferRequest = contextReader.GetFunctionArgument<BuyShopOfferRequest>(context);
        if (!Guid.TryParseExact(buyShopOfferRequest.buyShopOfferIdempotencyKey, "N", out Guid parsedBuyShopOfferIdempotencyKey)) throw new InvalidOperationException("BuyShopOffer idempotency key is invalid.");

        //[Duong] Load the current price and grants from config.
        ShopOfferConfig shopOffer = await shopConfigService.LoadPurchasableOffer(buyShopOfferRequest.shopId, buyShopOfferRequest.offerId);

        //[Duong] Buy the offer with the request's purchase key.
        BuyShopOfferResponse buyShopOfferResponse = await shopPurchaseService.BuyShopOffer(contextReader.CreateEconomyApi(context), contextReader.GetCallerEconomyEntity(context), shopOffer, parsedBuyShopOfferIdempotencyKey.ToString("N"));
        string json = JsonConvert.SerializeObject(buyShopOfferResponse);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
    }
}
