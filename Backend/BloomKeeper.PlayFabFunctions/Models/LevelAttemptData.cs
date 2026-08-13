using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public class LevelAttemptData
{
    public int schemaVersion = LevelAttemptContract.CurrentSchemaVersion;
    public string attemptId;
    public string startOperationId;
    public int levelId;
    public LevelAttemptStatus status;
    public bool? didWin;
    public int? score;
    public int? stars;
}
