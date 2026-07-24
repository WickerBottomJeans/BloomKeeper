using System.Collections.Generic;

namespace BloomKeeper.PlayFabFunctions.Models;

public class PlayerProgressionData
{
    public int schemaVersion = 1;
    public int highestUnlockedLevel = 1;

    /// <summary>
    /// Key = levelId. Value = saved progress for that level.
    /// </summary>
    public Dictionary<int, LevelProgressData> levels = new Dictionary<int, LevelProgressData>();

    /// <summary>
    /// Key = canonical attempt ID. Value = the request data accepted for that attempt.
    /// </summary>
    public Dictionary<string, ProcessedLevelAttemptData> processedLevelAttempts = new Dictionary<string, ProcessedLevelAttemptData>();
}
