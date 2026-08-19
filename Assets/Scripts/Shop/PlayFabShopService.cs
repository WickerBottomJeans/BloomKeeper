using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    public class PlayFabShopService
    {
        private const string LoadShopFunctionName = "LoadShop";

        public Task<LoadShopResponse> LoadShop(PlayFabAuthSession authSession, string shopId)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));
            if (string.IsNullOrWhiteSpace(shopId)) throw new ArgumentException("Shop ID is missing.", nameof(shopId));

            var completion = new TaskCompletionSource<LoadShopResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var functionParameter = new LoadShopRequest { shopId = shopId };
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket, authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = LoadShopFunctionName,
                FunctionParameter = functionParameter
            };

            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleLoadShopResult(result, shopId, completion), error => completion.SetException(new ShopLoadException($"PlayFab LoadShop request failed: {error.GenerateErrorReport()}", PlayFabRetryPolicy.IsRetryable(error), error.RetryAfterSeconds)));
            return completion.Task;
        }
    
        private void HandleLoadShopResult(ExecuteFunctionResult result, string requestedShopId, TaskCompletionSource<LoadShopResponse> completion)
        {
            try
            {
                completion.SetResult(CreateShopResponse(result, requestedShopId));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private LoadShopResponse CreateShopResponse(ExecuteFunctionResult result, string requestedShopId)
        {
            if (result == null) throw new InvalidOperationException("PlayFab LoadShop returned no execution result.");
            if (result.Error != null) throw new ShopLoadException($"PlayFab LoadShop Azure Function failed: {result.Error.Error}: {result.Error.Message}", PlayFabRetryPolicy.IsRetryable(result.Error));
            if (result.FunctionResultTooLarge == true) throw new InvalidOperationException("PlayFab LoadShop returned a result that exceeded the PlayFab size limit.");
            if (result.FunctionResult == null) throw new InvalidOperationException("PlayFab LoadShop returned no function result.");

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            LoadShopResponse response = JsonConvert.DeserializeObject<LoadShopResponse>(json);
            if (response == null) throw new InvalidOperationException("PlayFab LoadShop returned an invalid response.");
            ValidateShopResponse(response, requestedShopId);
            return response;
        }

        private static void ValidateShopResponse(LoadShopResponse response, string requestedShopId)
        {
            if (response.schemaVersion != ShopContract.CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab LoadShop returned unsupported schema version {response.schemaVersion}.");
            if (!string.Equals(response.shopId, requestedShopId, StringComparison.Ordinal)) throw new InvalidOperationException($"PlayFab LoadShop returned shop {response.shopId} for requested shop {requestedShopId}.");
            if (response.offerCatalogRevision <= 0) throw new InvalidOperationException("PlayFab LoadShop returned an invalid offer catalog revision.");
            if (response.shopfrontRevision <= 0) throw new InvalidOperationException("PlayFab LoadShop returned an invalid shopfront revision.");
            if (response.offers == null) throw new InvalidOperationException("PlayFab LoadShop returned no offers.");

            foreach (ShopOfferViewData offer in response.offers)
            {
                if (offer == null) throw new InvalidOperationException("PlayFab LoadShop returned a null offer.");
                if (string.IsNullOrWhiteSpace(offer.offerId)) throw new InvalidOperationException("PlayFab LoadShop returned an offer without an ID.");
                if (string.IsNullOrWhiteSpace(offer.displayName)) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} has no display name.");
                if (offer.cost == null) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} has no cost.");
                if (string.IsNullOrWhiteSpace(offer.cost.presentationKey)) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} cost has no presentation key.");
                if (offer.cost.quantity <= 0) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} cost quantity must be greater than zero.");
                if (offer.grants == null || offer.grants.Count == 0) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} returned no grants.");

                foreach (ShopGrantViewData grant in offer.grants)
                {
                    if (grant == null) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} returned a null grant.");
                    if (string.IsNullOrWhiteSpace(grant.grantId)) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} returned a grant without an ID.");
                    if (string.IsNullOrWhiteSpace(grant.presentationKey)) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} grant {grant.grantId} has no presentation key.");
                    if (grant.displayQuantity.HasValue && grant.displayQuantity.Value <= 0) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} grant {grant.grantId} has an invalid display quantity.");
                }
            }
        }
    }
}
