using System.Collections.Generic;

namespace BloomKeeper.PlayFabFunctions.Models;

public class LoadProgressionResponse
{
    public int schemaVersion;
    public int highestUnlockedLevel;
    public Dictionary<int, LevelProgressData> levels;
}
