using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    public class PlayFabLevelAttemptService
    {
        private const string StartLevelAttemptFunctionName = "StartLevelAttempt";
        private const string AbandonLevelAttemptFunctionName = "AbandonLevelAttempt";
        private const string CompleteLevelAttemptFunctionName = "CompleteLevelAttempt";

        public Task<StartLevelAttemptResponse> StartLevelAttempt(PlayFabAuthSession authSession, string startLevelRequestIdempotencyKey, int levelId)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));
            if (!Guid.TryParseExact(startLevelRequestIdempotencyKey, "N", out _)) throw new ArgumentException("Start level request idempotency key must be a canonical GUID.", nameof(startLevelRequestIdempotencyKey));

            var functionParameter = new StartLevelAttemptRequest { startLevelRequestIdempotencyKey = startLevelRequestIdempotencyKey, levelId = levelId };
            return ExecuteLevelAttemptFunction<StartLevelAttemptRequest, StartLevelAttemptResponse>(authSession, StartLevelAttemptFunctionName, functionParameter, ValidateStartLevelAttemptResponse);
        }

        /// <summary>
        /// [Duong] Requests that the server abandon the player's current level attempt.
        /// </summary>
        public Task<AbandonLevelAttemptResponse> AbandonLevelAttempt(PlayFabAuthSession authSession, string levelAttemptId)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));
            if (!Guid.TryParseExact(levelAttemptId, "N", out _)) throw new ArgumentException("Level attempt ID must be a canonical GUID.", nameof(levelAttemptId));

            var functionParameter = new AbandonLevelAttemptRequest { levelAttemptId = levelAttemptId };
            return ExecuteLevelAttemptFunction<AbandonLevelAttemptRequest, AbandonLevelAttemptResponse>(authSession, AbandonLevelAttemptFunctionName, functionParameter, ValidateAbandonLevelAttemptResponse);
        }

        public Task<CompleteLevelAttemptResponse> CompleteLevelAttempt(PlayFabAuthSession authSession, CompleteLevelAttemptRequest functionParameter)
        {
            if (authSession == null) throw new ArgumentNullException(nameof(authSession));
            if (functionParameter == null) throw new ArgumentNullException(nameof(functionParameter));

            var completion = new TaskCompletionSource<CompleteLevelAttemptResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            ExecuteFunctionRequest request = CreateExecuteFunctionRequest(authSession, CompleteLevelAttemptFunctionName, functionParameter);
            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleCompleteLevelAttemptResult(result, functionParameter.didWin, functionParameter.stars, completion), error => completion.SetException(new LevelCompletionSubmissionException($"PlayFab CompleteLevelAttempt request failed: {error.GenerateErrorReport()}", PlayFabRetryPolicy.IsRetryable(error), error.RetryAfterSeconds)));
            return completion.Task;
        }

        private  Task<TResponse> ExecuteLevelAttemptFunction<TRequest, TResponse>(PlayFabAuthSession authSession, string functionName, TRequest functionParameter, Action<TResponse> validateResponse) where TResponse : class
        {
            var completion = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            ExecuteFunctionRequest request = CreateExecuteFunctionRequest(authSession, functionName, functionParameter);
            PlayFabCloudScriptAPI.ExecuteFunction(request, result => HandleLevelAttemptResult(result, functionName, validateResponse, completion), error => completion.SetException(new LevelAttemptRequestException($"PlayFab {functionName} request failed: {error.GenerateErrorReport()}", PlayFabRetryPolicy.IsRetryable(error), error.RetryAfterSeconds)));
            return completion.Task;
        }

        private  ExecuteFunctionRequest CreateExecuteFunctionRequest(PlayFabAuthSession authSession, string functionName, object functionParameter)
        {
            return new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(authSession.SessionTicket, authSession.EntityToken, authSession.PlayFabId, authSession.EntityId, authSession.EntityType),
                Entity = new EntityKey { Id = authSession.EntityId, Type = authSession.EntityType },
                FunctionName = functionName,
                FunctionParameter = functionParameter
            };
        }

        private  void HandleLevelAttemptResult<TResponse>(ExecuteFunctionResult result, string functionName, Action<TResponse> validateResponse, TaskCompletionSource<TResponse> completion) where TResponse : class
        {
            try
            {
                TResponse response = DeserializeFunctionResult<TResponse>(result, functionName, (message, isRetryable) => new LevelAttemptRequestException(message, isRetryable));
                validateResponse(response);
                completion.SetResult(response);
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private  void HandleCompleteLevelAttemptResult(ExecuteFunctionResult result, bool didWin, int earnedStars, TaskCompletionSource<CompleteLevelAttemptResponse> completion)
        {
            try
            {
                CompleteLevelAttemptResponse response = DeserializeFunctionResult<CompleteLevelAttemptResponse>(result, CompleteLevelAttemptFunctionName, (message, isRetryable) => new LevelCompletionSubmissionException(message, isRetryable));
                ValidateCompleteLevelAttemptResponse(response, didWin, earnedStars);
                completion.SetResult(response);
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private  TResponse DeserializeFunctionResult<TResponse>(ExecuteFunctionResult result, string functionName, Func<string, bool, Exception> createFunctionException) where TResponse : class
        {
            if (result == null) throw new InvalidOperationException($"PlayFab {functionName} returned no execution result.");
            if (result.Error != null) throw createFunctionException($"PlayFab {functionName} Azure Function failed: {result.Error.Error}: {result.Error.Message}", PlayFabRetryPolicy.IsRetryable(result.Error));
            if (result.FunctionResultTooLarge == true) throw createFunctionException($"PlayFab {functionName} returned a result that exceeded the PlayFab size limit.", false);
            if (result.FunctionResult == null) throw new InvalidOperationException($"PlayFab {functionName} returned no function result.");

            string json = result.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(result.FunctionResult);
            TResponse response = JsonConvert.DeserializeObject<TResponse>(json);
            if (response == null) throw new InvalidOperationException($"PlayFab {functionName} returned an invalid response.");
            return response;
        }

        private  void ValidateStartLevelAttemptResponse(StartLevelAttemptResponse response)
        {
            if (response.schemaVersion != LevelAttemptContract.CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab StartLevelAttempt returned unsupported schema version {response.schemaVersion}.");
            if (response.outcome == StartLevelAttemptOutcome.Approved && (!Guid.TryParseExact(response.levelAttemptId, "N", out _) || response.rejectionReason.HasValue)) throw new InvalidOperationException("PlayFab StartLevelAttempt returned invalid started-attempt data.");
            if (response.outcome == StartLevelAttemptOutcome.Rejected && (!response.rejectionReason.HasValue || !string.IsNullOrEmpty(response.levelAttemptId))) throw new InvalidOperationException("PlayFab StartLevelAttempt returned invalid rejection data.");
            if (response.outcome == StartLevelAttemptOutcome.Rejected && response.rejectionReason != StartLevelAttemptRejectionReason.LevelLocked && response.rejectionReason != StartLevelAttemptRejectionReason.InsufficientLives && response.rejectionReason != StartLevelAttemptRejectionReason.OperationConflict && response.rejectionReason != StartLevelAttemptRejectionReason.LevelUnavailable) throw new InvalidOperationException($"PlayFab StartLevelAttempt returned unsupported rejection reason {response.rejectionReason}.");
            if (response.outcome != StartLevelAttemptOutcome.Approved && response.outcome != StartLevelAttemptOutcome.Rejected) throw new InvalidOperationException($"PlayFab StartLevelAttempt returned unsupported outcome {response.outcome}.");
            PlayerLivesContract.ValidateSnapshot(response.lives);
        }

        private  void ValidateAbandonLevelAttemptResponse(AbandonLevelAttemptResponse response)
        {
            if (response.schemaVersion != LevelAttemptContract.CurrentSchemaVersion) throw new InvalidOperationException($"PlayFab AbandonLevelAttempt returned unsupported schema version {response.schemaVersion}.");
            if (response.outcome == AbandonLevelAttemptOutcome.Abandoned && response.rejectionReason.HasValue) throw new InvalidOperationException("PlayFab AbandonLevelAttempt returned a rejection reason for an abandoned attempt.");
            if (response.outcome == AbandonLevelAttemptOutcome.Rejected && !response.rejectionReason.HasValue) throw new InvalidOperationException("PlayFab AbandonLevelAttempt rejected the attempt without a reason.");
            if (response.outcome != AbandonLevelAttemptOutcome.Abandoned && response.outcome != AbandonLevelAttemptOutcome.Rejected) throw new InvalidOperationException($"PlayFab AbandonLevelAttempt returned unsupported outcome {response.outcome}.");
        }

        private  void ValidateCompleteLevelAttemptResponse(CompleteLevelAttemptResponse response, bool didWin, int earnedStars)
        {
            if (response.outcome == CompleteLevelAttemptOutcome.Saved && (response.levelProgress == null || response.rejectionReason.HasValue)) throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned invalid saved progression data.");
            if (response.outcome == CompleteLevelAttemptOutcome.Saved && didWin && response.boosterInventorySnapshot == null) throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned no booster inventory snapshot for a saved win.");
            if (response.outcome == CompleteLevelAttemptOutcome.Rejected && !response.rejectionReason.HasValue) throw new InvalidOperationException("PlayFab CompleteLevelAttempt rejected the attempt without a reason.");
            if (response.outcome != CompleteLevelAttemptOutcome.Saved && response.outcome != CompleteLevelAttemptOutcome.Rejected) throw new InvalidOperationException($"PlayFab CompleteLevelAttempt returned unsupported outcome {response.outcome}.");
            if (response.completionRewardPresentationKeys == null) throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned no completion reward presentation keys collection.");
            if (response.outcome == CompleteLevelAttemptOutcome.Saved && didWin && response.completionRewardPresentationKeys.Count > earnedStars) throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned more completion rewards than earned stars.");
            foreach (string completionRewardPresentationKey in response.completionRewardPresentationKeys)
                if (string.IsNullOrWhiteSpace(completionRewardPresentationKey)) throw new InvalidOperationException("PlayFab CompleteLevelAttempt returned an empty completion reward presentation key.");
            PlayerLivesContract.ValidateSnapshot(response.lives);
        }
    }
}
