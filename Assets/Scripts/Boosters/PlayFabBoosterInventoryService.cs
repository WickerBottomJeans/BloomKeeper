using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Boosters;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    public class PlayFabBoosterInventoryService : IBoosterConsumptionGateway
    {
        private const string LoadBoosterInventoryFunctionName = "LoadBoosterInventory";
        private const string ConsumeBoosterFunctionName = "ConsumeBooster";
        private const int SupportedSchemaVersion = BoosterInventoryContract.CurrentSchemaVersion;
        private static readonly IReadOnlyDictionary<string, BoosterType> BoosterTypesByFriendlyId = new Dictionary<string, BoosterType>
        {
            { BoosterCatalogIds.BloomWandFriendlyId, BoosterType.BloomWand },
            { BoosterCatalogIds.GardenersGloveFriendlyId, BoosterType.GardenersGlove }
        };
        private static readonly IReadOnlyDictionary<BoosterType, string> FriendlyIdsByBoosterType = BoosterTypesByFriendlyId.ToDictionary(entry => entry.Value, entry => entry.Key);

        public Task<BoosterInventoryData> LoadInventory(PlayFabAuthSession authSession)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));

            var completion = new TaskCompletionSource<BoosterInventoryData>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket, authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = LoadBoosterInventoryFunctionName
            };

            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleLoadInventoryResult(result, completion), error => completion.SetException(new InvalidOperationException($"PlayFab LoadBoosterInventory failed: {error.GenerateErrorReport()}")));
            return completion.Task;
        }

        public Task<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, BoosterInventoryData inventory)> ConsumeBooster(PlayFabAuthSession authSession, string boosterConsumptionIdempotencyKey, BoosterType boosterType)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));
            if (!Guid.TryParseExact(boosterConsumptionIdempotencyKey, "N", out _))
                throw new ArgumentException("Booster consumption idempotency key must be a canonical GUID.", nameof(boosterConsumptionIdempotencyKey));
            if (!FriendlyIdsByBoosterType.TryGetValue(boosterType, out string friendlyId))
                throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType,
                    "Booster type is not supported by PlayFab inventory.");

            var completion =
                new TaskCompletionSource<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason,
                    BoosterInventoryData inventory)>(TaskCreationOptions.RunContinuationsAsynchronously);
            var functionParameter = new ConsumeBoosterRequest { operationId = boosterConsumptionIdempotencyKey, boosterFriendlyId = friendlyId };
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket,
                    authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = ConsumeBoosterFunctionName,
                FunctionParameter = functionParameter
            };

            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleConsumeBoosterResult(result, completion),
                error => completion.SetException(new BoosterConsumptionException(
                    $"PlayFab ConsumeBooster request failed: {error.GenerateErrorReport()}",
                    PlayFabRetryPolicy.IsRetryable(error))));
            return completion.Task;
        }

        private  void HandleLoadInventoryResult(ExecuteFunctionResult result, TaskCompletionSource<BoosterInventoryData> completion)
        {
            try
            {
                completion.SetResult(CreateInventoryFromFunctionResult(result));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private  BoosterInventoryData CreateInventoryFromFunctionResult(ExecuteFunctionResult result)
        {
            if (result == null || result.Error != null || result.FunctionResultTooLarge == true || result.FunctionResult == null)
                throw new InvalidOperationException("PlayFab LoadBoosterInventory failed.");

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            LoadBoosterInventoryResponse response = JsonConvert.DeserializeObject<LoadBoosterInventoryResponse>(json);
            if (response == null) throw new InvalidOperationException("PlayFab LoadBoosterInventory returned an invalid response.");
            if (response.schemaVersion != SupportedSchemaVersion) throw new InvalidOperationException("PlayFab LoadBoosterInventory returned an unsupported schema version.");
            return CreateInventory(response.quantitiesByFriendlyId, "PlayFab LoadBoosterInventory");
        }

        private  void HandleConsumeBoosterResult(ExecuteFunctionResult result, TaskCompletionSource<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, BoosterInventoryData inventory)> completion)
        {
            try
            {
                completion.SetResult(CreateConsumeBoosterResult(result));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private  (ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, BoosterInventoryData inventory) CreateConsumeBoosterResult(ExecuteFunctionResult result)
        {
            if (result == null) throw new BoosterConsumptionException("PlayFab ConsumeBooster returned no execution result.", false);
            if (result.Error != null) throw new BoosterConsumptionException($"PlayFab ConsumeBooster Azure Function failed: {result.Error.Error}: {result.Error.Message}", PlayFabRetryPolicy.IsRetryable(result.Error));
            if (result.FunctionResultTooLarge == true) throw new BoosterConsumptionException("PlayFab ConsumeBooster returned a result that exceeded the PlayFab size limit.", false);
            if (result.FunctionResult == null) throw new BoosterConsumptionException("PlayFab ConsumeBooster returned no function result.", false);

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            ConsumeBoosterResponse response = JsonConvert.DeserializeObject<ConsumeBoosterResponse>(json);
            if (response == null) throw new BoosterConsumptionException("PlayFab ConsumeBooster returned an invalid response.", false);
            if (response.schemaVersion != SupportedSchemaVersion) throw new BoosterConsumptionException("PlayFab ConsumeBooster returned an unsupported schema version.", false);
            if (response.outcome == ConsumeBoosterOutcome.Consumed && response.rejectionReason.HasValue) throw new BoosterConsumptionException("PlayFab ConsumeBooster returned a consumed result with a rejection reason.", false);
            if (response.outcome == ConsumeBoosterOutcome.Rejected && !response.rejectionReason.HasValue) throw new BoosterConsumptionException("PlayFab ConsumeBooster rejected the use without a reason.", false);
            if (response.outcome != ConsumeBoosterOutcome.Consumed && response.outcome != ConsumeBoosterOutcome.Rejected) throw new BoosterConsumptionException($"PlayFab ConsumeBooster returned unsupported outcome {response.outcome}.", false);
            BoosterInventoryData inventory = CreateInventory(response.quantitiesByFriendlyId, "PlayFab ConsumeBooster");
            return (response.outcome, response.rejectionReason, inventory);
        }

        private  BoosterInventoryData CreateInventory(IReadOnlyDictionary<string, int> quantitiesByFriendlyId, string operationName)
        {
            if (quantitiesByFriendlyId == null) throw new InvalidOperationException($"{operationName} returned no booster quantities.");

            var quantities = new Dictionary<BoosterType, int>();
            foreach (KeyValuePair<string, int> entry in quantitiesByFriendlyId)
            {
                if (!BoosterTypesByFriendlyId.TryGetValue(entry.Key, out BoosterType boosterType))
                    throw new InvalidOperationException($"{operationName} returned unsupported Friendly ID {entry.Key}.");
                if (entry.Value < 0)
                    throw new InvalidOperationException($"{operationName} returned an invalid quantity for {entry.Key}.");
                if (!quantities.TryAdd(boosterType, entry.Value))
                    throw new InvalidOperationException($"{operationName} returned duplicate booster type {boosterType}.");
            }

            if (quantities.Count != BoosterTypesByFriendlyId.Count)
                throw new InvalidOperationException($"{operationName} did not return every supported booster.");

            return new BoosterInventoryData(quantities);
        }
    }
}
