using System.Collections.Generic;

namespace DefaultNamespace
{
    public class PlayerProgressionData
    {
        public int schemaVersion = 1;
        public int highestUnlockedLevel = 1;

        /// <summary>
        /// [Duong] Key = levelId. Value = saved progress for that level.
        /// </summary>
        public Dictionary<int, LevelProgressData> levels = new Dictionary<int, LevelProgressData>();

        public void ApplyLevelProgress(int levelId, LevelProgressData levelProgress, int highestUnlockedLevel)
        {
            this.highestUnlockedLevel = highestUnlockedLevel;
            levels[levelId] = levelProgress;
        }
    }
}
