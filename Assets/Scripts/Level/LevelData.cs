using System.Collections.Generic;

namespace DefaultNamespace
{
    //TODO: need to make sure json is legit, put that logic somewhere
    public class LevelData
    {
        public int levelId;
        public int boardWidth;
        public int boardHeight;
        public List<TileData> tiles;
        public List<StarScoreThresholdJson> starScoreThresholds;
        public List<ObjectiveJson> objectives;
        public List<ConstrainerJson> constrainers = new();
    }
}
