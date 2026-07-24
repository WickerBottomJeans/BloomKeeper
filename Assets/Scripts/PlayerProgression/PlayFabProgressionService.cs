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

        
        /// <summary>
        /// Submits the locally produced result of a completed level attempt for server validation
        /// </summary>
        /// <param name="authSession"></param>
        /// <param name="functionParameter"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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
                    completion.SetException(new LevelCompletionSubmissionException($"PlayFab CompleteLevelAttempt request failed: {error.GenerateErrorReport()}", PlayFabRetryPolicy.IsRetryable(error), error.RetryAfterSeconds));
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
            if (result == null) throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned no execution result.");
            if (result.Error != null) throw new LevelCompletionSubmissionException($"PlayFab CompleteLevelAttempt Azure Function failed: {result.Error.Error}: {result.Error.Message}", PlayFabRetryPolicy.IsRetryable(result.Error));
            if (result.FunctionResultTooLarge == true) throw new LevelCompletionSubmissionException("PlayFab CompleteLevelAttempt returned a result that exceeded the PlayFab size limit.", false);
            if (result.FunctionResult == null) throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned no function result.");

            string json = result.FunctionResult is string stringResult
                ? stringResult
                : JsonConvert.SerializeObject(result.FunctionResult);
            CompleteLevelAttemptResponse response = JsonConvert.DeserializeObject<CompleteLevelAttemptResponse>(json);

            if (response == null) throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned an invalid response.");
            if (response.outcome == CompleteLevelAttemptOutcome.Saved && (response.levelProgress == null || response.rejectionReason.HasValue))
                throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned invalid saved progression data.");
            if (response.outcome == CompleteLevelAttemptOutcome.Rejected && !response.rejectionReason.HasValue)
                throw new InvalidOperationException("PlayFab CompleteLevelAttempt rejected the attempt without a reason.");
            if (response.outcome != CompleteLevelAttemptOutcome.Saved && response.outcome != CompleteLevelAttemptOutcome.Rejected)
                throw new InvalidOperationException($"PlayFab CompleteLevelAttempt returned unsupported outcome {response.outcome}.");

            return response;
        }

        #endregion
    }
}
