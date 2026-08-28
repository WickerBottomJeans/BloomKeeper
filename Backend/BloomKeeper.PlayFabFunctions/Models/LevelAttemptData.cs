using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public class LevelAttemptData
{
    public int schemaVersion = LevelAttemptContract.CurrentSchemaVersion;
    public string attemptId;
    public string startLevelRequestIdempotencyKey;
    public int levelId;
    public LevelAttemptStatus status;
    public bool didSpendLife;
    public bool? didWin;
    public int? score;
    public int? stars;
}
