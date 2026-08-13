using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    public class PlayFabProgressionService
    {
        private const string LoadProgressionFunctionName = "LoadProgression";

        public Task<PlayerProgressionData> LoadProgression(PlayFabAuthSession authSession)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));

            var completion =
                new TaskCompletionSource<PlayerProgressionData>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket,
                    authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = LoadProgressionFunctionName
            };

            PlayFabCloudScriptAPI.ExecuteFunction(request,
                result => { HandleLoadProgressionResult(result, completion); },
                error =>
                {
                    completion.SetException(
                        new InvalidOperationException(
                            $"PlayFab LoadProgression failed: {error.GenerateErrorReport()}"));
                });

            return completion.Task;
        }

        private  void HandleLoadProgressionResult(ExecuteFunctionResult result,
            TaskCompletionSource<PlayerProgressionData> completion)
        {
            try
            {
                completion.SetResult(CreateProgressionFromFunctionResult(result));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private  PlayerProgressionData CreateProgressionFromFunctionResult(ExecuteFunctionResult result)
        {
            if (result == null || result.Error != null || result.FunctionResultTooLarge == true ||
                result.FunctionResult == null)
                throw new InvalidOperationException("PlayFab LoadProgression failed.");

            string json = result.FunctionResult is string stringResult
                ? stringResult
                : JsonConvert.SerializeObject(result.FunctionResult);
            PlayerProgressionData progression = JsonConvert.DeserializeObject<PlayerProgressionData>(json);

            if (progression == null || progression.schemaVersion <= 0 || progression.levels == null)
                throw new InvalidOperationException("PlayFab LoadProgression returned invalid progression data.");

            return progression;
        }
    }
}
