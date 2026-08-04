using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DefaultNamespace
{
    //TODO: need to make sure json is legit, put that logic somewhere
    public class LevelData
    {
        public int levelId;
        public int chapterId;
        public int? nextLevelId;
        public int boardWidth;
        public int boardHeight;
        public List<TileData> tiles;
        public List<StarScoreThresholdJson> starScoreThresholds;
        public List<ObjectiveJson> objectives;
        public List<ConstrainerJson> constrainers = new();

        [JsonIgnore]
        public int StarCap
        {
            get
            {
                if (starScoreThresholds == null)
                    throw new InvalidOperationException($"Level {levelId} has no star score thresholds.");

                int starCap = 0;
                foreach (StarScoreThresholdJson threshold in starScoreThresholds)
                    if (threshold.starCount > starCap) starCap = threshold.starCount;
                return starCap;
            }
        }
    }
}
