using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    public class PlayFabTutorialProgressService
    {
        public Task<CompleteTutorialResponse> CompleteTutorial(PlayFabAuthSession playFabAuthSession, string tutorialId)
        {
            if (playFabAuthSession == null) throw new ArgumentNullException(nameof(playFabAuthSession));
            TutorialProgressContract.ValidateTutorialId(tutorialId);
            var completeTutorialCompletionSource = new TaskCompletionSource<CompleteTutorialResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completeTutorialRequest = new CompleteTutorialRequest { tutorialId = tutorialId };
            var executeFunctionRequest = new ExecuteFunctionRequest
            {
                AuthenticationContext = new PlayFabAuthenticationContext(playFabAuthSession.SessionTicket, playFabAuthSession.EntityToken, playFabAuthSession.PlayFabId, playFabAuthSession.EntityId, playFabAuthSession.EntityType),
                Entity = new EntityKey { Id = playFabAuthSession.EntityId, Type = playFabAuthSession.EntityType },
                FunctionName = "CompleteTutorial",
                FunctionParameter = completeTutorialRequest
            };
            PlayFabCloudScriptAPI.ExecuteFunction(executeFunctionRequest, executeFunctionResult => HandleCompleteTutorialResult(executeFunctionResult, tutorialId, completeTutorialCompletionSource), error => completeTutorialCompletionSource.SetException(new PlayFabRequestException($"PlayFab CompleteTutorial request failed: {error.GenerateErrorReport()}", PlayFabRetryPolicy.IsRetryable(error), error.RetryAfterSeconds)));
            return completeTutorialCompletionSource.Task;
        }

        private void HandleCompleteTutorialResult(ExecuteFunctionResult executeFunctionResult, string tutorialId, TaskCompletionSource<CompleteTutorialResponse> completeTutorialCompletionSource)
        {
            try
            {
                completeTutorialCompletionSource.SetResult(ReadCompleteTutorialResponse(executeFunctionResult, tutorialId));
            }
            catch (Exception exception)
            {
                completeTutorialCompletionSource.SetException(exception);
            }
        }

        private CompleteTutorialResponse ReadCompleteTutorialResponse(ExecuteFunctionResult executeFunctionResult, string tutorialId)
        {
            if (executeFunctionResult == null) throw new InvalidOperationException("PlayFab CompleteTutorial returned no execution result.");
            if (executeFunctionResult.Error != null) throw new PlayFabRequestException($"PlayFab CompleteTutorial Azure Function failed: {executeFunctionResult.Error.Error}: {executeFunctionResult.Error.Message}", PlayFabRetryPolicy.IsRetryable(executeFunctionResult.Error));
            if (executeFunctionResult.FunctionResultTooLarge == true) throw new InvalidOperationException("PlayFab CompleteTutorial exceeded the result size limit.");
            if (executeFunctionResult.FunctionResult == null) throw new InvalidOperationException("PlayFab CompleteTutorial returned no function result.");
            string json = executeFunctionResult.FunctionResult is string stringResult ? stringResult : JsonConvert.SerializeObject(executeFunctionResult.FunctionResult);
            CompleteTutorialResponse completeTutorialResponse = JsonConvert.DeserializeObject<CompleteTutorialResponse>(json);
            if (completeTutorialResponse == null || completeTutorialResponse.schemaVersion != TutorialProgressContract.CurrentSchemaVersion) throw new InvalidOperationException("PlayFab CompleteTutorial returned an unsupported response.");
            if (completeTutorialResponse.tutorialId != tutorialId) throw new InvalidOperationException("PlayFab CompleteTutorial returned a different tutorial ID.");
            TutorialProgressContract.ValidateTutorialProgress(completeTutorialResponse.playerTutorialProgressData);
            if (!completeTutorialResponse.playerTutorialProgressData.completedTutorialIds.Contains(tutorialId)) throw new InvalidOperationException("PlayFab CompleteTutorial did not confirm completion.");
            return completeTutorialResponse;
        }
    }
}
