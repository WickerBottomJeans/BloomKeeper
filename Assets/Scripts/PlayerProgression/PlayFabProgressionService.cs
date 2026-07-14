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
        private const string CompleteLevelAttemptFunctionName = "CompleteLevelAttempt";
        
        #region Load progression

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

        private static void HandleLoadProgressionResult(ExecuteFunctionResult result,
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

        private static PlayerProgressionData CreateProgressionFromFunctionResult(ExecuteFunctionResult result)
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

        #endregion
        
        #region Complete level attempt

        public Task<CompleteLevelAttemptResponse> CompleteLevelAttempt(PlayFabAuthSession authSession, CompleteLevelAttemptRequest functionParameter)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));
            if (functionParameter == null) throw new ArgumentNullException(nameof(functionParameter));

            var completion =
                new TaskCompletionSource<CompleteLevelAttemptResponse>(TaskCreationOptions
                    .RunContinuationsAsynchronously);

            var request = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket,
                    authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionParameter = functionParameter,
                FunctionName = CompleteLevelAttemptFunctionName
            };
            
            PlayFabCloudScriptAPI.ExecuteFunction(request,
                result => { HandleCompleteLevelAttemptResult(result, completion); },
                error =>
                {
                    //TODO: notify player or sth
                    completion.SetException(
                        new InvalidOperationException(
                            $"PlayFab CompleteLevelAttempt failed: {error.GenerateErrorReport()}"));
                });

            return completion.Task;
        }

        private static void HandleCompleteLevelAttemptResult(ExecuteFunctionResult result,
            TaskCompletionSource<CompleteLevelAttemptResponse> completion)
        {
            try
            {
                completion.SetResult(CreateCompleteLevelAttemptResponseFromFunctionResult(result));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private static CompleteLevelAttemptResponse CreateCompleteLevelAttemptResponseFromFunctionResult(ExecuteFunctionResult result)
        {
            if (result == null || result.Error != null || result.FunctionResultTooLarge == true ||
                result.FunctionResult == null)
                throw new InvalidOperationException("PlayFab CompleteLevelAttempt failed.");

            string json = result.FunctionResult is string stringResult
                ? stringResult
                : JsonConvert.SerializeObject(result.FunctionResult);
            CompleteLevelAttemptResponse response = JsonConvert.DeserializeObject<CompleteLevelAttemptResponse>(json);

            if (response == null || response.levelProgress == null)
                throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned invalid progression data.");

            return response;
            
        }

        #endregion
    }
}
