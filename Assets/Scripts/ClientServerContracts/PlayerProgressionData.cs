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
    }
}
