using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    /// <summary>
    /// Loads the current player state from PlayFab.
    /// </summary>
    public class PlayFabPlayerStateService
    {
        private const string LoadPlayerStateFunctionName = "LoadPlayerState";

        /// <summary>
        /// Requests and validates the current player state.
        /// </summary>
        public Task<LoadPlayerStateResponse> LoadPlayerState(PlayFabAuthSession authSession)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));

            var completion = new TaskCompletionSource<LoadPlayerStateResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Build the authenticated LoadPlayerState request.
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket, authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = LoadPlayerStateFunctionName
            };

            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleLoadPlayerStateResult(result, completion), error => completion.SetException(new PlayFabRequestException($"PlayFab LoadPlayerState request failed: {error.GenerateErrorReport()}", PlayFabRetryPolicy.IsRetryable(error), error.RetryAfterSeconds)));
            return completion.Task;
        }

        /// <summary>
        /// Completes the pending state request from PlayFab's callback.
        /// </summary>
        private void HandleLoadPlayerStateResult(ExecuteFunctionResult result, TaskCompletionSource<LoadPlayerStateResponse> completion)
        {
            try
            {
                completion.SetResult(CreatePlayerStateFromFunctionResult(result));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        /// <summary>
        /// Parses and validates one LoadPlayerState function result.
        /// </summary>
        private LoadPlayerStateResponse CreatePlayerStateFromFunctionResult(ExecuteFunctionResult result)
        {
            if (result == null) throw new InvalidOperationException("PlayFab LoadPlayerState returned no execution result.");
            if (result.Error != null) throw new PlayFabRequestException($"PlayFab LoadPlayerState Azure Function failed: {result.Error.Error}: {result.Error.Message}", PlayFabRetryPolicy.IsRetryable(result.Error));
            if (result.FunctionResultTooLarge == true) throw new InvalidOperationException("PlayFab LoadPlayerState returned a result that exceeded the PlayFab size limit.");
            if (result.FunctionResult == null) throw new InvalidOperationException("PlayFab LoadPlayerState returned no function result.");

            // Parse and validate the returned account state.
            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            LoadPlayerStateResponse response = JsonConvert.DeserializeObject<LoadPlayerStateResponse>(json);
            if (response == null || response.schemaVersion != LoadPlayerStateContract.CurrentSchemaVersion) throw new InvalidOperationException("PlayFab LoadPlayerState returned an unsupported response.");
            if (response.progression == null || response.progression.schemaVersion <= 0 || response.progression.levels == null) throw new InvalidOperationException("PlayFab LoadPlayerState returned invalid progression data.");
            PlayerLivesContract.ValidateSnapshot(response.lives);
            return response;
        }
    }
}
