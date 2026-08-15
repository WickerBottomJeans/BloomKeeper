using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    public class PlayFabPlayerStateService
    {
        private const string LoadPlayerStateFunctionName = "LoadPlayerState";

        public Task<LoadPlayerStateResponse> LoadPlayerState(PlayFabAuthSession authSession)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));

            var completion = new TaskCompletionSource<LoadPlayerStateResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket, authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = LoadPlayerStateFunctionName
            };

            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleLoadPlayerStateResult(result, completion), error => completion.SetException(new InvalidOperationException($"PlayFab LoadPlayerState failed: {error.GenerateErrorReport()}")));
            return completion.Task;
        }

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

        private LoadPlayerStateResponse CreatePlayerStateFromFunctionResult(ExecuteFunctionResult result)
        {
            if (result == null || result.Error != null || result.FunctionResultTooLarge == true || result.FunctionResult == null) throw new InvalidOperationException("PlayFab LoadPlayerState failed.");

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            LoadPlayerStateResponse response = JsonConvert.DeserializeObject<LoadPlayerStateResponse>(json);
            if (response == null || response.schemaVersion != LoadPlayerStateContract.CurrentSchemaVersion) throw new InvalidOperationException("PlayFab LoadPlayerState returned an unsupported response.");
            if (response.progression == null || response.progression.schemaVersion <= 0 || response.progression.levels == null) throw new InvalidOperationException("PlayFab LoadPlayerState returned invalid progression data.");
            PlayerLivesContract.ValidateSnapshot(response.lives);
            return response;
        }
    }
}
