using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Boosters;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    public class PlayFabInventoryService : IBoosterConsumptionGateway
    {
        private const string LoadPlayerInventoryFunctionName = "LoadPlayerInventory";
        private const string ConsumeBoosterFunctionName = "ConsumeBooster";
        private const int SupportedSchemaVersion = PlayerInventoryContract.CurrentSchemaVersion;

        public Task<PlayerInventoryData> LoadPlayerInventory(PlayFabAuthSession authSession)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));

            var completion = new TaskCompletionSource<PlayerInventoryData>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket, authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = LoadPlayerInventoryFunctionName
            };

            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleLoadPlayerInventoryResult(result, completion), error => completion.SetException(new InvalidOperationException($"PlayFab LoadPlayerInventory failed: {error.GenerateErrorReport()}")));
            return completion.Task;
        }

        public PlayerInventoryData CreatePlayerInventory(PlayerInventorySnapshot playerInventorySnapshot)
        {
            if (playerInventorySnapshot == null) throw new ArgumentNullException(nameof(playerInventorySnapshot));
            return CreatePlayerInventory(playerInventorySnapshot.quantitiesByCatalogId, "PlayFab player inventory snapshot");
        }

        public Task<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, PlayerInventoryData playerInventory)> ConsumeBooster(PlayFabAuthSession authSession, string boosterConsumptionIdempotencyKey, BoosterType boosterType)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));
            if (!Guid.TryParseExact(boosterConsumptionIdempotencyKey, "N", out _))
                throw new ArgumentException("Booster consumption idempotency key must be a canonical GUID.", nameof(boosterConsumptionIdempotencyKey));
            string boosterCatalogId = PlayerInventoryData.GetBoosterCatalogId(boosterType);

            var completion = new TaskCompletionSource<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, PlayerInventoryData playerInventory)>(TaskCreationOptions.RunContinuationsAsynchronously);
            var functionParameter = new ConsumeBoosterRequest { boosterConsumptionIdempotencyKey = boosterConsumptionIdempotencyKey, boosterCatalogId = boosterCatalogId };
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket,
                    authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = ConsumeBoosterFunctionName,
                FunctionParameter = functionParameter
            };

            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleConsumeBoosterResult(result, completion),
                error => completion.SetException(new PlayFabRequestException(
                    $"PlayFab ConsumeBooster request failed: {error.GenerateErrorReport()}",
                    PlayFabRetryPolicy.IsRetryable(error))));
            return completion.Task;
        }

        private void HandleLoadPlayerInventoryResult(ExecuteFunctionResult result, TaskCompletionSource<PlayerInventoryData> completion)
        {
            try
            {
                completion.SetResult(CreatePlayerInventoryFromFunctionResult(result));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private PlayerInventoryData CreatePlayerInventoryFromFunctionResult(ExecuteFunctionResult result)
        {
            if (result == null || result.Error != null || result.FunctionResultTooLarge == true || result.FunctionResult == null)
                throw new InvalidOperationException("PlayFab LoadPlayerInventory failed.");

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            LoadPlayerInventoryResponse response = JsonConvert.DeserializeObject<LoadPlayerInventoryResponse>(json);
            if (response == null) throw new InvalidOperationException("PlayFab LoadPlayerInventory returned an invalid response.");
            if (response.schemaVersion != SupportedSchemaVersion) throw new InvalidOperationException("PlayFab LoadPlayerInventory returned an unsupported schema version.");
            if (response.playerInventorySnapshot == null) throw new InvalidOperationException("PlayFab LoadPlayerInventory returned no player inventory snapshot.");
            return CreatePlayerInventory(response.playerInventorySnapshot.quantitiesByCatalogId, "PlayFab LoadPlayerInventory");
        }

        private void HandleConsumeBoosterResult(ExecuteFunctionResult result, TaskCompletionSource<(ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, PlayerInventoryData playerInventory)> completion)
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

        private (ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? rejectionReason, PlayerInventoryData playerInventory) CreateConsumeBoosterResult(ExecuteFunctionResult result)
        {
            if (result == null) throw new PlayFabRequestException("PlayFab ConsumeBooster returned no execution result.", false);
            if (result.Error != null) throw new PlayFabRequestException($"PlayFab ConsumeBooster Azure Function failed: {result.Error.Error}: {result.Error.Message}", PlayFabRetryPolicy.IsRetryable(result.Error));
            if (result.FunctionResultTooLarge == true) throw new PlayFabRequestException("PlayFab ConsumeBooster returned a result that exceeded the PlayFab size limit.", false);
            if (result.FunctionResult == null) throw new PlayFabRequestException("PlayFab ConsumeBooster returned no function result.", false);

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            ConsumeBoosterResponse response = JsonConvert.DeserializeObject<ConsumeBoosterResponse>(json);
            if (response == null) throw new PlayFabRequestException("PlayFab ConsumeBooster returned an invalid response.", false);
            if (response.schemaVersion != SupportedSchemaVersion) throw new PlayFabRequestException("PlayFab ConsumeBooster returned an unsupported schema version.", false);
            if (response.outcome == ConsumeBoosterOutcome.Consumed && response.rejectionReason.HasValue) throw new PlayFabRequestException("PlayFab ConsumeBooster returned a consumed result with a rejection reason.", false);
            if (response.outcome == ConsumeBoosterOutcome.Rejected && !response.rejectionReason.HasValue) throw new PlayFabRequestException("PlayFab ConsumeBooster rejected the use without a reason.", false);
            if (!Enum.IsDefined(typeof(ConsumeBoosterOutcome), response.outcome)) throw new PlayFabRequestException($"PlayFab ConsumeBooster returned undefined outcome {response.outcome}.", false);
            if (response.playerInventorySnapshot == null) throw new PlayFabRequestException("PlayFab ConsumeBooster returned no player inventory snapshot.", false);
            PlayerInventoryData playerInventory = CreatePlayerInventory(response.playerInventorySnapshot.quantitiesByCatalogId, "PlayFab ConsumeBooster");
            return (response.outcome, response.rejectionReason, playerInventory);
        }

        private PlayerInventoryData CreatePlayerInventory(IReadOnlyDictionary<string, int> quantitiesByCatalogId, string operationName)
        {
            if (quantitiesByCatalogId == null) throw new InvalidOperationException($"{operationName} returned no inventory quantities.");
            try
            {
                return new PlayerInventoryData(quantitiesByCatalogId);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{operationName} returned invalid player inventory.", exception);
            }
        }
    }
}
