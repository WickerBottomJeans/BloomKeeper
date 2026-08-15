using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public class LevelAttemptCompletionResult
{
    public CompleteLevelAttemptResponse Response { get; }
    public PlayerProgressionData? UpdatedProgression { get; }
    public LevelAttemptData? UpdatedLevelAttempt { get; }

    public LevelAttemptCompletionResult(CompleteLevelAttemptResponse response, PlayerProgressionData? updatedProgression, LevelAttemptData? updatedLevelAttempt)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
        UpdatedProgression = updatedProgression;
        UpdatedLevelAttempt = updatedLevelAttempt;
    }
}
