using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using DefaultNamespace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace BloomKeeper.PlayFabFunctions.Functions;

public class CompleteTutorialFunction
{
    private const int MaxWriteAttempts = 3;
    private const int InitialConflictRetryDelayMilliseconds = 100;
    private readonly PlayFabFunctionContextReader playFabFunctionContextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabEntityFileClient playFabEntityFileClient = new PlayFabEntityFileClient();
    private readonly TutorialProgressFileStore tutorialProgressFileStore = new TutorialProgressFileStore();

    [Function("CompleteTutorial")]
    public async Task<IActionResult> CompleteTutorial([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        PlayFabFunctionExecutionContext playFabFunctionExecutionContext = await playFabFunctionContextReader.ReadContext(request);
        CompleteTutorialRequest completeTutorialRequest = playFabFunctionContextReader.GetFunctionArgument<CompleteTutorialRequest>(playFabFunctionExecutionContext);
        if (completeTutorialRequest == null || completeTutorialRequest.schemaVersion != TutorialProgressContract.CurrentSchemaVersion) return new BadRequestResult();
        try
        {
            TutorialProgressContract.ValidateTutorialId(completeTutorialRequest.tutorialId);
        }
        catch (ArgumentException)
        {
            return new BadRequestResult();
        }

        var playFabDataInstanceApi = playFabFunctionContextReader.CreateDataApi(playFabFunctionExecutionContext);
        var callerEntityKey = playFabFunctionContextReader.GetCallerEntity(playFabFunctionExecutionContext);
        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            var entityFilesResponse = await playFabEntityFileClient.LoadEntityFileMetadata(playFabDataInstanceApi, callerEntityKey);
            (PlayerTutorialProgressData playerTutorialProgressData, bool _) = await tutorialProgressFileStore.LoadTutorialProgress(playFabEntityFileClient, entityFilesResponse);
            if (playerTutorialProgressData.completedTutorialIds.Add(completeTutorialRequest.tutorialId))
            {
                try
                {
                    await playFabEntityFileClient.UploadFile(playFabDataInstanceApi, callerEntityKey, tutorialProgressFileStore.FileName, tutorialProgressFileStore.SerializeTutorialProgress(playerTutorialProgressData), entityFilesResponse.ProfileVersion);
                }
                catch (EntityProfileVersionConflictException) when (writeAttempt < MaxWriteAttempts)
                {
                    int delayMilliseconds = InitialConflictRetryDelayMilliseconds * (1 << (writeAttempt - 1));
                    await Task.Delay(delayMilliseconds, request.HttpContext.RequestAborted);
                    continue;
                }
                catch (EntityProfileVersionConflictException)
                {
                    return new StatusCodeResult(StatusCodes.Status409Conflict);
                }
            }

            var completeTutorialResponse = new CompleteTutorialResponse { tutorialId = completeTutorialRequest.tutorialId, playerTutorialProgressData = playerTutorialProgressData };
            return new ContentResult { Content = JsonConvert.SerializeObject(completeTutorialResponse), ContentType = "application/json", StatusCode = StatusCodes.Status200OK };
        }

        throw new InvalidOperationException("CompleteTutorial exhausted its write attempts without returning a result.");
    }
}
