using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    /// <summary>
    /// Sends shop requests to PlayFab.
    /// </summary>
    public class PlayFabShopService
    {
        private const string LoadShopFunctionName = "LoadShop";
        private const string BuyShopOfferFunctionName = "BuyShopOffer";

        /// <summary>
        /// [Duong] Asks the server for shop data.
        /// </summary>
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

            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleLoadShopResult(result, shopId, completion), error => completion.SetException(new PlayFabRequestException($"PlayFab LoadShop request failed: {error.GenerateErrorReport()}", PlayFabRetryPolicy.IsRetryable(error), error.RetryAfterSeconds)));
            return completion.Task;
        }

        /// <summary>
        /// [Duong] Asks the server to buy a shop offer.
        /// </summary>
        public Task<BuyShopOfferResponse> BuyShopOffer(PlayFabAuthSession authSession, string shopId, string offerId,
            string buyShopOfferIdempotencyKey)
        {
            // [Duong] Validate purchase request data.
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));
            if (string.IsNullOrWhiteSpace(shopId)) throw new ArgumentException("Shop ID is missing.", nameof(shopId));
            if (string.IsNullOrWhiteSpace(offerId))
                throw new ArgumentException("Offer ID is missing.", nameof(offerId));
            if (!Guid.TryParseExact(buyShopOfferIdempotencyKey, "N", out _))
                throw new ArgumentException("BuyShopOffer idempotency key must be a canonical GUID.",
                    nameof(buyShopOfferIdempotencyKey));

            // [Duong] Build the purchase request.
            var completion =
                new TaskCompletionSource<BuyShopOfferResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var functionParameter = new BuyShopOfferRequest
                { shopId = shopId, offerId = offerId, buyShopOfferIdempotencyKey = buyShopOfferIdempotencyKey };
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket,
                    authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = BuyShopOfferFunctionName,
                FunctionParameter = functionParameter
            };

            // [Duong] Send the purchase request.
            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleBuyShopOfferResult(result, completion),
                error => completion.SetException(new PlayFabRequestException(
                    $"PlayFab BuyShopOffer request failed: {error.GenerateErrorReport()}",
                    PlayFabRetryPolicy.IsRetryable(error), error.RetryAfterSeconds)));
            return completion.Task;
        }

        /// <summary>
        /// Finishes the pending LoadShop task.
        /// </summary>
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

        /// <summary>
        /// [Duong] Finishes the pending BuyShopOffer task.
        /// </summary>
        private void HandleBuyShopOfferResult(ExecuteFunctionResult result, TaskCompletionSource<BuyShopOfferResponse> completion)
        {
            try
            {
                completion.SetResult(CreateBuyShopOfferResponse(result));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        /// <summary>
        /// [Duong] Reads a BuyShopOffer response from PlayFab.
        /// </summary>
        private BuyShopOfferResponse CreateBuyShopOfferResponse(ExecuteFunctionResult result)
        {
            if (result == null) throw new InvalidOperationException("PlayFab BuyShopOffer returned no execution result.");
            if (result.Error != null) throw new PlayFabRequestException($"PlayFab BuyShopOffer Azure Function failed: {result.Error.Error}: {result.Error.Message}", PlayFabRetryPolicy.IsRetryable(result.Error));
            if (result.FunctionResultTooLarge == true) throw new InvalidOperationException("PlayFab BuyShopOffer returned a result that exceeded the PlayFab size limit.");
            if (result.FunctionResult == null) throw new InvalidOperationException("PlayFab BuyShopOffer returned no function result.");

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            BuyShopOfferResponse response = JsonConvert.DeserializeObject<BuyShopOfferResponse>(json);
            if (response == null) throw new InvalidOperationException("PlayFab BuyShopOffer returned an invalid response.");
            ValidateBuyShopOfferResponse(response);
            return response;
        }

        /// <summary>
        /// Checks that a purchase response contains a consistent outcome and account snapshot.
        /// </summary>
        private static void ValidateBuyShopOfferResponse(BuyShopOfferResponse response)
        {
            // Validate the purchase outcome.
            if (response.schemaVersion != ShopContract.CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab BuyShopOffer returned unsupported schema version {response.schemaVersion}.");
            if (response.outcome == BuyShopOfferOutcome.Purchased && response.rejectionReason.HasValue) throw new InvalidOperationException("PlayFab BuyShopOffer returned a purchased result with a rejection reason.");
            if (response.outcome == BuyShopOfferOutcome.Rejected && !response.rejectionReason.HasValue) throw new InvalidOperationException("PlayFab BuyShopOffer rejected the purchase without a reason.");
            if (!Enum.IsDefined(typeof(BuyShopOfferOutcome), response.outcome)) throw new InvalidOperationException($"PlayFab BuyShopOffer returned undefined outcome {response.outcome}.");
            if (response.rejectionReason.HasValue && !Enum.IsDefined(typeof(BuyShopOfferRejectionReason), response.rejectionReason.Value)) throw new InvalidOperationException($"PlayFab BuyShopOffer returned undefined rejection reason {response.rejectionReason}.");

            // Validate the optional inventory snapshot.
            if (response.playerInventorySnapshot != null && response.playerInventorySnapshot.quantitiesByCatalogId == null) throw new InvalidOperationException("PlayFab BuyShopOffer returned an invalid player inventory snapshot.");
            if (response.lives != null) PlayerLivesContract.ValidateSnapshot(response.lives);
        }

        /// <summary>
        /// Reads a LoadShop response from PlayFab.
        /// </summary>
        private LoadShopResponse CreateShopResponse(ExecuteFunctionResult result, string requestedShopId)
        {
            if (result == null) throw new InvalidOperationException("PlayFab LoadShop returned no execution result.");
            if (result.Error != null) throw new PlayFabRequestException($"PlayFab LoadShop Azure Function failed: {result.Error.Error}: {result.Error.Message}", PlayFabRetryPolicy.IsRetryable(result.Error));
            if (result.FunctionResultTooLarge == true) throw new InvalidOperationException("PlayFab LoadShop returned a result that exceeded the PlayFab size limit.");
            if (result.FunctionResult == null) throw new InvalidOperationException("PlayFab LoadShop returned no function result.");

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            LoadShopResponse response = JsonConvert.DeserializeObject<LoadShopResponse>(json);
            if (response == null) throw new InvalidOperationException("PlayFab LoadShop returned an invalid response.");
            ValidateShopResponse(response, requestedShopId);
            return response;
        }

        /// <summary>
        /// Checks that a LoadShop response contains valid display data.
        /// </summary>
        private static void ValidateShopResponse(LoadShopResponse response, string requestedShopId)
        {
            if (response.schemaVersion != ShopContract.CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab LoadShop returned unsupported schema version {response.schemaVersion}.");
            if (!string.Equals(response.shopId, requestedShopId)) throw new InvalidOperationException($"PlayFab LoadShop returned shop {response.shopId} for requested shop {requestedShopId}.");
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
                    if (!Enum.IsDefined(typeof(ShopDisplayValueKind), grant.displayValueKind)) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} grant {grant.grantId} has undefined display value kind {grant.displayValueKind}.");
                    if (grant.displayValue <= 0) throw new InvalidOperationException($"PlayFab LoadShop offer {offer.offerId} grant {grant.grantId} has an invalid display value.");
                }
            }
        }
    }
}
